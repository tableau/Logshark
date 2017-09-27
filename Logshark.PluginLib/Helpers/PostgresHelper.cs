using Logshark.PluginLib.Extensions;
using ServiceStack.OrmLite;

namespace Logshark.PluginLib.Helpers
{
    public static class PostgresHelper
    {
        public static bool ContainsRecord<T>(IDbConnectionFactory connectionFactory) where T : new()
        {
            using (var db = connectionFactory.OpenDbConnection())
            {
                return db.ContainsRecord<T>();
            }
        }

        public static bool ContainsRecord(IDbConnectionFactory connectionFactory, string tableName)
        {
            using (var db = connectionFactory.OpenDbConnection())
            {
                return db.ContainsRecord(tableName);
            }
        }
    }
}