using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public interface INpgsqlDataContext
    {
        string DatabaseName { get; }

        Task<int> ExecuteNonQuery(string commandText, Dictionary<string, object> parameters = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0);
        Task<int> ExecuteNonQueryToServiceDatabase(string commandText, Dictionary<string, object> parameters = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0);
        Task<T> ExecuteScalar<T>(string commandText, Dictionary<string, object> parameters = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0);
        Task<T> ExecuteScalarToServiceDatabase<T>(string commandText, Dictionary<string, object> parameters = null, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0);
    }
}