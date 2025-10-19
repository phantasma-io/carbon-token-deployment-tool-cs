using Newtonsoft.Json;
using PhantasmaPhoenix.RPC;

public static class CarbonTokenDeployment
{
	public static T LoadEnvOrThrow<T>(string name)
	{
		var v = LoadEnv<T>(name, default);
		if (v == null || EqualityComparer<T>.Default.Equals(v, default))
		{
			throw new($"{name} not set in env");
		}
		return v;
	}

	public static T? LoadEnv<T>(string name, T? defaultValue)
	{
		var v = Environment.GetEnvironmentVariable(name);
		if (string.IsNullOrWhiteSpace(v))
		{
			return defaultValue;
		}

		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)ulong.Parse(v);
		}
		else if (typeof(T) == typeof(uint))
		{
			return (T)(object)uint.Parse(v);
		}
		else if (typeof(T) == typeof(string))
		{
			return (T)(object)v;
		}
		throw new Exception($"Unsupported type {typeof(T)}");
	}

	public static async Task Run(PhantasmaAPI api, string wif)
	{
		var SYMBOL = LoadEnvOrThrow<string>("SYMBOL");
		var CARBON_TOKEN_ID = LoadEnv<ulong>("CARBON_TOKEN_ID", 0);
		var CARBON_TOKEN_SERIES_ID = LoadEnv<uint>("CARBON_TOKEN_SERIES_ID", 0);

		var ROM = LoadEnv<string>("ROM", null);

		var TOKEN_METADATA_FIELDS = LoadEnvOrThrow<string>("TOKEN_METADATA_FIELDS");

		Dictionary<string, string>? tokenMetadataFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(TOKEN_METADATA_FIELDS);
		if (tokenMetadataFields == null)
		{
			throw new("Could not deserialize TOKEN_METADATA_FIELDS");
		}

		var NFT_METADATA_NAME = LoadEnvOrThrow<string>("NFT_METADATA_NAME");
		var NFT_METADATA_DESCRIPTION = LoadEnvOrThrow<string>("NFT_METADATA_DESCRIPTION");
		var NFT_METADATA_IMAGE_URL = LoadEnvOrThrow<string>("NFT_METADATA_IMAGE_URL");
		var NFT_METADATA_INFO_URL = LoadEnvOrThrow<string>("NFT_METADATA_INFO_URL");
		var NFT_METADATA_ROYALTIES = LoadEnvOrThrow<uint>("NFT_METADATA_ROYALTIES");

		var CREATE_TOKEN_MAX_DATA = LoadEnvOrThrow<ulong>("CREATE_TOKEN_MAX_DATA");
		var CREATE_TOKEN_SERIES_MAX_DATA = LoadEnvOrThrow<ulong>("CREATE_TOKEN_SERIES_MAX_DATA");
		var MINT_TOKEN_MAX_DATA = LoadEnvOrThrow<ulong>("MINT_TOKEN_MAX_DATA");

		var GAS_FEE_BASE = LoadEnvOrThrow<ulong>("GAS_FEE_BASE");
		var GAS_FEE_CREATE_TOKEN_BASE = LoadEnvOrThrow<ulong>("GAS_FEE_CREATE_TOKEN_BASE");
		var GAS_FEE_CREATE_TOKEN_SYMBOL = LoadEnvOrThrow<ulong>("GAS_FEE_CREATE_TOKEN_SYMBOL");
		var GAS_FEE_CREATE_TOKEN_SERIES = LoadEnvOrThrow<ulong>("GAS_FEE_CREATE_TOKEN_SERIES");
		var GAS_FEE_MULTIPLIER = LoadEnvOrThrow<ulong>("GAS_FEE_MULTIPLIER");

		if (CARBON_TOKEN_ID == 0)
		{
			var (success, carbonTokenId) = await CreateTokenHelper.Run(api,
				wif,
				SYMBOL,
				tokenMetadataFields,
				CREATE_TOKEN_MAX_DATA,
				GAS_FEE_BASE,
				GAS_FEE_CREATE_TOKEN_BASE,
				GAS_FEE_CREATE_TOKEN_SYMBOL,
				GAS_FEE_MULTIPLIER);

			if (!success || carbonTokenId == null)
			{
				throw new Exception("Could not create token");
			}

			Console.WriteLine("tokenId result: " + carbonTokenId);
			CARBON_TOKEN_ID = carbonTokenId.Value;
		}

		if (CARBON_TOKEN_SERIES_ID == 0)
		{
			var (success, carbonSeriesId) = await CreateTokenSeriesHelper.Run(api,
				wif,
				CARBON_TOKEN_ID,
				CREATE_TOKEN_SERIES_MAX_DATA,
				GAS_FEE_BASE,
				GAS_FEE_CREATE_TOKEN_SERIES,
				GAS_FEE_MULTIPLIER);

			if (!success || carbonSeriesId == null)
			{
				throw new Exception("Could not create NFT series");
			}

			Console.WriteLine("seriesId result: " + carbonSeriesId);

			CARBON_TOKEN_SERIES_ID = carbonSeriesId.Value;
		}

		var mintResult = await MintNonFungibleHelper.Run(api,
			wif,
			CARBON_TOKEN_ID,
			CARBON_TOKEN_SERIES_ID,
			NFT_METADATA_NAME,
			NFT_METADATA_DESCRIPTION,
			NFT_METADATA_IMAGE_URL,
			NFT_METADATA_INFO_URL,
			NFT_METADATA_ROYALTIES,
			ROM,
			MINT_TOKEN_MAX_DATA,
			GAS_FEE_BASE,
			GAS_FEE_MULTIPLIER);

		Console.WriteLine("mintResult: " + mintResult);
	}
}
