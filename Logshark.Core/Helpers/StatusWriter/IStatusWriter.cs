namespace Logshark.Core.Helpers.StatusWriter
{
    /// <summary>
    /// Represents a startable & stoppable status writer about some kind of state.
    /// </summary>
    public interface IStatusWriter
    {
        void Start();

        void Stop();

        void WriteStatus();

        void Dispose();
    }
}