using PhantasmaPhoenix.Core.Extensions;
using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Vm;
using PhantasmaPhoenix.RPC;

public static class CreateTokenSeriesHelper
{
	public static async Task<(bool, uint?)> Run(PhantasmaAPI api,
		string wif,
		ulong tokenId,
		TokenSchemas tokenSchemas,
		ulong maxData,
		ulong gasFeeBase,
		ulong gasFeeCreateTokenSeries)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);

		var newPhantasmaSeriesId = IdHelper.GetRandomId(); // Phantasma series ID
		byte[] sharedRom = [];// todo

		// Write out the variables that are expected for a new series (encoded with respect to the seriesMetadataSchema used when creating the token)
		using MemoryStream metadataBuffer = new();
		using BinaryWriter wMetadata = new(metadataBuffer);
		new VmDynamicStruct
		{
			fields = [
				new VmNamedDynamicVariable{ name = StandardMeta.id, value = new VmDynamicVariable(newPhantasmaSeriesId) },
				new VmNamedDynamicVariable{ name = new SmallString("mode"), value = new VmDynamicVariable((byte)(sharedRom.Length == 0 ? 0 : 1)) },
				new VmNamedDynamicVariable{ name = new SmallString("rom"), value = new VmDynamicVariable(sharedRom) },
			]
		}.Write(tokenSchemas.seriesMetadata, wMetadata);

		// CreateTokenSeries expects (int64, SeriesInfo) args
		using MemoryStream argsBuffer = new();
		using BinaryWriter wArgs = new(argsBuffer);
		wArgs.Write8(tokenId);
		wArgs.Write(new SeriesInfo
		{
			maxMint = 0, // limit on minting, or 0=no limit
			maxSupply = 0, // limit on how many can exist at once
			owner = new Bytes32(txSender.PublicKey),
			metadata = metadataBuffer.ToArray(), // VmDynamicStruct encoded with TokenInfo.tokenSchemas.seriesMetadata
			rom = new VmStructSchema { fields = [], flags = VmStructSchema.Flags.None },
			ram = new VmStructSchema { fields = [], flags = VmStructSchema.Flags.None },
		});

		TxMsg tx = new TxMsg
		{
			type = TxTypes.Call, // Generic transaction type - Call a single function
			expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeMilliseconds(),
			maxGas = (gasFeeBase + gasFeeCreateTokenSeries) * 10000,
			maxData = maxData,
			gasFrom = new Bytes32(txSender.PublicKey),
			payload = SmallString.Empty,
			msg = new TxMsgCall
			{
				moduleId = (uint)ModuleId.Token, // Call a method in the "token" module
				methodId = (uint)TokenContract_Methods.CreateTokenSeries,// Call the CreateTokenSeries method
				args = argsBuffer.ToArray() // CreateTokenSeries expects (int64, SeriesInfo) args
			}
		};

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
		var success = false;
		string? seriesResult = null;
		await CheckTransactionState.Run(api, txHash, (state, result) => { success = state == ExecutionState.Halt; seriesResult = result; });

		if (!success || string.IsNullOrWhiteSpace(seriesResult))
		{
			Console.WriteLine("Series creation failed");
			return (false, null);
		}

		Console.WriteLine("seriesResult: " + seriesResult);

		uint carbonSeriesId;
		using (var s = new MemoryStream(Convert.FromHexString(seriesResult)))
		using (var r = new BinaryReader(s))
		{
			BinaryStreamExt.Read4(r, out carbonSeriesId);
		}

		return (success, carbonSeriesId);
	}
}
