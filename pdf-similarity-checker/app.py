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

# Load pre-trained Hugging Face model
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')

# Vector database (FAISS index)
index_file = "pdf_embeddings.index"
metadata_file = "pdf_metadata.json"
embedding_dim = 384  # Dimension for 'all-MiniLM-L6-v2'

# Initialize FAISS index
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

@app.route('/upload_and_train', methods=['POST'])
def upload_and_train():
    try:
        pdf = request.files['pdf']
        pdf_name = pdf.filename

        text = extract_text(pdf)
        embedding = model.encode(text)

        # Store embedding into FAISS
        index.add(np.array([embedding]))
        metadata.append({"filename": pdf_name})

        # Save the index and metadata
        faiss.write_index(index, index_file)
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f)

        return jsonify({"message": "PDF trained successfully."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/check_similarity', methods=['POST'])
def check_similarity():
    try:
        pdf = request.files['pdf']
        text = extract_text(pdf)
        embedding = model.encode(text)

        # Perform similarity search (nearest neighbor)
        distances, indices = index.search(np.array([embedding]), k=3)  # top 3 similar results

        results = []
        for distance, idx in zip(distances[0], indices[0]):
            results.append({
                "filename": metadata[idx]['filename'],
                "similarity": float(1 / (1 + distance))  # convert distance to similarity (0-1)
            })

        return jsonify({"similar_pdfs": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
