using LogShark.Containers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace LogShark.Writers.Sql.Connections.Npgsql
{

    public class NpgsqlTypeProjection<T>
    {
        private const string DefaultSchema = "public";

        /// <summary>
        /// TableName is populated in the following priority (all of which will be converted to snake_case)
        /// 1. TableAttribute
        /// 2. DataSetInfo
        /// 3. Class name
        /// </summary>
        public string TableName { get; private set; }
        public string Schema { get; private set; }
        public IEnumerable<NpgsqlTypePropertyProjection<T>> TypePropertyProjections { get; private set; }
        private readonly bool _tableNameIsSetFromAttribute = false;

        public NpgsqlTypeProjection(DataSetInfo dataSetInfo)
        {
            var typeAttributes = typeof(T).GetCustomAttributes(true).Select(a => a as Attribute);
            var tableAttribute = typeAttributes.OfType<TableAttribute>().FirstOrDefault();

            _tableNameIsSetFromAttribute = tableAttribute?.Name != null;
            TableName = (tableAttribute?.Name ?? (dataSetInfo != null ? $"{dataSetInfo.Name}" : null) ?? typeof(T).Name);
            Schema = tableAttribute?.Schema ?? DefaultSchema;

            TypePropertyProjections = typeof(T)
                .GetProperties()
                .Where(p => p.CanRead)
                .Select(property => new NpgsqlTypePropertyProjection<T>(property))
                .OrderBy(tpp => tpp.Order)
                .ThenBy(tpp => tpp.ColumnName)
                .ToList();
        }
    }
}
