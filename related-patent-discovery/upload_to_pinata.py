import requests
import os
from dotenv import load_dotenv

load_dotenv()

PINATA_API_KEY = os.getenv("PINATA_API_KEY")
PINATA_SECRET_API_KEY = os.getenv("PINATA_SECRET_API_KEY")

def upload_file_to_pinata(file_path):
    url = "https://api.pinata.cloud/pinning/pinFileToIPFS"
    headers = {
        "pinata_api_key": PINATA_API_KEY,
        "pinata_secret_api_key": PINATA_SECRET_API_KEY
    }

    with open(file_path, "rb") as f:
        files = {'file': (os.path.basename(file_path), f)}
        response = requests.post(url, files=files, headers=headers)

    print("Status:", response.status_code)
    print("Response:", response.text)

    if response.status_code == 200:
        ipfs_hash = response.json()["IpfsHash"]
        return f"https://gateway.pinata.cloud/ipfs/{ipfs_hash}"
    else:
        return None

if __name__ == "__main__":
    file_path = "example-patent.pdf"
    if not os.path.exists(file_path):
        print("File not found.")
    else:
        link = upload_file_to_pinata(file_path)
        if link:
            print("Uploaded to IPFS via Pinata!")
            print("🔗", link)
        else:
            print("Upload failed.")
