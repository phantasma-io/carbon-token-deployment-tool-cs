# Carbon Token Deployment Tool (CS)
Tool to deploy Carbon tokens based on CS SDK

## How to use

Copy env.default to .env and set following fields:

RPC: URL of testnet/mainnet or local Phantasma RPC
WIF: WIF of deployer's wallet
SYMBOL: Symbol of new token
ROM: ROM of new token in base16 encoding, can be empty
METADATA_FIELDS: Dictionary with NFT metadata fields (see env.default)

Various fees and limits, tweak for your needs or leave default values:
CREATE_TOKEN_MAX_DATA=1000000000
CREATE_TOKEN_SERIES_MAX_DATA=100000000
MINT_TOKEN_MAX_DATA=100000000
GAS_FEE_BASE=10000
GAS_FEE_CREATE_TOKEN_BASE=10000000000
GAS_FEE_CREATE_TOKEN_SYMBOL=10000000000
GAS_FEE_CREATE_TOKEN_SERIES=2500000000

