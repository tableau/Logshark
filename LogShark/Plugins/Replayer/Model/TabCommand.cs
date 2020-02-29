using Newtonsoft.Json;
using System;

namespace Logshark.Plugins.Replayer.Models
{
    /// <summary>
    /// Stores commands and time that it was executed
    /// Used for serializing the commands in creation of Replay json file
    /// </summary>
    public class TabCommand : IComparable<TabCommand>
    {
        [JsonProperty("Time")]
        public string Time { get; set; }

        [JsonProperty("Command")]
        public Command Command { get; set; }

        public TabCommand(string time, Command command)
        {
            Time = time;
            Command = command;
        }

        /// <summary>
        /// Compare two tabcommand based on the time value
        /// compare the time as a string
        /// </summary>
        public int CompareTo(TabCommand other)
        {
            return String.Compare(Time, other.Time, StringComparison.Ordinal);
        }
    }
}