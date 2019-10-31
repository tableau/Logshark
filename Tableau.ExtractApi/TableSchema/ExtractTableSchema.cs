using com.tableausoftware.common;
using com.tableausoftware.hyperextract;
using System.Collections.Generic;
using System.Reflection;
using Tableau.ExtractApi.Extensions;

namespace Tableau.ExtractApi.TableSchema
{
    internal class ExtractTableSchema<T>
    {
        public Collation DefaultCollation { get; private set; }

        public IList<MappedColumnDefinition> MappedColumns { get; private set; }

        public ExtractTableSchema() : this(Collation.EN_US)
        {
        }

        public ExtractTableSchema(Collation collation)
        {
            DefaultCollation = collation;
            MappedColumns = new List<MappedColumnDefinition>();

            PropertyInfo[] modelProperties = typeof(T).GetProperties();
            for (int propertyIndex = 0; propertyIndex < modelProperties.Length; propertyIndex++)
            {
                if (modelProperties[propertyIndex].IsPersistable())
                {
                    MappedColumns.Add(new MappedColumnDefinition(propertyIndex, new ColumnDefinition(modelProperties[propertyIndex])));
                }
            }
        }

        public bool IsCompatibleWith(TableDefinition table)
        {
            if (MappedColumns.Count != table.getColumnCount())
            {
                return false;
            }

            for (int columnIndex = 0; columnIndex < table.getColumnCount(); columnIndex++)
            {
                if (table.getColumnType(columnIndex) != MappedColumns[columnIndex].Column.ExtractType)
                {
                    return false;
                }
            }

            return true;
        }
    }
}