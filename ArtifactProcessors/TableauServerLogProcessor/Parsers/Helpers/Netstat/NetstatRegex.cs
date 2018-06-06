using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat
{
    /// <summary>
    /// Helper class for maintaining metadata around regexes used for Netstat parsing.
    /// </summary>
    internal class NetstatRegex
    {
        public enum EntryType
        {
            ActiveInternetConnection,
            UnixDomainSocket
        }

        public EntryType Type { get; private set; }

        public Regex Regex { get; private set; }

        public NetstatRegex(EntryType entryType, Regex regex)
        {
            Type = entryType;
            Regex = regex;
        }
    }
}