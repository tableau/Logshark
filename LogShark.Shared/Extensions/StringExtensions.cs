using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogShark.Shared.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex TsmNodeNameCapture = new Regex(@"^node\d+", RegexOptions.Compiled);
        private static readonly Regex TabadminWorkerNameCapture = new Regex(@"^worker\d+", RegexOptions.Compiled);
        private static readonly Regex TsmV0WorkerNameCapture = new Regex(@"^(?<hostname>[^/]+)/tabadminagent.+/logs/.+", RegexOptions.Compiled);
        private static readonly Regex BridgeHostNameCapture = new Regex(@"^Bridge*",RegexOptions.Compiled);

        public static string CleanControlCharacters(this string message)
        {
            // Replace control characters with string literals like "{\u001F}"
            return String.Join(
                String.Empty,
                message.Select(c => Char.IsControl(c) && !Char.IsWhiteSpace(c) ? $"{{\\u{Convert.ToInt32(c).ToString("X4")}}}" : c.ToString()));
        }

        public static string EnforceMaxLength(this string stringToTrim, int maxSymbolsToKeep)
        {
            return stringToTrim.Substring(0, Math.Min(stringToTrim.Length, maxSymbolsToKeep));
        }

        public static string NormalizePath(this string rawPath, string prefixToRemove)
        {
            var trimmedPath = rawPath.StartsWith(prefixToRemove) // Removing root from the start of the path (if present). Using length helps with the different dir separators on different platforms
                ? rawPath.Substring(prefixToRemove.Length + 1) // +1 for dir separator               
                : rawPath;
            
            return trimmedPath.NormalizeSeparatorsToUnix(); // To keep it consistent. "Directory" on Windows produce \, but ZipFile on Windows produce /
        }

        public static string NormalizeSeparatorsToUnix(this string rawPath)
        {
            return rawPath.Replace("\\", "/");
        }
        
        public static string RemoveZipFromTail(this string rawPath)
        {
            return rawPath.EndsWith(".zip")
                ? rawPath.Substring(0, rawPath.Length - ".zip".Length)
                : rawPath;
        }
        
        public static string RemoveTarFromTail(this string rawPath)
        {
            return rawPath.EndsWith(".tar")
                ? rawPath.Substring(0, rawPath.Length - ".tar".Length)
                : rawPath;
        }
        
        public static string GetWorkerIdFromFilePath(this string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }
            
            Match match;
            if ((match = TsmNodeNameCapture.Match(fullPath)).Success) // All TSM Nodes
            {
                return match.Value;
            }
            
            if ((match = TabadminWorkerNameCapture.Match(fullPath)).Success) // Tabadmin workers (excluding Primary)
            {
                return match.Value;
            }

            if ((match = TsmV0WorkerNameCapture.Match(fullPath)).Success) // TSM v0 all nodes (hostnames)
            {
                return match.GetString("hostname");
            }
            if(fullPath.Contains("Bridge") || fullPath.Contains("LiveQuery")) // Bridge Agents
                    {
                 var split = fullPath.Split('/');
                if (split.Length > 1)
                {
                    var folderName = split[split.Length - 2];
                    return folderName;
                }else
                {
                    return "HostUnknown";
                }
            }


            return "worker0"; // Tabadmin primary or Desktop
        }

        // Lock is needed because this logic can reshuffle the list. This could cause race conditions between threads.
        public static Match GetRegexMatchAndMoveCorrectRegexUpFront(this string lineToMatch, IList<Regex> regexList, object regexListLockObject)
        {
            if (regexList == null)
            {
                return null;
            }

            lock (regexListLockObject)
            {
                for (var i = 0; i < regexList.Count; ++i)
                {
                    var match = regexList[i].Match(lineToMatch);
                    if (match.Success)
                    {
                        regexList.MoveToFront(i);
                        return match;
                    }
                }

                return null;
            }
        }

        public static string TrimSurroundingDoubleQuotes(this string original)
        {
            if (original == null)
            {
                return null;
            }

            var startIndex = original.StartsWith("\"") ? 1 : 0;
            var length = original.EndsWith("\"") ? original.Length - 1 : original.Length;
            length = startIndex == 1 ? length - 1 : length;
            return original.Substring(startIndex, length);
        }
        
        // Had to create our own method, as FileInfo doesn't work correctly when constructed with Windows path on Linux machine
        // I.e. if code runs in Linux containers, but tries to process path generated by Windows machine (i.e. C:\ProgramData\Tableau...)    
        public static string ExtractFileName(this string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return fullPath;
            }

            var normalizedFullPath = fullPath.NormalizeSeparatorsToUnix();

            if (normalizedFullPath.EndsWith("/"))
            {
                throw new ArgumentException("Value appears to be a directory path. Extracting file name is not applicable", nameof(fullPath));
            }

            var lastSeparator = normalizedFullPath.LastIndexOf('/');

            if (lastSeparator == -1) // No separators found
            {
                return fullPath;
            }

            return normalizedFullPath.Substring(lastSeparator + 1);
        }
    }
}