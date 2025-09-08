from web3 import Web3
import os
from dotenv import load_dotenv

load_dotenv()

# Infura + Sepolia connection
INFURA_PROJECT_ID = os.getenv("INFURA_PROJECT_ID")
PRIVATE_KEY = os.getenv("PRIVATE_KEY")  # Optional, for write access

provider_url = f"https://sepolia.infura.io/v3/{INFURA_PROJECT_ID}"
web3 = Web3(Web3.HTTPProvider(provider_url))

# Set your latest deployed contract address and ABI
CONTRACT_ADDRESS = Web3.to_checksum_address("0xeF036C42eD1b35D8488d0719Bc713530e99F2Ea7")

ABI = [
    {
        "inputs": [
            {"internalType": "uint256", "name": "patentId", "type": "uint256"},
            {"internalType": "string", "name": "_hash", "type": "string"}
        ],
        "name": "storeHash",
        "outputs": [],
        "stateMutability": "nonpayable",
        "type": "function"
    },
    {
        "inputs": [
            {"internalType": "uint256", "name": "patentId", "type": "uint256"}
        ],
        "name": "getHash",
        "outputs": [{"internalType": "string", "name": "", "type": "string"}],
        "stateMutability": "view",
        "type": "function"
    }
]

# Initialize contract
contract = web3.eth.contract(address=CONTRACT_ADDRESS, abi=ABI)

def get_cid_from_blockchain(patent_id: int) -> str:
    try:
        return contract.functions.getHash(patent_id).call()
    except Exception as e:
        print(f"Error getting CID from blockchain: {e}")
        return None

def store_cid_on_blockchain(patent_id: int, cid: str) -> str:
    try:
        account = web3.eth.account.from_key(PRIVATE_KEY)
        nonce = web3.eth.get_transaction_count(account.address)

        txn = contract.functions.storeHash(patent_id, cid).build_transaction({
            "from": account.address,
            "nonce": nonce,
            "gas": 200000,
            "maxFeePerGas": web3.to_wei("50", "gwei"),
            "maxPriorityFeePerGas": web3.to_wei("2", "gwei"),
            "chainId": 11155111
        })

        signed_txn = web3.eth.account.sign_transaction(txn, private_key=PRIVATE_KEY)
        txn_hash = web3.eth.send_raw_transaction(signed_txn.rawTransaction)

        print(f"[INFO] Tx sent: {txn_hash.hex()}")
        receipt = web3.eth.wait_for_transaction_receipt(txn_hash, timeout=120)
        print(f"[INFO] Tx mined in block {receipt.blockNumber}")

        return txn_hash.hex()

    except Exception as e:
        print("Error storing CID to blockchain:", e)
        return None


# def store_cid_on_blockchain(patent_id: int, cid: str) -> str:
#     try:
#         account = web3.eth.account.from_key(PRIVATE_KEY)
#         nonce = web3.eth.get_transaction_count(account.address)

#         txn = contract.functions.storeHash(patent_id, cid).build_transaction({
#             "from": account.address,
#             "nonce": nonce,
#             "gas": 200000,
#             "gasPrice": web3.to_wei("10", "gwei"),
#             "chainId": 11155111  # Sepolia
#         })

#         signed_txn = web3.eth.account.sign_transaction(txn, private_key=PRIVATE_KEY)
#         txn_hash = web3.eth.send_raw_transaction(signed_txn.rawTransaction)

#         return txn_hash.hex()

#     except Exception as e:
#         print("Error storing CID to blockchain:", e)
#         return None

