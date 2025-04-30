# tools/rebuild_faiss_index.py

import json
import numpy as np
import faiss
from sentence_transformers import SentenceTransformer
import os

# Paths
DATA_PATH = "patents.json"
EMBED_PATH = "cache/embeddings.npy"
INDEX_PATH = "cache/faiss.index"

# Ensure cache dir exists
os.makedirs("cache", exist_ok=True)

# Load dataset
with open(DATA_PATH, "r", encoding="utf-8") as f:
    patent_data = json.load(f)

# Extract abstracts
texts = [p["abstract"] for p in patent_data if "abstract" in p and p["abstract"].strip()]
print(f"[INFO] Total abstracts found: {len(texts)}")

# Generate and cache embeddings
model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")
embeddings = model.encode(texts, show_progress_bar=True)
np.save(EMBED_PATH, embeddings)

# Build and save FAISS index
dimension = embeddings.shape[1]
index = faiss.IndexFlatL2(dimension)
index.add(embeddings)
faiss.write_index(index, INDEX_PATH)

print(f"[SUCCESS] Embeddings and FAISS index written to cache/")
