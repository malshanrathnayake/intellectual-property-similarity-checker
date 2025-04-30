# Decentralized Intellectual Property Protection Platform with AI-Powered Similarity Detection

This repository contains the codebase for the **Decentralized Intellectual Property Protection Platform with AI-Powered Similarity Detection**, a comprehensive solution designed to protect intellectual property (IP) rights through blockchain integration and advanced AI-driven similarity checking.

## Introduction

The rising prevalence of digital content has amplified the risk of IP infringements. Traditional systems, often centralized and manual, fail to efficiently handle modern challenges. This platform provides a decentralized, secure, and automated IP protection solution, leveraging blockchain and sophisticated AI algorithms.

## Core Components

### 1. Blockchain Integration with Similarity Checker

**Responsible:** Malshan Rathnayake

**Objective:**  
Integrate blockchain technology to facilitate secure and immutable IP registration, manage IP ownership transfers, and automate smart contract execution.

**Key Features:**
- Immutable IP registration and ownership tracking
- Smart contract-enabled IP transaction automation
- Decentralized storage of metadata on Ethereum blockchain

**Technologies Used:**
- Ethereum blockchain (Solidity smart contracts, Hardhat)
- Moralis for wallet authentication
- IPFS via Pinata for decentralized data storage
- ASP.NET MVC CoreUI Dashboard

---

### 2. Similarity Checking for Text-Based Content (Books/Novels) and Continuous Model Learning

**Responsible:** Shan Dilhara Wanigasuriya

**Objective:**  
Develop advanced semantic similarity checking models to identify plagiarism and paraphrased content in books and novels with continuous learning capabilities.

**Key Features:**
- Semantic similarity detection using transformer-based language models
- Identification of paraphrased and contextually rewritten content
- Continuous adaptive learning through fine-tuning on textual datasets

**Technologies Used:**
- Python (Flask/Django for APIs)
- SentenceTransformers (text embeddings)
- FAISS (vector similarity search)
- NLTK (Natural Language Processing Toolkit)

---

### 3. Related Patent Discovery

**Responsible:** Chamodi Liyanage

**Objective:**  
Automatically identify semantically similar patents to ensure novelty of patent applications, enhancing efficiency and accuracy in novelty checks.

**Key Features:**
- Semantic analysis and ranking of patents based on abstract, title, and claims
- Robust PDF text extraction with OCR fallback
- Decentralized storage and retrieval using IPFS and blockchain

**Technologies Used:**
- FastAPI (backend)
- SentenceTransformers and FAISS (semantic search)
- PyMuPDF and Tesseract OCR (text extraction)
- IPFS for decentralized storage
- Ethereum blockchain for secure storage of metadata

---

### 4. Similarity Checking for Video-Based Content and Continuous Model Learning

**Responsible:** Jehan Silva

**Objective:**  
Build a decentralized, real-time video similarity checking system with adaptive AI capabilities, ensuring scalable and secure video IP protection.

**Key Features:**
- Real-time video similarity detection using deep learning models
- Continuous learning and adaptation via federated learning techniques
- Blockchain-based IP registration for video content

**Technologies Used:**
- PyTorch/TensorFlow (AI model development)
- Video similarity models (C3D, I3D, TimeSformer)
- IPFS decentralized storage
- Solidity smart contracts (via Hardhat and Alchemy)
- React.js frontend

---

## Project Status

- **Blockchain Integration:** ✅ Completed  
- **Text Similarity Checker:** ✅ Completed  
- **Patent Similarity Checker:** ✅ Completed  
- **Video Similarity Checker:** ✅ Completed

---

## Next Steps

- Legal validation of resources  
- Ownership transfer and patent verification processes  
- Model fine-tuning for enhanced accuracy  
- UI/UX enhancements

---

## Contributors

- **Malshan Rathnayake** (Blockchain Integration)  
- **Shan Dilhara Wanigasuriya** (Text Similarity Checker)  
- **Chamodi Liyanage** (Patent Discovery)  
- **Jehan Silva** (Video Similarity Checker)

---

## Supervisors

- **Dr. Dharshana Kasthurirathna** (Supervisor)  
- **Dr. Kalpani Manathunga** (Co-supervisor)

---

## Usage

### Clone the repository
```bash
git clone https://github.com/malshanrathnayake/intellectual-property-similarity-checker.git
```

### Setup & Run
```bash
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --reload
```

### License
This project is licensed under the MIT License. See the LICENSE file for details.

