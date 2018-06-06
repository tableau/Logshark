using System;

namespace Logshark.Tests.Helpers
{
    internal static class PostgresHelper
    {
        public static string GetNewPostgresDbName()
        {
            return String.Format("TEST_{0}_{1}", Environment.MachineName, DateTime.Now);
        }
    }
}