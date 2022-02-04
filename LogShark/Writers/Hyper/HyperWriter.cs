using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LogShark.Containers;
using Tableau.HyperAPI;
using static Tableau.HyperAPI.TableDefinition;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace LogShark.Writers.Hyper
{
    public class HyperWriter<T> : BaseWriter<T>
    {
        private static readonly Dictionary<Type, Func<PropertyInfo, bool, Column>> _columnSwitch = new Dictionary<Type, Func<PropertyInfo, bool, Column>>
            {
                { typeof(bool),             (p, nullable) => new Column(p.Name, SqlType.Bool(), Nullability.Nullable) },
                { typeof(int),              (p, nullable) => new Column(p.Name, SqlType.Int(), Nullability.Nullable) },
                { typeof(short),            (p, nullable) => new Column(p.Name, SqlType.SmallInt(), Nullability.Nullable) },
                { typeof(long),             (p, nullable) => new Column(p.Name, SqlType.BigInt(), Nullability.Nullable) },
                { typeof(double),           (p, nullable) => new Column(p.Name, SqlType.Double(), Nullability.Nullable) },
                { typeof(string),           (p, nullable) => new Column(p.Name, SqlType.Text(), Nullability.Nullable) },
                { typeof(DateTime),         (p, nullable) => new Column(p.Name, SqlType.Timestamp(), Nullability.Nullable) },
                { typeof(DateTimeOffset),   (p, nullable) => new Column(p.Name, SqlType.Timestamp(), Nullability.Nullable) }
            };

        private readonly string _dbPath;
        private readonly Action<Inserter, T> _updateAction;
        private readonly Inserter _inserter;
        
        public HyperWriter(DataSetInfo dataSetInfo, Endpoint server, string hyperOutputDir, string tableName, ILogger logger)
        : base(dataSetInfo, logger, nameof(HyperWriter<T>))
        {
            _updateAction = UpdateAction();
            _dbPath =  Path.Combine(Path.GetFullPath(hyperOutputDir), $"{tableName}.hyper");

            // Create our DB file if it doesn't already exist
            if (!File.Exists(_dbPath))
            {
                using (var createDbConnection = new Connection(server))
                {
                    createDbConnection.Catalog.CreateDatabaseIfNotExists(_dbPath);
                }
            }

            // Create our table if it doesn't already exist
            var connection = HyperConnectionManager.Instance.GetConnection(server, _dbPath);
            connection.Catalog.CreateTableIfNotExists(new TableDefinition(tableName, GetColumnDefs()));
            if (!connection.Catalog.HasTable(tableName))
            {
                var columnDefs = GetColumnDefs();
                var tableDef = new TableDefinition(tableName, columnDefs);
                connection.Catalog.CreateTable(tableDef);
            }

            _inserter = new Inserter(connection, tableName);

            Logger.LogDebug("{writerType} created for database {databasePath} and table {tableName}", nameof(HyperWriter<T>), _dbPath, tableName);
        }

        protected override void InsertNonNullLineLogic(T objectToWrite)
        {
            _updateAction(_inserter, objectToWrite);
            _inserter.EndRow();
        }

        protected override void CloseLogic()
        {
            _inserter.Execute();
        }

        public override void Dispose()
        {
            base.Dispose();
            
            // Clear our inserter, and remember to clean up our Hyper Connection, which is being managed by 
            // the HyperConnectionManager singleton
            _inserter.Dispose();
            HyperConnectionManager.Instance.DisposeConnection(_dbPath);
        }

        private static IEnumerable<Column> GetColumnDefs()
        {
            return typeof(T)
                .GetProperties()
                .Select(property =>
                {
                    var propertyType = property.PropertyType;
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return _columnSwitch[property.PropertyType.GetGenericArguments()[0]](property, true);
                    }

                    return _columnSwitch[propertyType](property, false);
                });
        }

        private static Action<Inserter, T> UpdateAction()
        {
            var inserter = Expression.Parameter(typeof(Inserter), "inserter");
            var poco = Expression.Parameter(typeof(T), "poco");
            var block = Expression.Block(typeof(T)
                .GetProperties()
                .Select(property =>
                {
                    // Determine the method we will need to call
                    var method = $"Set{property.PropertyType.Name}";
                    var propertyType = property.PropertyType;
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        method = $"SetNullable{property.PropertyType.GetGenericArguments()[0].Name}";
                    }

                    // Call the method based on the type of property we are looking at
                    return Expression.Call(
                        null,
                        typeof(HyperDataSetter).GetMethod(method),
                        inserter,
                        Expression.Property(poco, property));
                }));

            return Expression.Lambda<Action<Inserter, T>>(block, inserter, poco)
                .Compile();
        }
    }
}
