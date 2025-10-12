using PhantasmaPhoenix.RPC;

public static class CarbonTokenTransfer
{
	public static async Task Run(PhantasmaAPI api, string wif)
	{
		var symbol = Environment.GetEnvironmentVariable("SYMBOL");
		if (string.IsNullOrWhiteSpace(symbol))
		{
			throw new("SYMBOL not set in env");
		}

		var recipient = Environment.GetEnvironmentVariable("RECIPIENT");
		if (string.IsNullOrWhiteSpace(recipient))
		{
			throw new("RECIPIENT not set in env");
		}

		var amount = Environment.GetEnvironmentVariable("AMOUNT");
		if (string.IsNullOrWhiteSpace(amount))
		{
			throw new("AMOUNT not set in env");
		}

		await TransferTokenHelper.Run(api, wif, recipient, symbol, ulong.Parse(amount));
	}
}
