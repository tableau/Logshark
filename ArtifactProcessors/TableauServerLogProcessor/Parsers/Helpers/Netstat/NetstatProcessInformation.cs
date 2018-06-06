namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat
{
    internal class NetstatProcessInformation
    {
        public int? ProcessId { get; protected set; }

        public string ProcessName { get; protected set; }

        public NetstatProcessInformation(int? processId, string processName)
        {
            ProcessId = processId;
            ProcessName = processName;
        }
    }
}