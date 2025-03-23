// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract PropertyRegistry is ERC721, Ownable {
    
    uint256 public tokenIdCounter;

    struct Property {
        string ipfsHash;
        address owner;
        uint256 timestamp;
    }

    mapping(uint256 => Property) public properties;

    event PropertyRegistered(
        uint256 indexed tokenId,
        address indexed owner,
        string ipfsHash,
        uint256 timestamp
    );

    event PropertyTransferred(
        uint256 indexed tokenId,
        address indexed from,
        address indexed to,
        uint256 timestamp
    );

    constructor() ERC721("PdfPropertyRegistry", "PDFPROP") Ownable(msg.sender) {}

    function registerProperty(string memory ipfsHash, address owner) external returns (uint256) {
        tokenIdCounter++;
        uint256 newTokenId = tokenIdCounter;

        _safeMint(owner, newTokenId);
        properties[newTokenId] = Property(ipfsHash, owner, block.timestamp);

        emit PropertyRegistered(newTokenId, owner, ipfsHash, block.timestamp);

        return newTokenId;
    }

    function transferProperty(address to, uint256 tokenId) external {
        require(ownerOf(tokenId) == msg.sender, "You are not the property owner");
        _transfer(msg.sender, to, tokenId);

        properties[tokenId].owner = to;
        emit PropertyTransferred(tokenId, msg.sender, to, block.timestamp);
    }

    function getPropertyDetails(uint256 tokenId) external view returns (Property memory) {
        require(tokenId > 0 && tokenId <= tokenIdCounter, "Property does not exist");
        return properties[tokenId];
    }
}
