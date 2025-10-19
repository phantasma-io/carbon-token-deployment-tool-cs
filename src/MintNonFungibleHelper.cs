using System.Numerics;
using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules.Builders;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.TxHelpers;
using PhantasmaPhoenix.RPC;

public static class MintNonFungibleHelper
{
	public static async Task<bool> Run(PhantasmaAPI api,
		string wif,
		ulong carbonTokenId,
		uint carbonSeriesId,
		string name,
		string description,
		string imageURL,
		string infoURL,
		uint royalties,
		string? romHex,
		ulong maxData,
		ulong gasFeeBase,
		ulong feeMultiplier)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);

		BigInteger phantasmaId = IdHelper.GetRandomId(); // Arbitrary phantasma ID

		Console.WriteLine("Minting NFT with phantasma ID: " + phantasmaId);

		byte[] phantasmaRomData = []; // [0x01, 0x42]; // todo - arbitrary / TOMB data

		if (!string.IsNullOrWhiteSpace(romHex))
		{
			phantasmaRomData = Convert.FromHexString(romHex);
		}

		var rom = NftRomBuilder.BuildAndSerialize(phantasmaId,
			name,
			description,
			imageURL,
			infoURL,
			royalties,
			phantasmaRomData,
			null);

		var feeOptions = new MintNftFeeOptions(
			gasFeeBase,
			feeMultiplier
		);

		var hexTx = MintNonFungibleTxHelper.BuildTxAndSignHex(
			carbonTokenId,
			carbonSeriesId,
			txSender,
			new Bytes32(txSender.PublicKey),
			rom,
			Array.Empty<byte>(),
			feeOptions,
			maxData
		);

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
