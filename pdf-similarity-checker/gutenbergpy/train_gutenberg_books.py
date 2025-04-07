import os
import json
import numpy as np
import faiss
import nltk
from sentence_transformers import SentenceTransformer
from gutenbergpy.caches.sqlitecache import SQLiteCache
from gutenbergpy.textget import get_text_by_id
from nltk.tokenize import sent_tokenize
from gutenbergpy.gutenbergcachesettings import GutenbergCacheSettings


# Download NLTK tokenizer model (one-time setup)
nltk.download('punkt', quiet=True)

# ====== Configuration ======
##BOOK_IDS = [1342,84,2701]  # Pride, Alice, Moby Dick, Sherlock Holmes
EMBEDDING_DIM = 384
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DB_PATH = os.path.join(BASE_DIR, "gutenbergindex.db")
INDEX_FILE = os.path.join(BASE_DIR, "pdf_embeddings.index")
METADATA_FILE = os.path.join(BASE_DIR, "pdf_metadata.json")

# ====== Load SentenceTransformer Model ======
print("📦 Loading model...")
model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")

# ====== Setup FAISS Index ======
if os.path.exists(INDEX_FILE):
    index = faiss.read_index(INDEX_FILE)
    with open(METADATA_FILE, "r") as f:
        metadata = json.load(f)
else:
    index = faiss.IndexFlatL2(EMBEDDING_DIM)
    metadata = []

# ====== Load SQLite Cache ======
GutenbergCacheSettings.CACHE_FILENAME = DB_PATH
cache = SQLiteCache()
if not cache:
    raise Exception("Gutenberg metadata cache not found.")

existing_ids = {entry['book_id'] for entry in metadata}

# ========= Extract first 3000 Gutenberg book IDs =========
print("Fetching first 10 book IDs from metadata...")
query = "SELECT DISTINCT gutenbergbookid FROM books"
cursor = cache.native_query(query)
BOOK_IDS = [row[0] for row in cursor.fetchall()][:10]
print(f"Selected {len(BOOK_IDS)} books for embedding.")

# ====== Training Loop ======
for book_id in BOOK_IDS:
    try:
        print(f"\nProcessing Book ID: {book_id}")

        if book_id in existing_ids:
            print(f"Book ID {book_id} already exists. Skipping.")
            continue

        # Get title and author from database
        query = f"""
        SELECT books.gutenbergbookid, authors.name, titles.name
        FROM books
        JOIN book_authors ON books.id = book_authors.bookid
        JOIN authors ON authors.id = book_authors.authorid
        JOIN titles ON books.id = titles.bookid
        WHERE books.gutenbergbookid = {book_id}
        """
        cursor = cache.native_query(query)
        metadata_query = cursor.fetchall()

        if not metadata_query:
            print(f"No metadata found for Book ID: {book_id}")
            continue

        author = metadata_query[0][1] or "Unknown"
        title = metadata_query[0][2] or f"Book {book_id}"

        # Read and embed book text
        text = get_text_by_id(book_id).decode("utf-8")
        if len(text) < 1000:
            print("Skipping book due to insufficient content.")
            continue

        # Embed the entire book as a single vector
        book_embedding = model.encode(text)
        book_embedding = book_embedding / np.linalg.norm(book_embedding)  # Optional: normalize

        index.add(np.array([book_embedding]))
        
        # Chunk & embed
        # sentences = sent_tokenize(text)
        # chunks = [" ".join(sentences[i:i+30]) for i in range(0, len(sentences), 30)]
        # chunk_embeddings = model.encode(chunks)
        #book_embedding = np.mean(text, axis=0)
        #index.add(np.array([book_embedding]))

        metadata.append({
            "book_id": book_id,
            "title": title,
            "author": author
        })

        print(f"Embedded '{title}' by {author}")

    except Exception as e:
        print(f"Failed to process book {book_id}: {e}")

# ====== Save index and metadata ======
faiss.write_index(index, INDEX_FILE)
with open(METADATA_FILE, "w") as f:
    json.dump(metadata, f, indent=2)

print("\n🎉 Training complete. Books embedded and stored in FAISS.")
print(f"Index saved to {INDEX_FILE}")