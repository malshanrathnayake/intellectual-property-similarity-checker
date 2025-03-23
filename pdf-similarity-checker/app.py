from flask import Flask, request, jsonify
from flask_cors import CORS
from sentence_transformers import SentenceTransformer
import fitz  # PyMuPDF
import faiss
import os
import numpy as np
import json

app = Flask(__name__)
CORS(app)

# Base directory and files
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
index_file = os.path.join(BASE_DIR, "pdf_embeddings.index")
metadata_file = os.path.join(BASE_DIR, "pdf_metadata.json")

# Load pre-trained model
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
embedding_dim = 384

# Initialize FAISS + metadata
if os.path.exists(index_file):
    index = faiss.read_index(index_file)
    with open(metadata_file, 'r') as f:
        metadata = json.load(f)
else:
    index = faiss.IndexFlatL2(embedding_dim)
    metadata = []

# PDF text extraction
def extract_text(pdf_file):
    text = ""
    with fitz.open(stream=pdf_file.read(), filetype="pdf") as doc:
        for page in doc:
            text += page.get_text()
    return text

# Upload and train endpoint
@app.route('/upload_and_train', methods=['POST'])
def upload_and_train():
    try:
        pdf = request.files['pdf']
        pdf_name = pdf.filename

        text = extract_text(pdf)
        embedding = model.encode(text)

        index.add(np.array([embedding]))
        metadata.append({"filename": pdf_name})

        faiss.write_index(index, index_file)
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f)

        return jsonify({"message": "PDF trained successfully."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Check similarity endpoint
@app.route('/check_similarity', methods=['POST'])
def check_similarity():
    try:
        pdf = request.files['pdf']
        text = extract_text(pdf)
        embedding = model.encode(text)

        num_embeddings = index.ntotal
        if num_embeddings == 0:
            return jsonify({
                "similar_pdfs": [],
                "message": "No PDFs in the database to compare against."
            })

        k = min(3, num_embeddings)
        distances, indices = index.search(np.array([embedding]), k=k)

        results = []
        seen_files = set()
        for distance, idx in zip(distances[0], indices[0]):
            if 0 <= idx < len(metadata):
                filename = metadata[idx]['filename']
                if filename not in seen_files:
                    results.append({
                        "filename": filename,
                        "similarity": float(1 / (1 + distance))
                    })
                    seen_files.add(filename)

        return jsonify({"similar_pdfs": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

# Run server
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
