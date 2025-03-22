from flask import Flask, request, jsonify
from flask_cors import CORS
from transformers import CLIPProcessor, CLIPModel
import torch
import faiss
import numpy as np
import os
import json
from PIL import Image

app = Flask(__name__)
CORS(app)

# Base directory explicitly defined
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

# Initialize FAISS index and metadata
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
    return embedding.cpu().numpy()[0]

@app.route('/upload_and_train_image', methods=['POST'])
def upload_and_train_image():
    try:
        image = request.files['image']
        image_name = image.filename
        image_path = os.path.join(uploaded_images_dir, image_name)
        
        image.save(image_path)

        embedding = generate_image_embedding(image_path)

        index.add(np.array([embedding]))
        metadata.append({"filename": image_name})

        faiss.write_index(index, index_file)
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f)

        os.remove(image_path)

        return jsonify({"message": "Image trained successfully."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/check_image_similarity', methods=['POST'])
def check_image_similarity():
    try:
        image = request.files['image']
        image_name = image.filename
        image_path = os.path.join(uploaded_images_dir, image_name)

        image.save(image_path)

        embedding = generate_image_embedding(image_path)

        num_embeddings = index.ntotal
        if num_embeddings == 0:
            return jsonify({"similar_images": [], "message": "No embeddings available."})

        k = min(3, num_embeddings)
        distances, indices = index.search(np.array([embedding]), k=k)

        results = []
        seen_files = set()
        for distance, idx in zip(distances[0], indices[0]):
            filename = metadata[idx]['filename']
            if filename not in seen_files:
                results.append({
                    "filename": filename,
                    "similarity": float(1 / (1 + distance))
                })
                seen_files.add(filename)

        os.remove(image_path)

        return jsonify({"similar_images": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=7000)
