using System.Numerics;
using PhantasmaPhoenix.Core.Extensions;
using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Vm;
using PhantasmaPhoenix.RPC;

public static class MintNonFungibleHelper
{
	public static async Task<bool> Run(PhantasmaAPI api,
		string wif,
		ulong carbonTokenId,
		TokenSchemas tokenSchemas,
		uint carbonSeriesId,
		string? romHex,
		ulong maxData,
		ulong gasFeeBase)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);

		BigInteger phantasmaId = IdHelper.GetRandomId(); // Arbitrary phantasma ID

		Console.WriteLine("Minting NFT with phantasma ID: " + phantasmaId);

		byte[] phantasmaRomData = []; // [0x01, 0x42]; // todo - arbitrary / TOMB data

		if (!string.IsNullOrWhiteSpace(romHex))
		{
			phantasmaRomData = Convert.FromHexString(romHex);
		}

		// Write out the variables that are expected for a new NFT instance (encoded with respect to the rom schema used when creating the token)
		using MemoryStream romBuffer = new();
		using BinaryWriter wRom = new(romBuffer);
		new VmDynamicStruct
		{
			fields = [
				new VmNamedDynamicVariable{ name = StandardMeta.id, value = new VmDynamicVariable(phantasmaId) },
				new VmNamedDynamicVariable{ name = new SmallString("rom"), value = new VmDynamicVariable(phantasmaRomData) },
			]
		}.Write(tokenSchemas.rom, wRom);

		TxMsg tx = new TxMsg
		{
			type = TxTypes.MintNonFungible, // Specialized minting TX
			expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeMilliseconds(),
			maxGas = gasFeeBase * 1000,
			maxData = maxData,
			gasFrom = new Bytes32(txSender.PublicKey),
			payload = SmallString.Empty,
			msg = new TxMsgMintNonFungible
			{
				tokenId = carbonTokenId,
				seriesId = carbonSeriesId,
				to = new Bytes32(txSender.PublicKey),
				rom = romBuffer.ToArray(),
				ram = []
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
		await CheckTransactionState.Run(api, txHash, (state, _) => { success = state == ExecutionState.Halt; });

		return success;
	}
}
