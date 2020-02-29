using System;
using Npgsql;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public class NpgsqlConnectionWrapper : IDisposable
    {
        public NpgsqlConnection Connection { get; }
        
        public NpgsqlConnectionWrapper(string connectionString)
        {
            Connection = new NpgsqlConnection(connectionString);
            Connection.Open();
        }
        
        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }
    }
}