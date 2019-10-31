using com.tableausoftware.hyperextract;
using Optional;
using Optional.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Tableau.ExtractApi.Exceptions;
using Tableau.ExtractApi.Helpers;
using Tableau.ExtractApi.TableSchema;

using Type = System.Type;

namespace Tableau.ExtractApi.Writer
{
    internal sealed class ExtractWriter<T> : IExtractWriter<T>
    {
        private static readonly DateTime MinimumSupportedDateTime = new DateTime(1000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);

        private static readonly IDictionary<Type, Action<Row, int, object>> TypeColumnSetterMap = new Dictionary<Type, Action<Row, int, object>>
        {
            { typeof(bool), (row, i, value) => row.setBoolean(i, (bool) value) },
            { typeof(char), (row, i, value) => row.setString(i, value.ToString()) },
            { typeof(DateTime), (row, i, value) => { DateTime ts = (DateTime) value;
                                                     if (ts >= MinimumSupportedDateTime) row.setDateTime(i, ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, ts.Millisecond); }},
            { typeof(decimal), (row, i, value) => row.setDouble(i, Convert.ToDouble((decimal) value)) },
            { typeof(double), (row, i, value) => row.setDouble(i, (double) value) },
            { typeof(float), (row, i, value) => row.setDouble(i, Convert.ToDouble((float) value)) },
            { typeof(int), (row, i, value) => row.setInteger(i, (int) value) },
            { typeof(long), (row, i, value) => row.setLongInteger(i, (long) value) },
            { typeof(string), (row, i, value) => row.setString(i, value.ToString()) },
            { typeof(TimeSpan), (row, i, value) => { TimeSpan ts = (TimeSpan) value;
                                                     row.setDuration(i, ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds); }}
        };

        private static readonly Action<Row, int, object> SetColumnNullAction = (row, i, _) => row.setNull(i);

        private readonly Table table;
        private readonly ExtractTableSchema<T> schema;
        private readonly TableDefinition tableDefinition;
        private readonly Row row; // The Extract API performs better if you re-use a single Row object instead of allocating a new one for every insert.

        public ExtractWriter(Table table, ExtractTableSchema<T> schema)
        {
            this.table = table;
            this.schema = schema;

            tableDefinition = table.getTableDefinition();
            row = new Row(tableDefinition);
        }

        public Option<Unit, ExtractInsertionException> Insert(T item)
        {
            return Safe.Try(() =>
            {
                UpdateRow(item);
                table.insert(row);
                return Unit.Default;
            }).MapException(ex => new ExtractInsertionException(String.Format("Failed to insert item: {0}", ex.Message), ex));
        }

        public void Dispose()
        {
            row.close();
            tableDefinition.close();
        }

        private void UpdateRow(T item)
        {
            PropertyInfo[] properties = item.GetType().GetProperties();

            for (int columnIndex = 0; columnIndex < schema.MappedColumns.Count; columnIndex++)
            {
                var columnMapping = schema.MappedColumns[columnIndex];
                var propertyValue = properties[columnMapping.ModelPropertyIndex].GetValue(item);

                var columnDefinition = columnMapping.Column;
                var persistedType = columnDefinition.IsNullable ? columnDefinition.InnerType : columnDefinition.Type;

                var setColumnValueAction = GetColumnValueSetAction(persistedType, propertyValue);
                setColumnValueAction(row, columnIndex, propertyValue);
            }
        }

        private Action<Row, int, object> GetColumnValueSetAction(Type valueType, object value)
        {
            if (value == null || !TypeColumnSetterMap.ContainsKey(valueType))
            {
                return SetColumnNullAction;
            }

            return TypeColumnSetterMap[valueType];
        }
    }
}