using System;
using System.Collections.Generic;
using System.Text;
using Tableau.HyperAPI;

namespace LogShark.Writers.Hyper
{
    class HyperConnectionManager
    {
        private Dictionary<string, Connection> _connectionDict = new Dictionary<string, Connection>();
        private Dictionary<string, int> _connectionRefCount = new Dictionary<string, int>();

        private static readonly Lazy<HyperConnectionManager> _instance = new Lazy<HyperConnectionManager>(() => new HyperConnectionManager());

        private HyperConnectionManager()
        { }

        public static HyperConnectionManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public Connection GetConnection(Endpoint endpoint, string dbPath)
        {
            // If this is a new hyper connection then create our dictionary entries
            if (!_connectionDict.ContainsKey(dbPath))
            {
                var con = new Connection(endpoint, dbPath, CreateMode.CreateIfNotExists);
                _connectionDict[dbPath] = con;
                _connectionRefCount[dbPath] = 0;
            }

            // Update the ref count and return the connection
            _connectionRefCount[dbPath]++;
            return _connectionDict[dbPath];
        }

        public void DisposeConnection(string dbPath)
        {
            // If we are trying to clean up a connection that doesn't exist, we have a problem
            if (!_connectionDict.ContainsKey(dbPath) || !_connectionRefCount.ContainsKey(dbPath))
            {
                throw new ArgumentException("Invalid dbPath provided to DisposeConnection");
            }

            // Decrement our ref count
            _connectionRefCount[dbPath]--;

            // Cleanly dispose if our ref count is 0
            if (_connectionRefCount[dbPath] == 0)
            {
                _connectionDict[dbPath].Dispose();
                _connectionDict.Remove(dbPath);
                _connectionRefCount.Remove(dbPath);
            }
        }
    }
}
