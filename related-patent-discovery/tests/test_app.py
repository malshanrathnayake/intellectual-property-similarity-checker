import pytest
from unittest.mock import patch, MagicMock
from io import BytesIO
import numpy as np

# -------------------------------
# Test: GET /search
# -------------------------------
@pytest.mark.asyncio
async def test_search(client):
    with patch("app.index.search", return_value=(np.array([[0.5]]), np.array([[0]]))):
        response = await client.get("/search", params={"query": "neural network"})
        assert response.status_code == 200
        json_data = response.json()
        assert "results" in json_data
        assert isinstance(json_data["results"], list)


# -------------------------------
# Test: POST /register
# -------------------------------
@pytest.mark.asyncio
async def test_register(client):
    with patch("app.index.search", return_value=(np.array([[1.1]]), np.array([[0]]))):
        payload = {
            "title": "Test Patent",
            "abstract": "An abstract on AI models.",
            "claims": ["1. A system that learns...", "2. The method of claim 1..."]
        }
        response = await client.post("/register", json=payload)
        assert response.status_code == 200
        json_data = response.json()
        assert "status" in json_data
        assert json_data["status"] in ["approved", "rejected"]


# -------------------------------
# Test: GET /cid/{id}
# -------------------------------
@pytest.mark.asyncio
async def test_read_cid(client):
    with patch("app.get_cid_from_blockchain", return_value="mockedCID"):
        response = await client.get("/cid/123")
        assert response.status_code == 200
        assert response.json()["cid"] == "mockedCID"

# -------------------------------
# Test: GET /registered
# -------------------------------
@pytest.mark.asyncio
async def test_get_recent_patents(client):
    with patch("app.get_cid_from_blockchain", return_value="mockedCID"):
        response = await client.get("/registered")
        assert response.status_code == 200
        data = response.json()
        assert "patents" in data
        assert isinstance(data["patents"], list)
        if data["patents"]:
            assert "id" in data["patents"][0]

# -------------------------------
# Test: POST /register/pdf
# -------------------------------
@pytest.mark.asyncio
async def test_register_pdf(client):
    dummy_pdf_bytes = b"%PDF-1.4 dummy pdf for testing"

    with patch("app.extract_patent_sections", return_value={
        "title": "Mock Title",
        "abstract": "Mock abstract text.",
        "claims": ["1. Mock claim", "2. Another claim"],
        "source": "mock"
    }), patch("app.get_cid_from_blockchain", return_value="mockCID"), \
         patch("app.upload_file_to_pinata", return_value="mockPDFCID"), \
         patch("app.upload_json_to_pinata", return_value="mockMetaCID"), \
         patch("app.store_cid_on_blockchain", return_value="mockTXHash"), \
         patch("app.index.search", return_value=(np.array([[1.1]]), np.array([[0]]))):

        files = {"file": ("test.pdf", BytesIO(dummy_pdf_bytes), "application/pdf")}
        response = await client.post("/register/pdf", files=files)

        assert response.status_code == 200
        json_data = response.json()

        # Safe check
        assert "success" in json_data, f"Missing 'success' key in response: {json_data}"

        if json_data["success"]:
            assert json_data["status"] in ["approved", "rejected"]
        else:
            assert "message" in json_data


