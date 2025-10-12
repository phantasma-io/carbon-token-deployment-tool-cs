using System.Numerics;
using System.Security.Cryptography;

public static class IdHelper
{
	public static BigInteger GetRandomId()
	{
		byte[] bytes = new byte[32];
		RandomNumberGenerator.Fill(bytes);

		return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
	}
}
