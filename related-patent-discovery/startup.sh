#!/bin/bash
echo "========== INSTALLING SYSTEM DEPENDENCIES =========="
apt-get update -y
apt-get install -y poppler-utils tesseract-ocr

echo "========== STARTING FASTAPI APP =========="
gunicorn -w 2 -k uvicorn.workers.UvicornWorker --timeout 120 app:app
