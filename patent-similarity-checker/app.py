from flask import Flask, request, jsonify
from flask_cors import CORS
import faiss
import numpy as np
import os
import json
from sentence_transformers import SentenceTransformer

app = Flask(__name__)
CORS(app)

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

model = SentenceTransformer('sentence-transformers/all-mpnet-base-v2')

index_file = os.path.join(BASE_DIR, "patent_embeddings.index")
metadata_file = os.path.join(BASE_DIR, "patent_metadata.json")

# Load or initialize FAISS index
embedding_dim = 768
if os.path.exists(index_file):
    index = faiss.read_index(index_file)
    with open(metadata_file, 'r') as f:
        metadata = json.load(f)
else:
    index = faiss.IndexFlatL2(embedding_dim)
    metadata = []

# Save index & metadata helper
def save_data():
    faiss.write_index(index, index_file)
    with open(metadata_file, 'w') as f:
        json.dump(metadata, f)

# Endpoint to check similarity
@app.route('/check_patent_similarity', methods=['POST'])
def check_patent_similarity():
    try:
        data = request.json
        title = data.get('title', '')
        abstract = data.get('abstract', '')
        claims = data.get('claims', '')

        combined_text = f"{title}. {abstract}. {claims}"
        embedding = model.encode(combined_text)

        num_embeddings = index.ntotal
        if num_embeddings == 0:
            return jsonify({"similar_patents": [], "message": "No embeddings available."})

        k = min(5, num_embeddings)
        distances, indices = index.search(np.array([embedding]), k=k)

        results = []
        seen_patents = set()
        for distance, idx in zip(distances[0], indices[0]):
            pid = metadata[idx]['patent_id']
            if pid not in seen_patents:
                results.append({
                    "patent_id": pid,
                    "similarity": float(1 / (1 + distance))
                })
                seen_patents.add(pid)

        return jsonify({"similar_patents": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Real-time training endpoint
@app.route('/train_patent', methods=['POST'])
def train_patent():
    try:
        data = request.json
        patent_id = data.get('patent_id', '')
        title = data.get('title', '')
        abstract = data.get('abstract', '')
        claims = data.get('claims', '')

        if not patent_id or not abstract or not claims:
            return jsonify({"error": "patent_id, abstract, and claims are required"}), 400

        combined_text = f"{title}. {abstract}. {claims}"
        embedding = model.encode(combined_text)

        index.add(np.array([embedding]))
        metadata.append({'patent_id': patent_id, 'title': title})

        save_data()

        return jsonify({"message": "Patent trained successfully."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)
