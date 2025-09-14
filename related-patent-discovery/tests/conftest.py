import os
import sys
import pytest
import pytest_asyncio
from httpx import AsyncClient
from httpx import ASGITransport

# Fix path so 'app' can be founddd
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app import app

@pytest_asyncio.fixture
async def client():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as c:
        yield c
