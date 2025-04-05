import express from 'express';
import { create } from 'ipfs-http-client';
import { Wallet, Contract, JsonRpcProvider } from 'ethers';
import cors from 'cors';
import fs from 'fs';
import fileUpload from 'express-fileupload';
import 'dotenv/config';

const app = express();
app.use(express.json());
app.use(cors());
app.use(fileUpload());

// IPFS setup (using Infura)
// const ipfs = create({ url: 'https://ipfs.infura.io:5001/api/v0' });

const ipfs = create({
  host: '127.0.0.1',
  port: 5001,
  protocol: 'http'
});


// Ethereum setup (via Moralis or Infura)
// const provider = new ethers.providers.JsonRpcProvider('https://sepolia.infura.io/v3/7b3fbb83265547e7a0b4de05e8ce5a7a');
const provider = new JsonRpcProvider(`https://sepolia.infura.io/v3/${process.env.INFURA_API_KEY}`);

const privateKey = `${process.env.PRIVATE_KEY}`;
// const wallet = new ethers.Wallet(privateKey, provider);
const wallet = new Wallet(privateKey, provider);



// const contractABI = JSON.parse(fs.readFileSync('contract-abi.json'));
const contractJson = JSON.parse(fs.readFileSync('contract-abi.json'));
const contractABI = contractJson.abi;

const contractAddress = `${process.env.CONTRACT_ADDRESS}`;//npx hardhat compile && npx hardhat run scripts/deploy.js --network sepolia 
// const contract = new ethers.Contract(contractAddress, contractABI, wallet);
const contract = new Contract(contractAddress, contractABI, wallet);




// IPFS upload route
app.post('/ipfs/upload', async (req, res) => {
    const fileBuffer = req.files.file.data; // Use file upload middleware (express-fileupload or multer)
    const result = await ipfs.add(fileBuffer);
    res.json({ hash: result.path });
});

// Blockchain registration route
app.post('/blockchain/register', async (req, res) => {
    const { ipfsHash, walletAddress } = req.body;

    try {
        // Correctly pass the walletAddress as the `owner` param
        const tx = await contract.registerProperty(ipfsHash, walletAddress);
        console.log("📤 Transaction sent:", tx.hash);

        await tx.wait();

        // Get latest tokenId
        const tokenId = await contract.tokenIdCounter();
        console.log("📤 Token:", tokenId.toString());
        res.json({ tokenId: tokenId.toString() });

    } catch (error) {
        console.error("Blockchain registration failed:");
        console.error("Message:", error.message);
        console.error("Stack:", error.stack);
        res.status(500).json({ error: error.message });
    }
});



app.get('/ipfs/test', async (req, res) => {
    try {
      const result = await ipfs.add('Hello from local IPFS node!');
      res.json({ hash: result.path });
    } catch (e) {
      res.status(500).json({ error: e.message });
    }
});  
  
app.get('/blockchain/tokens/:walletAddress', async (req, res) => {
  const walletAddress = req.params.walletAddress;
  const tokenCount = await contract.balanceOf(walletAddress);

  let tokens = [];

  //track the total supply and loop over all tokens safely
  const maxTokenId = await contract.tokenIdCounter();

  for (let tokenId = 1; tokenId <= maxTokenId; tokenId++) {
    try {
      const owner = await contract.ownerOf(tokenId);
      if (owner.toLowerCase() === walletAddress.toLowerCase()) {
        const property = await contract.getPropertyDetails(tokenId);
        tokens.push({
          tokenId: tokenId.toString(),
          ipfsHash: property.ipfsHash,
          owner: property.owner,
          timestamp: property.timestamp.toString()
        });
      }
    } catch (err) {
      continue;
    }
  }

  res.json(tokens);
});


app.post('/blockchain/transfer', async (req, res) => {
  const { from, to, tokenId } = req.body;

  try {
    const tx = await contract.transferProperty(to, tokenId);
    await tx.wait();

    res.json({ success: true, txHash: tx.hash });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.get('/blockchain/history/:tokenId', async (req, res) => {
  const tokenId = req.params.tokenId;

  try {
    const filter = contract.filters.PropertyTransferred(tokenId, null, null);
    const events = await contract.queryFilter(filter, 0, "latest");

    const history = events.map(event => ({
      from: event.args.from,
      to: event.args.to,
      tokenId: event.args.tokenId.toString(),
      timestamp: event.args.timestamp.toString(),
      txHash: event.transactionHash
    }));

    res.json(history);
  } catch (error) {
    console.error("Failed to fetch transfer history:", error.message);
    res.status(500).json({ error: error.message });
  }
});


app.listen(4000, () => console.log("Blockchain service running on port 4000"));
