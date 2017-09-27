using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Logshark.PluginLib.Extensions
{
    public static class PostgresDbConnectionExtensions
    {
        private static readonly IOrmLiteDialectProvider provider = PostgreSqlDialect.Provider;

        public static bool ContainsRecord<T>(this IDbConnection db) where T : new()
        {
            return (db.Count<T>() > 0);
        }

        public static bool ContainsRecord(this IDbConnection db, string tableName)
        {
            if (!db.TableExists(tableName))
            {
                return false;
            }

            var command = db.CreateCommand();
            command.CommandText = String.Format("SELECT COUNT(*) FROM \"{0}\";", tableName);
            int recordCount = Convert.ToInt32(command.ExecuteScalar());

            return (recordCount > 0);
        }

        public static void CreateOrMigrateTable<T>(this IDbConnection db) where T : new()
        {
            ModelDefinition model = ModelDefinition<T>.Definition;
            var namingStrategy = provider.NamingStrategy;

            // Create the table if it doesn't already exist.
            db.CreateTableIfNotExists<T>();

            var tableName = namingStrategy.GetTableName(model.ModelName);

            // Find each of the missing fields
            List<string> columns = GetColumnNames(db, tableName);
            List<FieldDefinition> missingFields = model.FieldDefinitions
                                                    .Where(field => columns.Contains(namingStrategy.GetColumnName(field.FieldName)) == false)
                                                    .ToList();

            // Add a new column for each missing field
            foreach (var field in missingFields)
            {
                if (!field.IsNullable)
                {
                    throw new Exception(String.Format("Cannot migrate table {0} due to new non-nullable field {1}!", tableName, field.FieldName));
                }

                db.ExecuteSql(GetAddColumnStatement(tableName, field, namingStrategy));

                //Add the appropriate index
                if (field.IsIndexed)
                {
                    db.ExecuteSql(GetAddIndexStatement(tableName, field, model, namingStrategy));
                }
            }

            //Find any fields which are nullable in the model but not in the DB.
            List<string> nullableColumns = GetColumnNames(db, tableName, nullableOnly: true);
            List<FieldDefinition> missingNullableFields = model.FieldDefinitions
                                                               .Where(field => field.IsNullable && !nullableColumns.Contains(namingStrategy.GetColumnName(field.FieldName)))
                                                               .ToList();

            //Make them nullable.
            foreach (var field in missingNullableFields)
            {
                db.ExecuteSql(GetMakeColumnNullableStatement(tableName, field, namingStrategy));
            }
        }

        private static List<string> GetColumnNames(IDbConnection db, string tableName, bool nullableOnly = false)
        {
            var columns = new List<string>();
            using (var cmd = db.CreateCommand())
            {
                if (nullableOnly)
                {
                    cmd.CommandText = GetNullableColumnsQuery(tableName);
                }
                else
                {
                    cmd.CommandText = GetColumnsQuery(tableName);
                }

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var ordinal = reader.GetOrdinal("COLUMN_NAME");
                    columns.Add(reader.GetString(ordinal));
                }
                reader.Close();
            }
            return columns;
        }

        private static string GetColumnsQuery(string tableName)
        {
            return String.Format("select column_name from information_schema.columns where table_name = '{0}'", tableName);
        }

        private static string GetNullableColumnsQuery(string tableName)
        {
            return String.Format("select column_name from information_schema.columns where is_nullable = 'YES' and table_name = '{0}'", tableName);
        }

        private static string GetMakeColumnNullableStatement(string tableName, FieldDefinition field, INamingStrategy namingStrategy)
        {
            string columnName = namingStrategy.GetColumnName(field.FieldName);
            string makeColumnNullableStatement = String.Format("ALTER TABLE {0} ALTER COLUMN {1} DROP NOT NULL",
                                             tableName,
                                             columnName);

            return makeColumnNullableStatement;
        }

        private static string GetAddColumnStatement(string tableName, FieldDefinition field, INamingStrategy namingStrategy)
        {
            string columnName = namingStrategy.GetColumnName(field.FieldName);
            string addColumnStatement = String.Format("ALTER TABLE {0} ADD COLUMN {1} {2}",
                                             tableName,
                                             columnName,
                                             GetDataTypeForField(field.FieldType));

            return addColumnStatement;
        }

        private static string GetAddIndexStatement(string tableName, FieldDefinition field, ModelDefinition model, INamingStrategy namingStrategy)
        {
            string columnName = namingStrategy.GetColumnName(field.FieldName);
            if (field.IsUnique)
            {
                return String.Format("CREATE UNIQUE INDEX uidx_{0}_{1} ON {2} ({3})", model.ModelName.ToLowerInvariant(), field.FieldName.ToLowerInvariant(), tableName, columnName);
            }
            else
            {
                return String.Format("CREATE INDEX idx_{0}_{1} ON {2} ({3})", model.ModelName.ToLowerInvariant(), field.FieldName.ToLowerInvariant(), tableName, columnName);
            }
        }

        private static string GetDataTypeForField(Type fieldType)
        {
            //Make sure we use text for string instead of varchar.
            if (fieldType == typeof(String))
            {
                return "text";
            }
            else
            {
                return provider.GetColumnTypeDefinition(fieldType);
            }
        }
    }
}