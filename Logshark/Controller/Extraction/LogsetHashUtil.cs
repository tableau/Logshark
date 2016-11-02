using ICSharpCode.SharpZipLib.Zip;
using Logshark.Exceptions;
using Logshark.Helpers;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace Logshark.Controller.Extraction
{
    public class LogsetHashUtil
    {
        public static string GetLogSetHash(string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                return ComputeDirectoryHash(targetPath);
            }
            else
            {
                return ComputeZipFileHash(targetPath);
            }
        }

        public static bool IsValidMD5(string md5String)
        {
            Regex rgx = new Regex(@"[a-fA-F0-9]{32}");
            return rgx.IsMatch(md5String);
        }

        private static string ComputeDirectoryHash(string targetPath)
        {
            // Trim all occurrences of standard & alternate directory separator chars and then append a single standard separator to stay consistent.
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            IEnumerable<FileInfo> allFiles = DirectoryHelper.GetAllFiles(targetPath);

            SortedDictionary<string, long> fileSet = new SortedDictionary<string, long>();

            foreach (var file in allFiles)
            {
                string relativePath = file.FullName.Substring(targetPath.Length);

                // Filter out all worker zips and directories, we calculate the logset fingerprint based on contents of the primary only.
                if (!relativePath.Contains(Path.DirectorySeparatorChar + "worker") || RootIsWorker(relativePath))
                {
                    fileSet[relativePath] = file.Length;
                }
            }

            return GenerateMD5Hash(fileSet);
        }

        private static string ComputeZipFileHash(string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                throw new ArgumentException("Target file or directory does not exist!");
            }

            var fileExtension = Path.GetExtension(targetPath);
            if (fileExtension == null || !fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidLogsetException(fileExtension + " is not supported as an archive format!");
            }

            SortedDictionary<string, long> fileSet = new SortedDictionary<string, long>();

            ZipFile zipFile = null;
            try
            {
                zipFile = new ZipFile(File.OpenRead(targetPath));
                foreach (ZipEntry zipEntry in zipFile)
                {
                    string standardizedZipEntryName = zipEntry.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    if (zipEntry.IsFile &&
                        !standardizedZipEntryName.Contains(String.Concat(Path.AltDirectorySeparatorChar, "worker")) ||
                        RootIsWorker(standardizedZipEntryName))
                    {
                        fileSet[standardizedZipEntryName] = zipEntry.Size;
                    }
                }
            }
            catch (ZipException ex)
            {
                throw new InvalidLogsetException(String.Format("Unable to access contents of archive '{0}': {1}", targetPath, ex.Message), ex);
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true;
                    zipFile.Close();
                }
            }

            return GenerateMD5Hash(fileSet);
        }

        private static bool RootIsWorker(string fileName)
        {
            return fileName.Split(Path.DirectorySeparatorChar)[0].Contains("worker");
        }

        private static string GenerateMD5Hash(SortedDictionary<string, long> fileSet)
        {
            byte[] hashBytes = GetByteArray(fileSet);

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
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}