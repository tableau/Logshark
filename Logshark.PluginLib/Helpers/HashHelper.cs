using Org.BouncyCastle.Crypto.Digests;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Logshark.PluginLib.Helpers
{
    public static class HashHelper
    {
        public static string GenerateHashString(params object[] parameters)
        {
            // Generate seed string by concatenating all parameters.
            var hashString = String.Concat(parameters);
            byte[] hashBytes = GetByteArray(hashString);

            // Generate MD5 digest from hashed bytes.
            MD5Digest digest = new MD5Digest();
            digest.BlockUpdate(hashBytes, inOff: 0, length: hashBytes.Length);
            byte[] result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, outOff: 0);

            // Return the hex representation of the digest, not a 64-bit one.
            return BitConverter.ToString(result).Replace("-", String.Empty).ToLowerInvariant();
        }

        public static Guid GenerateHashGuid(params object[] parameters)
        {
            string hashString = GenerateHashString(parameters);
            return Guid.Parse(hashString);
        }

        private static byte[] GetByteArray(object obj)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }
    }
}