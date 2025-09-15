import os
os.environ["USE_TF"] = "0"
os.environ["KMP_DUPLICATE_LIB_OK"] = "TRUE"
from flask import Flask, request, jsonify
from flask_cors import CORS
from transformers import CLIPProcessor, CLIPModel
import torch
import cv2
import faiss
import numpy as np
import json
from datetime import datetime, timezone

app = Flask(__name__)
CORS(app)

# Set the base directory explicitly
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

# Load CLIP model
device = "cuda" if torch.cuda.is_available() else "cpu"
model_name = "openai/clip-vit-base-patch32"
model = CLIPModel.from_pretrained(model_name).to(device)
processor = CLIPProcessor.from_pretrained(model_name)

# FAISS index and metadata
index_file = os.path.join(BASE_DIR, "video_embeddings.index")
metadata_file = os.path.join(BASE_DIR, "video_metadata.json")
uploaded_videos_dir = os.path.join(BASE_DIR, "uploaded_videos")

os.makedirs(uploaded_videos_dir, exist_ok=True)

embedding_dim = 512

# Initialize FAISS index and metadata
if os.path.exists(index_file):
    index = faiss.read_index(index_file)
    with open(metadata_file, 'r') as f:
        metadata = json.load(f)
else:
    index = faiss.IndexFlatL2(embedding_dim)
    metadata = []

# Extract key frames from video
def extract_key_frames(video_path, interval=30):
    cap = cv2.VideoCapture(video_path)
    frames = []
    count = 0
    success, frame = cap.read()
    while success:
        if count % interval == 0:
            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            frames.append(frame)
        success, frame = cap.read()
        count += 1
    cap.release()
    return frames

# Generate embeddings from frames
def generate_video_embedding(frames):
    inputs = processor(images=frames, return_tensors="pt").to(device)
    with torch.no_grad():
        embeddings = model.get_image_features(**inputs)
    video_embedding = embeddings.mean(dim=0).cpu().numpy()
    return video_embedding

@app.route('/upload_and_train_video', methods=['POST'])
def upload_and_train_video():
    try:
        if 'video' not in request.files:
            return jsonify({"error": "Missing 'video' file."}), 400

        video = request.files['video']
        video_name = video.filename
        if not video_name:
            return jsonify({"error": "Uploaded file has no name."}), 400

        video_path = os.path.join(uploaded_videos_dir, video_name)
        os.makedirs(uploaded_videos_dir, exist_ok=True)
        video.save(video_path)

        # Optional text fields (C# sends these)
        title = request.form.get('title', '').strip()
        category = request.form.get('category', '').strip()
        description = request.form.get('description', '').strip()
        published_source = request.form.get('published_source', '').strip()
        creator = request.form.get('creator', '').strip()
        wallet_address = request.form.get('wallet_address', '').strip()

        # Auto-set date (UTC) as YYYY-MM-DD
        date_of_creation = datetime.now(timezone.utc).date().isoformat()

        # Embed & index
        frames = extract_key_frames(video_path)
        embedding = generate_video_embedding(frames)
        index.add(np.array([embedding], dtype=np.float32))

        # Save metadata with auto date
        metadata.append({
            "filename": video_name,
            "title": title,
            "category": category,
            "creator": creator,
            "description": description,
            "published_source": published_source,
            "date_of_creation": date_of_creation,
            "wallet_address": wallet_address
        })

        # Persist
        faiss.write_index(index, index_file)
        with open(metadata_file, 'w', encoding='utf-8') as f:
            json.dump(metadata, f, ensure_ascii=False, indent=2)

        # Clean up
        try:
            os.remove(video_path)
        except OSError:
            pass

        return jsonify({"message": "Video trained successfully with metadata."})

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route('/check_video_similarity', methods=['POST'])
def check_video_similarity():
    try:
        video = request.files['video']
        video_name = video.filename
        video_path = os.path.join(uploaded_videos_dir, video_name)

        video.save(video_path)

        frames = extract_key_frames(video_path)
        embedding = generate_video_embedding(frames)

        num_embeddings = index.ntotal
        if num_embeddings == 0:
            return jsonify({"similar_videos": [], "message": "No embeddings available."})

        k = min(3, num_embeddings)
        distances, indices = index.search(np.array([embedding], dtype=np.float32), k=k)

        results = []
        seen_files = set()
        for distance, idx in zip(distances[0], indices[0]):
            entry = metadata[idx]  # full metadata dict from video_metadata.json
            filename = entry.get("filename")
            if filename not in seen_files:
                results.append({
                    "filename": entry.get("filename"),
                    "title": entry.get("title"),
                    "category": entry.get("category"),
                    "creator": entry.get("creator"),
                    "description": entry.get("description"),
                    "published_source": entry.get("published_source"),
                    "date_of_creation": entry.get("date_of_creation"),
                    "wallet_address": entry.get("wallet_address", ""),  # optional
                    "similarity": float(1 / (1 + distance))
                })
                seen_files.add(filename)

        os.remove(video_path)

        return jsonify({"similar_videos": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/get_all_video_metadata', methods=['GET'])
def get_all_video_metadata():
    try:
        if not os.path.exists(metadata_file):
            return jsonify([])

        with open(metadata_file, 'r', encoding='utf-8') as f:
            data = json.load(f)

        return jsonify(data)

    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=6000)
