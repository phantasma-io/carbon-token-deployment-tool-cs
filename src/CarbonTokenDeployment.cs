using Newtonsoft.Json;
using PhantasmaPhoenix.RPC;
using PhantasmaPhoenix.RPC.Models;

public static class CarbonTokenDeployment
{
	public static async Task Run(PhantasmaAPI api, string wif)
	{
		var symbol = Environment.GetEnvironmentVariable("SYMBOL");
		if (string.IsNullOrWhiteSpace(symbol))
		{
			throw new("SYMBOL not set in env");
		}

		var metadataFieldsJson = Environment.GetEnvironmentVariable("METADATA_FIELDS");
		if (string.IsNullOrWhiteSpace(metadataFieldsJson))
		{
			throw new("METADATA_FIELDS not set in env");
		}

		Dictionary<string, string>? metadataFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(metadataFieldsJson);
		if (metadataFields == null)
		{
			throw new("Could not deserialize METADATA_FIELDS");
		}

		var tokenSchemas = CreateTokenHelper.PrepareTokenSchemas();

		var createTokenMaxData = Environment.GetEnvironmentVariable("CREATE_TOKEN_MAX_DATA");
		if (string.IsNullOrWhiteSpace(createTokenMaxData))
		{
			throw new("CREATE_TOKEN_MAX_DATA not set in env");
		}

		var gasFeeBase = Environment.GetEnvironmentVariable("GAS_FEE_BASE");
		if (string.IsNullOrWhiteSpace(gasFeeBase))
		{
			throw new("GAS_FEE_BASE not set in env");
		}

		var gasFeeCreateTokenBase = Environment.GetEnvironmentVariable("GAS_FEE_CREATE_TOKEN_BASE");
		if (string.IsNullOrWhiteSpace(gasFeeCreateTokenBase))
		{
			throw new("GAS_FEE_CREATE_TOKEN_BASE not set in env");
		}

		var gasFeeCreateTokenSymbol = Environment.GetEnvironmentVariable("GAS_FEE_CREATE_TOKEN_SYMBOL");
		if (string.IsNullOrWhiteSpace(gasFeeCreateTokenSymbol))
		{
			throw new("GAS_FEE_CREATE_TOKEN_SYMBOL not set in env");
		}

		await CreateTokenHelper.Run(api,
			wif,
			tokenSchemas,
			symbol,
			metadataFields,
			ulong.Parse(createTokenMaxData),
			ulong.Parse(gasFeeBase),
			ulong.Parse(gasFeeCreateTokenBase),
			ulong.Parse(gasFeeCreateTokenSymbol));

		TokenResult? token = null;
		var attemptsLeft = 30;
		for (; ; )
		{
			try
			{
				token = await api.GetTokenAsync(symbol);
			}
			catch
			{
				if (attemptsLeft == 0)
				{
					break;
				}

				await Task.Delay(1000);
				attemptsLeft--;
				continue;
			}

			if (token == null)
			{
				if (attemptsLeft == 0)
				{
					break;
				}

				await Task.Delay(1000);
				attemptsLeft--;
				continue;
			}

			break;
		}
		if (token == null)
		{
			throw new Exception("Token information not available");
		}

		var carbonTokenId = token.CarbonId;

		var createTokenSeriesMaxData = Environment.GetEnvironmentVariable("CREATE_TOKEN_SERIES_MAX_DATA");
		if (string.IsNullOrWhiteSpace(createTokenSeriesMaxData))
		{
			throw new("CREATE_TOKEN_SERIES_MAX_DATA not set in env");
		}

		var gasFeeCreateTokenSeries = Environment.GetEnvironmentVariable("GAS_FEE_CREATE_TOKEN_SERIES");
		if (string.IsNullOrWhiteSpace(gasFeeCreateTokenSeries))
		{
			throw new("GAS_FEE_CREATE_TOKEN_SERIES not set in env");
		}

		var (success, seriesId) = await CreateTokenSeriesHelper.Run(api,
			wif,
			carbonTokenId,
			tokenSchemas,
			ulong.Parse(createTokenSeriesMaxData),
			ulong.Parse(gasFeeBase),
			ulong.Parse(gasFeeCreateTokenSeries));

		if (!success || seriesId == null)
		{
			throw new Exception("Could not create NFT series");
		}

		Console.WriteLine("seriesId result: " + seriesId);

		var rom = Environment.GetEnvironmentVariable("ROM");

		var mintTokenMaxData = Environment.GetEnvironmentVariable("MINT_TOKEN_MAX_DATA");
		if (string.IsNullOrWhiteSpace(mintTokenMaxData))
		{
			throw new("MINT_TOKEN_MAX_DATA not set in env");
		}

		var mintResult = await MintNonFungibleHelper.Run(api,
			wif,
			carbonTokenId,
			tokenSchemas,
			seriesId.Value,
			rom,
			ulong.Parse(mintTokenMaxData),
			ulong.Parse(gasFeeBase));

		Console.WriteLine("mintResult: " + mintResult);
	}
}
