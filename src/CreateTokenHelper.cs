using PhantasmaPhoenix.Core;
using PhantasmaPhoenix.Core.Extensions;
using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Vm;
using PhantasmaPhoenix.RPC;

public static class CreateTokenHelper
{
	public static TokenSchemas PrepareTokenSchemas()
	{
		TokenSchemas tokenSchemas = new TokenSchemas
		{
			// Variables that every new NFT-series must supply:
			seriesMetadata = new VmStructSchema
			{
				// Every series must have: BigInteger ID; Byte mode; Byte[] phantasmaRom;
				fields = [
					new VmNamedVariableSchema {
						name = StandardMeta.id,
						schema = new VmVariableSchema{ type = VmType.Int256 }
					},
					new VmNamedVariableSchema { // Phantasma feature: Unique or duplicate series type
						name = new SmallString("mode"),
						schema = new VmVariableSchema{ type = VmType.Int8 }
					},
					new VmNamedVariableSchema { // Phantasma feature: If this is a duplicated series, store the duplicated ROM here:
						name = new SmallString("rom"),
						schema = new VmVariableSchema{ type = VmType.Bytes }
					}
				],
				flags = VmStructSchema.Flags.None
			},

			// Variables that every mint must supply:
			rom = new VmStructSchema
			{
				// Every NFT must have: Int256 ID; Byte[] phantasmaRom;
				fields = [
					new VmNamedVariableSchema {
						name = StandardMeta.id,
						schema = new VmVariableSchema{ type = VmType.Int256 }
					},
					new VmNamedVariableSchema { // Phantasma feature: If this is NOT a duplicated series, store the individual ROM here:
						name = new SmallString("rom"),
						schema = new VmVariableSchema{ type = VmType.Bytes }
					}
				],
				flags = VmStructSchema.Flags.None
			},

			// Variables that can be updated after minting:
			ram = new VmStructSchema { fields = [], flags = VmStructSchema.Flags.DynamicExtras },
			// ^ Leave this as dynamic so users can put anything there
		};

		return tokenSchemas;
	}
	public static async Task Run(PhantasmaAPI api,
		string wif,
		TokenSchemas tokenSchemas,
		string symbol,
		Dictionary<string, string> fields,
		ulong maxData,
		ulong gasFeeBase,
		ulong gasFeeCreateTokenBase,
		ulong gasFeeCreateTokenSymbol)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);

		VmNamedDynamicVariable[] metadataFields = [];
		foreach (var f in fields)
		{
			metadataFields = metadataFields.Append(new VmNamedDynamicVariable
			{
				name = new SmallString(f.Key),
				value = new VmDynamicVariable(f.Value)
			}).ToArray();
		}

		// Create a carbon structure for the token metadata
		using MemoryStream metadataBuffer = new();
		using BinaryWriter wMetadata = new(metadataBuffer);
		wMetadata.Write(new VmDynamicStruct
		{
			fields = metadataFields
		});
		// ^ There's no standard for token metadata field names yet!

		// Create a carbon structure to describe the schema for these tokens -- i.e. the variables our NFTs will have
		using MemoryStream schemaBuffer = new();
		using BinaryWriter wSchemas = new(schemaBuffer);
		wSchemas.Write(tokenSchemas);

		using MemoryStream argsBuffer = new();
		using BinaryWriter wArgs = new(argsBuffer);
		wArgs.Write(new TokenInfo
		{
			maxSupply = new IntX(0),
			flags = TokenFlags.NonFungible,
			decimals = 0, // only use non-zero for fungibles
			owner = new Bytes32(txSender.PublicKey),
			symbol = new SmallString(symbol), // symbol is optional, but required for current Phantasma API support
			metadata = metadataBuffer.ToArray(), // optional.
			tokenSchemas = schemaBuffer.ToArray() // optional. Should be empty for fungible tokens.
		});

		TxMsg tx = new TxMsg
		{
			type = TxTypes.Call, // Generic transaction type - Call a single function
			expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeMilliseconds(),
			maxGas = (gasFeeBase + gasFeeCreateTokenBase + (gasFeeCreateTokenSymbol >> (symbol.Length - 1))) * 10000,
			maxData = maxData,
			gasFrom = new Bytes32(txSender.PublicKey),
			payload = SmallString.Empty,
			msg = new TxMsgCall
			{
				moduleId = (uint)ModuleId.Token, // Call a method in the "token" module
				methodId = (uint)TokenContract_Methods.CreateToken,// Call the CreateToken method
				args = argsBuffer.ToArray() // CreateToken expects a 'TokenInfo' structure as the argument
			}
		};
		Console.WriteLine("maxGas: " + UnitConversion.ToDecimal(tx.maxGas, 10));

		var signedTxMsg = new SignedTxMsg
		{
			msg = tx,
			witnesses = new Witness[] {new Witness
							{
								address = new Bytes32(txSender.PublicKey),
								signature = new Bytes64(Ed25519.Sign(CarbonBlob.Serialize(tx), txSender.PrivateKey))
							}}
		};

		var carbonTx = CarbonBlob.Serialize(signedTxMsg);
		var hexTx = carbonTx.ToHex();
		Console.WriteLine("hexTx: " + hexTx);

		var txHash = await api.SendCarbonTransactionAsync(hexTx);
		if (string.IsNullOrWhiteSpace(txHash))
		{
			throw new("txHash is empty");
		}

		Console.WriteLine("txHash: " + txHash);
		await CheckTransactionState.Run(api, txHash, null);
	}
}
