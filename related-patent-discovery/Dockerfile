# ===== 1. Base image =====
FROM tiangolo/uvicorn-gunicorn-fastapi:python3.10-slim

# ===== 2. Install system dependencies =====
RUN apt-get update && apt-get install -y \
    poppler-utils \
    tesseract-ocr \
    && rm -rf /var/lib/apt/lists/*

# ===== 3. Set work directory =====
WORKDIR /app

# ===== 4. Copy project files =====
COPY . /app

# ===== 5. Install Python dependencies =====
RUN pip install --upgrade pip setuptools wheel && \
    pip install --no-cache-dir torch==2.0.1 torchvision==0.15.2 torchaudio==2.0.2 && \
    pip install --no-cache-dir sentence-transformers==2.2.2 faiss-cpu==1.7.4 && \
    pip install --no-cache-dir -r requirements.txt && \
    apt-get clean && rm -rf /var/lib/apt/lists/* /root/.cache/pip

# ===== 6. Expose FastAPI port =====
EXPOSE 8000

# ===== 7. Run app using Gunicorn + Uvicorn workers =====
CMD ["gunicorn", "-w", "2", "-k", "uvicorn.workers.UvicornWorker", "--bind", "0.0.0.0:8000", "--timeout", "120", "app:app"]

