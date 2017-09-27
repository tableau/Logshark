using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ConnectionModel.Helpers
{
    /// <summary>
    /// This class shouldn't have to exist, but some of the exceptions the MongoDB driver throws have really ugly exception messages that need to be cleaned up before presenting to a user.
    /// </summary>
    internal static class MongoExceptionHelper
    {
        private static readonly IEnumerable<Regex> MongoExceptionRegexes = new List<Regex>
        {
            // Match timeout/auth exceptions.
            new Regex(@".*(?<exception_type>MongoDB\.Driver\.[^\.]+?): (?<exception_message>.*?)\n",
                      RegexOptions.ExplicitCapture | RegexOptions.Compiled)
        };

        /// <summary>
        /// Wraps an exception inside of a MongoException, if it matches a known pattern.
        /// </summary>
        public static Exception GetMongoException(Exception ex)
        {
            foreach (var regex in MongoExceptionRegexes)
            {
                var match = regex.Match(ex.Message);
                if (match.Success)
                {
                    // Found a match!  Wrap this exception in a MongoException.
                    return new MongoException(match.Groups["exception_message"].Value, ex);
                }
            }

            // No known pattern; just return this as-is.
            return ex;
        }
    }
}