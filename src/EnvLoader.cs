using dotenv.net;

public static class EnvLoader
{
	/// <summary>
	/// Loads a .env file by walking upwards from common start points until found
	/// </summary>
	public static bool TryLoad(string fileName = ".env")
	{
		// Try from the project working directory
		if (TryFindUpwards(Directory.GetCurrentDirectory(), fileName, out var path))
		{
			try
			{
				DotEnv.Load(new DotEnvOptions(
					envFilePaths: new[] { path },
					overwriteExistingVars: true,
					probeForEnv: false
				));
			}
			catch
			{
				return false;
			}

			return true;
		}

		return false;
	}

	private static bool TryFindUpwards(string startDir, string fileName, out string foundPath)
	{
		foundPath = string.Empty;
		if (string.IsNullOrWhiteSpace(startDir)) return false;

		var dir = new DirectoryInfo(Path.GetFullPath(startDir));

		// Walk up to filesystem root
		while (dir != null)
		{
			var candidate = Path.Combine(dir.FullName, fileName);
			if (File.Exists(candidate))
			{
				foundPath = candidate;
				return true;
			}
			dir = dir.Parent;
		}

		return false;
	}
}
