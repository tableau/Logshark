using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Logshark.Common.Helpers
{
    public static class HashUtility
    {
        /// <summary>
        /// Computes an MD5 hash of the contents of a given directory by hashing the union of relative file paths and associated file sizes.
        /// </summary>
        public static string ComputeDirectoryHash(string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                throw new ArgumentException(String.Format("Target directory '{0}' does not exist!", targetPath), "targetPath");
            }

            // Trim all occurrences of standard & alternate directory separator chars and then append a single standard separator to stay consistent.
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Build a dictionary mapping relative file path to file size.
            var fileSizeMap = new SortedDictionary<string, long>();
            foreach (FileInfo file in DirectoryHelper.GetAllFiles(targetPath))
            {
                string relativePath = file.FullName.Substring(targetPath.Length);
                fileSizeMap[relativePath] = file.Length;
            }

            return GenerateMD5Hash(fileSizeMap);
        }

        private static string GenerateMD5Hash(object itemToHash)
        {
            byte[] hashBytes = GetByteArray(itemToHash);

            // Generate MD5 digest from hashed bytes.
            MD5Digest digest = new MD5Digest();
            digest.BlockUpdate(hashBytes, inOff: 0, length: hashBytes.Length);
            byte[] result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, outOff: 0);

            // Return the hex representation of the digest, not a 64-bit one.
            return BitConverter.ToString(result).Replace("-", String.Empty).ToLowerInvariant();
        }

        private static byte[] GetByteArray(object obj)
        {
            var formatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }
    }
}