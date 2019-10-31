namespace Tableau.ExtractApi.TableSchema
{
    internal sealed class MappedColumnDefinition
    {
        public int ModelPropertyIndex { get; private set; }

        public ColumnDefinition Column { get; private set; }

        public MappedColumnDefinition(int modelPropertyIndex, ColumnDefinition column)
        {
            ModelPropertyIndex = modelPropertyIndex;
            Column = column;
        }
    }
}