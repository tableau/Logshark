using com.tableausoftware.common;
using com.tableausoftware.hyperextract;
using Optional;
using System;
using System.Linq;
using Tableau.ExtractApi.Exceptions;
using Tableau.ExtractApi.Helpers;
using Tableau.ExtractApi.Writer;

using Table = com.tableausoftware.hyperextract.Table;

namespace Tableau.ExtractApi.TableSchema
{
    internal sealed class ExtractTable<T>
    {
        private readonly ExtractWriter<T> writer;

        public string Name { get; private set; }

        public ExtractTable(Extract extract)
        {
            // A Hyper extract can actually contain multiple tables, but currently Tableau will convert a multi-table extract into multiple single-table extracts
            // on packaged workbook load.  So per the API documentation guidance, we enforce single-table extracts that can only have a table name of "Extract".
            Name = "Extract";
            writer = InitializeWriter(extract);
        }

        public Option<Unit, ExtractInsertionException> Insert(T item)
        {
            return writer.Insert(item);
        }

        private ExtractWriter<T> InitializeWriter(Extract extract)
        {
            var schema = new ExtractTableSchema<T>();

            Table table;
            if (extract.hasTable(Name))
            {
                table = OpenExistingTable(extract, schema);
            }
            else
            {
                table = BuildNewTable(extract, schema);
            }

            return new ExtractWriter<T>(table, schema);
        }

        private Table OpenExistingTable(Extract extract, ExtractTableSchema<T> schema)
        {
            Table table = extract.openTable(Name);
            TableDefinition tableDefinition = table.getTableDefinition();

            try
            {
                if (!schema.IsCompatibleWith(tableDefinition))
                {
                    throw new ExtractTableLoadException(String.Format("Existing extract table is incompatible with model '{0}'", Name));
                }

                return table;
            }
            finally
            {
                tableDefinition.close();
            }
        }

        private Table BuildNewTable(Extract extract, ExtractTableSchema<T> schema)
        {
            var tableDefinition = new TableDefinition();

            try
            {
                tableDefinition.setDefaultCollation(Collation.EN_US);

                foreach (var column in schema.MappedColumns.Select(item => item.Column))
                {
                    tableDefinition.addColumn(column.Name, column.ExtractType);
                }

                return extract.addTable(Name, tableDefinition);
            }
            finally
            {
                tableDefinition.close();
            }
        }
    }
}