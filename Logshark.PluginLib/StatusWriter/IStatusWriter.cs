namespace Logshark.PluginLib.StatusWriter
{
    /// <summary>
    /// Represents a status writer about some kind of state.
    /// </summary>
    public interface IStatusWriter
    {
        void WriteStatus();

        void Dispose();
    }
}