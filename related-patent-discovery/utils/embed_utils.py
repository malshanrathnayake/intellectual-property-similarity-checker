import numpy as np
import os
import faiss

EMBED_PATH = "cache/embeddings.npy"
INDEX_PATH = "cache/faiss.index"


def load_cached_embeddings():
    if os.path.exists(EMBED_PATH):
        return np.load(EMBED_PATH)
    else:
        return np.empty((0, 768), dtype=np.float32)  # assuming 768 dim


def append_to_embeddings(new_embedding):
    new_embedding = np.array(new_embedding, dtype=np.float32)
    existing = load_cached_embeddings()
    combined = np.vstack([existing, new_embedding])
    np.save(EMBED_PATH, combined)


def save_faiss_index(index):
    faiss.write_index(index, INDEX_PATH)
