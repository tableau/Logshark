namespace Logshark.Core.Controller.Initialization
{
    internal interface IRunInitializer
    {
        RunInitializationResult Initialize(RunInitializationRequest request);
    }
}