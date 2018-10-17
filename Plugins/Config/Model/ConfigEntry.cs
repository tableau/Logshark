using System;
using System.Linq;

namespace Logshark.Plugins.Config.Model
{
    /// <summary>
    /// Models a key/value pair of config entry + value.
    /// </summary>
    public class ConfigEntry
    {
        public string RootKey { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? FileLastModified { get; set; }

        public ConfigEntry()
        {
        }

        public ConfigEntry(string key, string value, DateTime? fileLastModifiedTimestamp)
        {
            RootKey = GetRootKey(key);
            Key = key;
            Value = value;
            FileLastModified = fileLastModifiedTimestamp;
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