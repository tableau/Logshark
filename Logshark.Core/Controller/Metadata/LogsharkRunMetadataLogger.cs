using log4net;
using Logshark.Core.Exceptions;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Logshark.Core.Controller.Metadata
{
    internal class LogsharkRunMetadataLogger : ILogsharkRunMetadataWriter
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void WriteMetadata(LogsharkRunContext run)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(run);
                Log.DebugFormat("Started phase {0}: {1}", run.CurrentPhase, JsonConvert.SerializeObject(metadata));
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to write Logshark metadata for run '{0}': {1}", run.Id, ex.Message));
            }
        }
    }
}