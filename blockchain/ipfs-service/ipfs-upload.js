// ipfs-upload.js
const { create } = require('ipfs-http-client');
const fs = require('fs');

const client = create('https://ipfs.infura.io:5001/api/v0');

async function uploadFile(filePath) {
    const file = fs.readFileSync(filePath);
    const { path } = await client.add(file);
    console.log("Uploaded to IPFS:", path);
    return path;
}

uploadFile('path/to/your/file.pdf');
