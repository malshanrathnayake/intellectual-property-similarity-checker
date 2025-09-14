import os
os.environ["KMP_DUPLICATE_LIB_OK"] = "TRUE"

from flask import Flask, request, jsonify
from flask_cors import CORS
from transformers import CLIPProcessor, CLIPModel
import torch
import faiss
import numpy as np
import json
from PIL import Image
from datetime import datetime

app = Flask(__name__)
CORS(app)

# Base directory
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

# Model setup (CLIP)
device = "cuda" if torch.cuda.is_available() else "cpu"
model_name = "openai/clip-vit-base-patch32"
model = CLIPModel.from_pretrained(model_name).to(device)
processor = CLIPProcessor.from_pretrained(model_name)

# FAISS setup
index_file = os.path.join(BASE_DIR, "image_embeddings.index")
metadata_file = os.path.join(BASE_DIR, "image_metadata.json")
uploaded_images_dir = os.path.join(BASE_DIR, "uploaded_images")
os.makedirs(uploaded_images_dir, exist_ok=True)

embedding_dim = 512

# Initialize FAISS + metadata
if os.path.exists(index_file):
    index = faiss.read_index(index_file)
    with open(metadata_file, 'r') as f:
        metadata = json.load(f)
else:
    index = faiss.IndexFlatL2(embedding_dim)
    metadata = []

# Generate image embedding
def generate_image_embedding(image_path):
    image = Image.open(image_path).convert("RGB")
    inputs = processor(images=image, return_tensors="pt").to(device)
    with torch.no_grad():
        embedding = model.get_image_features(**inputs)
    embedding = embedding.cpu().numpy().flatten().astype("float32")
    return embedding


@app.route('/upload_and_train_image', methods=['POST'])
def upload_and_train_image():
    try:
        image = request.files['image']
        image_name = image.filename
        image_path = os.path.join(uploaded_images_dir, image_name)
        image.save(image_path)

        # Generate embedding
        embedding = generate_image_embedding(image_path)
        index.add(np.array([embedding]))

        # Collect metadata from request form
        title = request.form.get("title")
        category = request.form.get("category")
        creator = request.form.get("creator")
        description = request.form.get("description")
        published_source = request.form.get("published_source")
        date_of_creation = request.form.get("date_of_creation")
        wallet_address = request.form.get("wallet_address")

        # Convert date safely
        if date_of_creation:
            try:
                date_of_creation = str(datetime.strptime(date_of_creation, "%Y-%m-%d").date())
            except:
                date_of_creation = None

        # Append metadata
        metadata.append({
            "filename": image_name,
            "title": title,
            "category": category,
            "creator": creator,
            "description": description,
            "published_source": published_source,
            "date_of_creation": date_of_creation,
            "wallet_address": wallet_address
        })

        # Persist index + metadata
        faiss.write_index(index, index_file)
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f, indent=2)

        os.remove(image_path)
        return jsonify({"message": "Image trained successfully with metadata."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/check_image_similarity', methods=['POST'])
def check_image_similarity():
    try:
        image = request.files['image']
        image_name = image.filename
        image_path = os.path.join(uploaded_images_dir, image_name)
        image.save(image_path)

        # ensure embedding is 1D float32
        embedding = generate_image_embedding(image_path)

        if index.ntotal == 0:
            return jsonify({"similar_images": [], "message": "No embeddings available."})

        k = min(5, index.ntotal)  # ask for more neighbors so we can drop the self-match
        query_vec = np.array([embedding]).astype("float32")
        distances, indices = index.search(query_vec, k=k)

        results = []
        seen_files = set()
        for distance, idx in zip(distances[0], indices[0]):
            entry = metadata[idx]

            if entry["filename"] not in seen_files:
                results.append({
                    "filename": entry["filename"],
                    "title": entry.get("title"),
                    "category": entry.get("category"),
                    "creator": entry.get("creator"),
                    "description": entry.get("description"),
                    "published_source": entry.get("published_source"),
                    "date_of_creation": entry.get("date_of_creation"),
                    "wallet_address": entry.get("wallet_address"),
                    "similarity": float(1 / (1 + distance))
                })
                seen_files.add(entry["filename"])

        os.remove(image_path)
        return jsonify({"similar_images": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/get_all_metadata', methods=['GET'])
def get_all_metadata():
    try:
        if not os.path.exists(metadata_file):
            return jsonify([])

        with open(metadata_file, 'r') as f:
            data = json.load(f)

        return jsonify(data)

    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=7000)
