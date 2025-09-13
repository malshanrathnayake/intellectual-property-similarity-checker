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

# Base directory and filess
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
        text = extract_text(pdf)

        # Collect metadata fields from form
        book_id = 220  # Example book ID, replace with actual logic to generate unique IDs
        title = request.form.get('title', pdf.filename)
        author = request.form.get('author', 'Unknown')

        # Encode and normalize
        embedding = model.encode(text)
        embedding = embedding / np.linalg.norm(embedding)

        # Add to FAISS index
        index.add(np.array([embedding]))
        metadata.append({
            "book_id": book_id,
            "title": title,
            "author": author
        })

        # Save index and updated metadata
        faiss.write_index(index, index_file)
        with open(metadata_file, 'w', encoding='utf-8') as f:
            json.dump(metadata, f, indent=2)

        return jsonify({"message": "Book embedded and stored successfully."})

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
                "similar_books": [],
                "message": "No books in the database to compare against."
            })

        # Normalize embedding for cosine-like distance (optional but recommended)
        embedding = embedding / np.linalg.norm(embedding)

        k = min(3, num_embeddings)
        distances, indices = index.search(np.array([embedding]), k=k)

        results = []
        for distance, idx in zip(distances[0], indices[0]):
            if 0 <= idx < len(metadata):
                book = metadata[idx]
                similarity = float(1 / (1 + distance))  # or 1 - distance for cosine
                results.append({
                    "book_id": book.get("book_id"),
                    "title": book.get("title"),
                    "author": book.get("author"),
                    "similarity": round(similarity, 4)
                })

        return jsonify({"similar_books": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500


# Run server
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
