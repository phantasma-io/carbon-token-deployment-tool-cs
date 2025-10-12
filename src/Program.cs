using PhantasmaPhoenix.RPC;

if (!EnvLoader.TryLoad())
{
	throw new(".env not found");
}

var RpcUrl = Environment.GetEnvironmentVariable("RPC");
if (string.IsNullOrWhiteSpace(RpcUrl))
{
	throw new("RPC not set in env");
}

Console.WriteLine("RPC: " + RpcUrl);

using var rpcClient = new RpcClient(); // Optionally pass HttpClient/ILogger
using var api = new PhantasmaAPI(RpcUrl, rpcClient);

var wif = Environment.GetEnvironmentVariable("WIF");
if (string.IsNullOrWhiteSpace(wif))
{
	throw new("WIF not set in env");
}

// await CarbonTokenTransfer.Run(api, wif);
await CarbonTokenDeployment.Run(api, wif);
