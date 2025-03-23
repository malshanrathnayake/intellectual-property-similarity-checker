const hre = require("hardhat");

async function main() {
  const PropertyRegistry = await hre.ethers.getContractFactory("PropertyRegistry");
  const propertyRegistry = await PropertyRegistry.deploy();

  // ✅ Wait for deployment to finish
  await propertyRegistry.waitForDeployment();

  // ✅ Get the deployed address
  const address = await propertyRegistry.getAddress();

  console.log("PropertyRegistry deployed at:", address);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
