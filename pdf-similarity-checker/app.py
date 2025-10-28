from flask import Flask, request, jsonify
from flask_cors import CORS
from sentence_transformers import SentenceTransformer
import fitz  # PyMuPDF
import faiss
import os
import numpy as np
import json
from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from io import BytesIO
from flask import send_file

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
        if 'pdf' not in request.files:
            return jsonify({"error": "No PDF uploaded"}), 400
        
        pdf = request.files['pdf']
        text = extract_text(pdf).strip()

        # Reject empty or invalid PDF
        if not text or len(text.split()) < 10:
            return jsonify({
                "similar_books": [],
                "message": "No meaningful text found in PDF. Please upload a valid text-based document."
            }), 400

        # Encode and normalize
        embedding = model.encode(text)
         # Normalize embedding for cosine-like distance (optional but recommended)
        embedding = embedding / np.linalg.norm(embedding)

        # Ensure FAISS index is available
        num_embeddings = index.ntotal
        if num_embeddings == 0:
            return jsonify({
                "similar_books": [],
                "message": "No books in the database to compare against."
            }), 200

         # Switch to cosine similarity index if necessary
        if not isinstance(index, faiss.IndexFlatIP):
            xb = index.reconstruct_n(0, index.ntotal)
            xb = xb / np.linalg.norm(xb, axis=1, keepdims=True)
            new_index = faiss.IndexFlatIP(embedding_dim)
            new_index.add(xb)
            globals()['index'] = new_index
            print("⚠️ Converted FAISS index to cosine similarity (IP mode).")

        k = min(3, num_embeddings)
        similarities, indices = index.search(np.array([embedding]), k=k)

        results = []
        for sim, idx in zip(similarities[0], indices[0]):
            if 0 <= idx < len(metadata):
                book = metadata[idx]
                results.append({
                    "book_id": book.get("book_id"),
                    "title": book.get("title"),
                    "author": book.get("author"),
                    "year": book.get("year"),
                    "language": book.get("language"),
                    "publisher": book.get("publisher"),
                    "rights": book.get("rights"),
                    "downloads": book.get("downloads"),
                    "word_count": book.get("word_count"),
                    "char_count": book.get("char_count"),
                    "text_path": book.get("text_path"),
                    "similarity": round(float(sim), 4)
                })

        return jsonify({ "similar_books": results,
                         "count": len(results),
                         "message": f"Top {len(results)} similar works found (cosine similarity)."
        }), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from io import BytesIO
from datetime import datetime

@app.route('/generate_report', methods=['POST'])
def generate_report():
    try:
        data = request.get_json()
        results = data.get("similar_books", [])
        filename = data.get("filename", "UploadedDocument.pdf")
        title = "PDF Similarity Report"
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        # Create a PDF in memory
        buffer = BytesIO()
        c = canvas.Canvas(buffer, pagesize=A4)
        width, height = A4

        # Header
        c.setFont("Helvetica-Bold", 16)
        c.drawString(50, height - 50, title)
        c.setFont("Helvetica", 10)
        c.drawString(50, height - 70, f"Generated on: {timestamp}")
        c.drawString(50, height - 85, f"Uploaded File: {filename}")
        c.line(50, height - 95, width - 50, height - 95)

        # Table header
        y = height - 120
        c.setFont("Helvetica-Bold", 11)
        c.drawString(50, y, "Title")
        c.drawString(220, y, "Author")
        c.drawString(360, y, "Similarity")
        c.setFont("Helvetica", 10)
        y -= 15

        # Table rows
        for item in results:
            c.drawString(50, y, item.get("title", "N/A")[:25])
            c.drawString(220, y, item.get("author", "N/A")[:20])
            c.drawString(360, y, f"{item.get('similarity', 0) * 100:.2f}%")
            y -= 15
            if y < 100:  # new page if full
                c.showPage()
                y = height - 100

        c.save()
        buffer.seek(0)

        # Return the PDF file as a download
        return send_file(
            buffer,
            as_attachment=True,
            download_name="Similarity_Report.pdf",
            mimetype="application/pdf"
        )

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route('/', methods=['GET'])
def home():
    return jsonify({"status": "running", "message": "PDF Similarity API is live!"})

@app.route('/health', methods=['GET'])
def health():
    return "OK", 200

# Run server
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)