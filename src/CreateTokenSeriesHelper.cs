using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules.Builders;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.TxHelpers;
using PhantasmaPhoenix.RPC;

public static class CreateTokenSeriesHelper
{
	public static async Task<(bool, uint?)> Run(PhantasmaAPI api,
		string wif,
		ulong tokenId,
		ulong maxData,
		ulong gasFeeBase,
		ulong gasFeeCreateTokenSeries,
		ulong feeMultiplier)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);
		var txSenderPubKey = new Bytes32(txSender.PublicKey);

		var newPhantasmaSeriesId = IdHelper.GetRandomId(); // Phantasma series ID

		Console.WriteLine("Creating series with phantasma ID: " + newPhantasmaSeriesId);

		var seriesInfo = SeriesInfoBuilder.Build(newPhantasmaSeriesId,
			0,
			0,
			txSenderPubKey);

		var feeOptions = new CreateSeriesFeeOptions(
			gasFeeBase,
			gasFeeCreateTokenSeries,
			feeMultiplier
		);

		var hexTx = CreateTokenSeriesTxHelper.BuildTxAndSignHex(tokenId,
			seriesInfo,
			txSender,
			feeOptions,
			maxData);

		Console.WriteLine("hexTx: " + hexTx);

		var txHash = await api.SendCarbonTransactionAsync(hexTx);
		if (string.IsNullOrWhiteSpace(txHash))
		{
			throw new("txHash is empty");
		}

		Console.WriteLine("txHash: " + txHash);
		var success = false;
		string? txResult = null;
		await CheckTransactionState.Run(api, txHash, (state, result) => { success = state == ExecutionState.Halt; txResult = result; });

		if (!success || string.IsNullOrWhiteSpace(txResult))
		{
			Console.WriteLine("Series creation failed");
			return (false, null);
		}

		Console.WriteLine("seriesResult: " + txResult);

		uint carbonSeriesId = CreateTokenSeriesTxHelper.ParseResult(txResult);
		return (success, carbonSeriesId);
	}
}
