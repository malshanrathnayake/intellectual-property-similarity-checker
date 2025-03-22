import requests
import json
import faiss
import numpy as np
from sentence_transformers import SentenceTransformer

# Initialize Hugging Face model
model = SentenceTransformer('sentence-transformers/all-mpnet-base-v2')

# FAISS setup
embedding_dim = 768  # embedding size for all-mpnet-base-v2
index = faiss.IndexFlatL2(embedding_dim)
metadata = []

# Example WIPO data retrieval (Replace with actual WIPO API details)
def get_patent_data(patent_id):
    # Example endpoint (you must replace this with actual WIPO API endpoint and API key if required)
    url = f"https://patentscope.wipo.int/search/en/detail.jsf?docId={patent_id}&redirectedID=true"
    response = requests.get(url)
    if response.status_code == 200:
        data = response.json()
        title = data.get('title', '')
        abstract = data.get('abstract', '')
        claims = data.get('claims', '')
        combined_text = f"{title}. {abstract}. {claims}"
        return combined_text
    return None

patent_ids = ['WO2023001234', 'WO2022005678']  # Example patent IDs from WIPO
for pid in patent_ids:
    patent_text = get_patent_data(pid)
    if patent_text:
        embedding = model.encode(patent_text)
        index.add(np.array([embedding]))
        metadata.append({'patent_id': pid})

# Save embeddings and metadata
faiss.write_index(index, 'patent_embeddings.index')
with open('patent_metadata.json', 'w') as f:
    json.dump(metadata, f)
