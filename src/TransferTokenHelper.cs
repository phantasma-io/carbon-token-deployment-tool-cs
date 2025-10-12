using PhantasmaPhoenix.Core;
using PhantasmaPhoenix.Core.Extensions;
using PhantasmaPhoenix.Cryptography;
using PhantasmaPhoenix.Protocol.Carbon;
using PhantasmaPhoenix.Protocol.Carbon.Blockchain;
using PhantasmaPhoenix.RPC;

public static class TransferTokenHelper
{
	public static async Task Run(PhantasmaAPI api, string wif, string recipient, string symbol, ulong amount)
	{
		var txSender = PhantasmaKeys.FromWIF(wif);
		var recipientAddress = Address.Parse(recipient);

		var token = await api.GetTokenAsync(symbol) ?? throw new Exception("Token not found");
		if (!token.IsFungible())
		{
			throw new Exception("Token is not fungible");
		}

		var tokenCarbonId = token.CarbonId;
		Console.WriteLine("tokenCarbonId: " + tokenCarbonId);

		Console.WriteLine("amount: " + UnitConversion.ToDecimal(amount, token.Decimals));

		var tx = new TxMsg
		{
			type = TxTypes.TransferFungible,
			expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeMilliseconds(),
			maxGas = 10000000,
			maxData = 1000,
			gasFrom = new Bytes32(txSender.PublicKey),
			payload = new SmallString("phoenix-cs-sdk"),
			msg = new TxMsgTransferFungible
			{
				to = new Bytes32(recipientAddress.GetPublicKey()),
				tokenId = tokenCarbonId,
				amount = amount
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
		await CheckTransactionState.Run(api, txHash, null);
	}
}
