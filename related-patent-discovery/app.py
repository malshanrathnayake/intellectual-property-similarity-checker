from fastapi import FastAPI, Query, File, UploadFile
from pydantic import BaseModel
from typing import List
from sentence_transformers import SentenceTransformer
import faiss
import numpy as np
import json
from blockchain_service import get_cid_from_blockchain, store_cid_on_blockchain
from ipfs_uploader import upload_json_to_pinata, upload_file_to_pinata
from utils.pdf_extractor import extract_patent_sections
from utils.embed_utils import append_to_embeddings, save_faiss_index
import fitz
from time import time
import os
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # You can change this to ["http://127.0.0.1:5501"] for stricter rules
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

model = SentenceTransformer('sentence-transformers/all-mpnet-base-v2')

with open("patents.json", "r", encoding="utf-8") as f:
    patent_data = json.load(f)

EMBED_PATH = "cache/embeddings.npy"
INDEX_PATH = "cache/faiss.index"

if not os.path.exists(EMBED_PATH) or not os.path.exists(INDEX_PATH):
    raise RuntimeError("Cached FAISS index or embeddings not found. Run tools/rebuild_faiss_index.py first.")

embeddings = np.load(EMBED_PATH)
index = faiss.read_index(INDEX_PATH)

@app.get("/search")
def search_patents(query: str = Query(...), top_k: int = 5, threshold: float = 1.0):
    query_embedding = model.encode([query])
    distances, indices = index.search(np.array(query_embedding), top_k)

    results = []
    for dist, idx in zip(distances[0], indices[0]):
        if dist < threshold:
            result = patent_data[idx].copy()
            result["faiss_distance"] = float(round(dist, 4))
            cid = get_cid_from_blockchain(result["id"])
            result["cid"] = cid
            results.append(result)

    return {
        "query": query,
        "distance_threshold": threshold,
        "results_found": len(results),
        "results": results
    }

@app.get("/cid/{patent_id}")
def read_cid(patent_id: int):
    cid = get_cid_from_blockchain(patent_id)
    return {"patent_id": patent_id, "cid": cid}

class PatentSubmission(BaseModel):
    title: str
    abstract: str
    claims: List[str]

@app.post("/register")
def register_patent(submission: PatentSubmission, threshold: float = 0.7):
    content = submission.abstract + " " + " ".join(submission.claims)
    embedding = model.encode([content])
    distances, indices = index.search(np.array(embedding), 5)

    similar = [
        {
            "id": patent_data[i]["id"],
            "title": patent_data[i]["title"],
            "faiss_distance": float(round(d, 4))
        }
        for d, i in zip(distances[0], indices[0]) if d < threshold
    ]

    if similar:
        return {
            "status": "rejected",
            "reason": "semantically similar patents found",
            "similar": similar
        }

    return {
        "status": "approved",
        "message": "Patent is considered novel. Ready to store to IPFS and blockchain.",
        "faiss_distances": [float(round(d, 4)) for d in distances[0]]
    }

@app.post("/register/pdf")
async def register_patent_pdf(file: UploadFile = File(...), threshold: float = 0.7):
    try:
        file_bytes = await file.read()
        if len(file_bytes) > 10 * 1024 * 1024:
            return {"success": False, "message": "PDF too large. Must be under 10MB."}

        try:
            doc = fitz.open(stream=file_bytes, filetype="pdf")
            if len(doc) > 50:
                return {"success": False, "message": f"PDF too long ({len(doc)} pages). Max 50 allowed."}
        except Exception:
            pass

        extracted = extract_patent_sections(file_bytes)
        source = extracted.get("source", "unknown")
        title = extracted["title"]
        abstract = extracted["abstract"]
        claims = extracted["claims"]

        if not abstract:
            return {"success": False, "message": "Extraction failed — missing abstract."}

        content = abstract + " " + " ".join(claims)
        embedding = model.encode([content])
        distances, indices = index.search(np.array(embedding), 5)

        similar = []
        for d, i in zip(distances[0], indices[0]):
            if d < threshold:
                matched = patent_data[i]
                cid = get_cid_from_blockchain(matched["id"])  # fetch CID for rejected ones testt
                similar.append({
                    "id": matched["id"],
                    "title": matched["title"],
                    "faiss_distance": float(round(d, 4)),
                    "cid": cid,
                    "pdf_cid": matched.get("pdf_cid")
                })

        if similar:
            return {
                "success": False,
                "status": "rejected",
                "reason": "semantically similar patents found",
                "similar": similar
            }

        pdf_cid = upload_file_to_pinata(file_bytes, file.filename)
        new_patent = {
            "id": len(patent_data) + 1,
            "title": title,
            "abstract": abstract,
            "claims": claims,
            "pdf_cid": pdf_cid  # optional but helpful
        }

        metadata_cid = upload_json_to_pinata(new_patent, file.filename + "_meta.json")
        
        if not metadata_cid:
            return {"success": False, "message": "Failed to upload to IPFS."}

        tx_hash = store_cid_on_blockchain(new_patent["id"], metadata_cid)
        if not tx_hash:
            return {"success": False, "message": "CID uploaded but storing to blockchain failed."}

        patent_data.append(new_patent)
        with open("patents.json", "w", encoding="utf-8") as f:
            json.dump(patent_data, f, indent=2, ensure_ascii=False)

        # Incremental update
        index.add(np.array(embedding))
        append_to_embeddings(embedding)
        save_faiss_index(index)

        return {
            "success": True,
            "status": "approved",
            "source": source,
            "message": "Patent registered and stored on IPFS and blockchain.",
            "metadata_cid": metadata_cid,
            "pdf_cid": pdf_cid,
            "tx_hash": tx_hash,
            "faiss_distances": [float(round(d, 4)) for d in distances[0]],
            "extracted": {
                "title": title,
                "abstract": abstract,
                "claims": claims
            }
        }

    except Exception as e:
        print(f"[ERROR] {e}")
        return {"success": False, "message": str(e)}
    
@app.get("/registered")
def get_recent_patents(limit: int = 10):
    results = []
    for p in reversed(patent_data[-limit:]):  # get last N
        cid = get_cid_from_blockchain(p["id"])
        results.append({
            "id": p["id"],
            "title": p["title"],
            "abstract": p.get("abstract", ""),
            "claims": p.get("claims", []), 
            "cid": cid,
            "pdf_cid": p.get("pdf_cid")
        })
    return {"patents": results}

