namespace Tableau.ExtractApi.Helpers
{
    public struct Unit
    {
        private static readonly Unit unit = new Unit();

        public static Unit Default { get { return unit; } }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public override int GetHashCode()
        {
            return default(int);
        }

        public override string ToString()
        {
            return "(Unit)";
        }
    }
}