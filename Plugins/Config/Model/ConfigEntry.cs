using Logshark.PluginLib.Helpers;
using ServiceStack.DataAnnotations;
using System;
using System.Linq;

namespace Logshark.Plugins.Config.Model
{
    /// <summary>
    /// Models a key/value pair of config entry + value.
    /// </summary>
    [Alias("ConfigEntries")]
    public class ConfigEntry
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid ConfigEntryHash { get; set; }

        [Index]
        public DateTime? FileLastModified { get; set; }

        [Index]
        public string RootKey { get; set; }

        [Index]
        public string Key { get; set; }

        public string Value { get; set; }

        public ConfigEntry()
        {
        }

        public ConfigEntry(string logsetHash, DateTime? fileLastModifiedTimestamp, string key, string value)
        {
            LogsetHash = Guid.Parse(logsetHash);
            FileLastModified = fileLastModifiedTimestamp;
            ConfigEntryHash = HashHelper.GenerateHashGuid(logsetHash, key, value);
            RootKey = GetRootKey(key);
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return String.Format(@"{0}: {1}", Key, Value);
        }

        protected static string GetRootKey(string key)
        {
            if (!key.Contains('.'))
            {
                return key;
            }

            return key.Substring(0, key.IndexOf('.'));
        }
    }
}