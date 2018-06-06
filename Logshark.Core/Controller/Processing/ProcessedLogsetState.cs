namespace Logshark.Core.Controller.Processing
{
    public enum ProcessedLogsetState
    {
        NonExistent,
        InFlight,
        Corrupt,
        Incomplete,
        Indeterminable,
        Valid
    }
}