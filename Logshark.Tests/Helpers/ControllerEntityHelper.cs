using Logshark.RequestModel;
using Logshark.RequestModel.Config;

namespace Logshark.Tests.Helpers
{
    internal static class ControllerEntityHelper
    {
        public static LogsharkRequest GetMockRequest()
        {
            return new LogsharkRequestBuilder("841344039a95e108f623f941444b60eb", GetMockConfiguration())
                        .WithProjectName("Logshark Test Project")
                        .GetRequest();
        }

        public static LogsharkConfiguration GetMockConfiguration()
        {
            return LogsharkConfigReader.LoadConfiguration();
        }
    }
}