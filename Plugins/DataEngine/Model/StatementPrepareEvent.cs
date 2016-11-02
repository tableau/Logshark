namespace Logshark.Plugins.DataEngine.Model
{
    internal class StatementPrepareEvent
    {
        public int Guid { get; set; }
        public string Query { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}