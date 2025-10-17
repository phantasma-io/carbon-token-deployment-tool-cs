using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.Modules.Builders;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain.TxHelpers;
using PhantasmaPhoenix.RPC;

public static class CreateTokenHelper
{
	public static async Task<(bool, ulong?)> Run(PhantasmaAPI api,
		string wif,
		string symbol,
		Dictionary<string, string> fields,
		ulong maxData,
		ulong gasFeeBase,
		ulong gasFeeCreateTokenBase,
		ulong gasFeeCreateTokenSymbol,
		ulong feeMultiplier)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);
		var txSenderPubKey = new Bytes32(txSender.PublicKey);

		var tokenInfo = TokenInfoBuilder.Build(symbol,
			new IntX(0),
			true,
			0,
			txSenderPubKey,
			TokenMetadataBuilder.BuildAndSerialize(fields));

		var feeOptions = new CreateTokenFeeOptions(
			gasFeeBase,
			gasFeeCreateTokenBase,
			gasFeeCreateTokenSymbol,
			feeMultiplier
		);

		var hexTx = CreateTokenTxHelper.BuildTxAndSignHex(tokenInfo,
			txSender,
			feeOptions,
			maxData,
			null);

		Console.WriteLine("hexTx: " + hexTx);

		var txHash = await api.SendCarbonTransactionAsync(hexTx);
		if (string.IsNullOrWhiteSpace(txHash))
		{
			throw new("txHash is empty");
		}

		Console.WriteLine("txHash: " + txHash);
		bool success = false;
		string? txResult = null;
		await CheckTransactionState.Run(api, txHash, (state, result) => { success = state == ExecutionState.Halt; txResult = result; });

		if (!success || string.IsNullOrWhiteSpace(txResult))
		{
			Console.WriteLine("Token creation failed");
			return (false, null);
		}

		uint carbonTokenId = CreateTokenSeriesTxHelper.ParseResult(txResult);
		return (success, carbonTokenId);
	}
}
