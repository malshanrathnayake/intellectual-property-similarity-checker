from flask import Flask, request, jsonify
from flask_cors import CORS
from transformers import CLIPProcessor, CLIPModel
import torch
import cv2
import faiss
import numpy as np
import os
import json

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
        video = request.files['video']
        video_name = video.filename
        video_path = os.path.join(uploaded_videos_dir, video_name)
        
        video.save(video_path)

        frames = extract_key_frames(video_path)
        embedding = generate_video_embedding(frames)

        index.add(np.array([embedding]))
        metadata.append({"filename": video_name})

        faiss.write_index(index, index_file)
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f)

        os.remove(video_path)

        return jsonify({"message": "Video trained successfully."})

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

        os.remove(video_path)

        return jsonify({"similar_videos": results})

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=6000)