using LogParsers.Extensions;
using System;
using System.Collections.Generic;

namespace LogParsers.Helpers
{
    /// <summary>
    /// Contains functionality for standardizing the multiple types of time zone/offset data present in the logs into a single standard format.
    /// </summary>
    public static class TimeZoneStandardizer
    {
        private static readonly IDictionary<string, string> TimeZoneWindowsDictionary = null;
        private static readonly IDictionary<string, string> TimeZoneAbbreviationDictionary = new Dictionary<string, string>() {
                {"ACDT", "+1030"},
                {"ACST", "+0930"},
                {"ADT", "-0300"},
                {"AEDT", "+1100"},
                {"AEST", "+1000"},
                {"AHDT", "-0900"},
                {"AHST", "-1000"},
                {"AST", "-0400"},
                {"AT", "-0200"},
                {"AWDT", "+0900"},
                {"AWST", "+0800"},
                {"BAT", "+0300"},
                {"BDST", "+0200"},
                {"BET", "-1100"},
                {"BST", "-0300"},
                {"BT", "+0300"},
                {"BZT2", "-0300"},
                {"CADT", "+1030"},
                {"CAST", "+0930"},
                {"CAT", "-1000"},
                {"CCT", "+0800"},
                {"CDT", "-0500"},
                {"CED", "+0200"},
                {"CET", "+0100"},
                {"CST", "-0600"},
                {"CENTRAL", "-0600"},
                {"EAST", "+1000"},
                {"EDT", "-0400"},
                {"EED", "+0300"},
                {"EET", "+0200"},
                {"EEST", "+0300"},
                {"EST", "-0500"},
                {"EASTERN", "-0500"},
                {"FST", "+0200"},
                {"FWT", "+0100"},
                {"GMT", "-0000"},
                {"GST", "+1000"},
                {"HDT", "-0900"},
                {"HST", "-1000"},
                {"IDLE", "+1200"},
                {"IDLW", "-1200"},
                {"IST", "+0530"},
                {"IT", "+0330"},
                {"JST", "+0900"},
                {"JT", "+0700"},
                {"MDT", "-0600"},
                {"MED", "+0200"},
                {"MET", "+0100"},
                {"MEST", "+0200"},
                {"MEWT", "+0100"},
                {"MST", "-0700"},
                {"MOUNTAIN", "-0700"},
                {"MT", "+0800"},
                {"NDT", "-0230"},
                {"NFT", "-0330"},
                {"NT", "-1100"},
                {"NST", "+0630"},
                {"NZ", "+1100"},
                {"NZST", "+1200"},
                {"NZDT", "+1300"},
                {"NZT", "+1200"},
                {"PDT", "-0700"},
                {"PST", "-0800"},
                {"PACIFIC", "-0800"},
                {"ROK", "+0900"},
                {"SAD", "+1000"},
                {"SAST", "+0900"},
                {"SAT", "+0900"},
                {"SDT", "+1000"},
                {"SST", "+0200"},
                {"SWT", "+0100"},
                {"USZ3", "+0400"},
                {"USZ4", "+0500"},
                {"USZ5", "+0600"},
                {"USZ6", "+0700"},
                {"UT", "-0000"},
                {"UTC", "-0000"},
                {"UZ10", "+1100"},
                {"WAT", "-0100"},
                {"WET", "-0000"},
                {"WST", "+0800"},
                {"YDT", "-0800"},
                {"YST", "-0900"},
                {"ZP4", "+0400"},
                {"ZP5", "+0500"},
                {"ZP6", "+0600"}
            };

        /// <summary>
        /// Standardizes a potential timezone/offset value to a single format.
        /// </summary>
        /// <param name="rawTimeZone">The timezone/offset value to parse.</param>
        /// <returns>Standardized version of the timezone/offset value.</returns>
        public static string StandardizeTimeZone(string rawTimeZone)
        {
            // If this is already in numeric offset form, just return it.
            if (rawTimeZone.StartsWith("+") || rawTimeZone.StartsWith("-"))
            {
                return rawTimeZone;
            }

            // Check abbreviation dictionary for a match.
            if (TimeZoneAbbreviationDictionary.ContainsKey(rawTimeZone))
            {
                return TimeZoneAbbreviationDictionary[rawTimeZone];
            }

            // Check Windows timezone name dictionary for a match.
            var timeZoneWindowsDictionary = GetTimeZoneWindowsDictionary();
            if (timeZoneWindowsDictionary.ContainsKey(rawTimeZone))
            {
                return timeZoneWindowsDictionary[rawTimeZone];
            }

            // Give up.
            return rawTimeZone;
        }

        private static IDictionary<string, string> GetTimeZoneWindowsDictionary()
        {
            if (TimeZoneWindowsDictionary != null)
            {
                return TimeZoneWindowsDictionary;
            }

            // Build a dictionary of all known Windows timezone names and their respective offset values.
            IDictionary<string, string> timeZoneWindowsDictionary = new Dictionary<string, string>();
            foreach (var zone in TimeZoneInfo.GetSystemTimeZones())
            {
                // Process Standard timezone name.
                timeZoneWindowsDictionary.Add(zone.StandardName, zone.BaseUtcOffset.ToShortString());

                // Process Daylight Saving Time timezone name.
                if (!timeZoneWindowsDictionary.ContainsKey(zone.DaylightName))
                {
                    // We need to add an hour to account for Daylight Savings Time
                    var dsOffset = new TimeSpan(1, 0, 0);
                    timeZoneWindowsDictionary.Add(zone.DaylightName, zone.BaseUtcOffset.Add(dsOffset).ToShortString());
                }
            }

            return timeZoneWindowsDictionary;
        }
    }
}
