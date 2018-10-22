using Newtonsoft.Json;
using System;

namespace Logshark.Plugins.ReplayCreation.Models
{
    /// <summary>
    /// Stores commands and time that it was executed
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
        /// <param name="other"></param>
        /// <returns></returns>                 
        public int CompareTo(TabCommand other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}