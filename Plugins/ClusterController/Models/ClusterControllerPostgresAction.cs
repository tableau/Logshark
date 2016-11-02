using Logshark.PluginLib.Helpers;
using Logshark.Plugins.ClusterController.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ClusterControllerPostgresAction : ClusterControllerEvent
    {
        [Index]
        public string Action { get; set; }

        public ClusterControllerPostgresAction()
        {
        }

        public ClusterControllerPostgresAction(BsonDocument document, Guid logsetHash)
            : base(document, logsetHash)
        {
            Action = GetActionValue(document);
            EventHash = GetEventHash();
        }

        protected string GetActionValue(BsonDocument document)
        {
            String message = BsonDocumentHelper.GetString("message", document);

            switch (message)
            {
                case ClusterControllerConstants.POSTGRES_START_AS_MASTER:
                    return "StartAsMaster";

                case ClusterControllerConstants.POSTGRES_FAILOVER_AS_MASTER:
                    return "FailoverAsMaster";

                case ClusterControllerConstants.POSTGRES_START_AS_SLAVE:
                    return "StartAsSlave";

                case ClusterControllerConstants.POSTGRES_STOP:
                    return "Stop";

                case ClusterControllerConstants.POSTGRES_RESTART:
                    return "Restart";

                default:
                    throw new Exception("No action for log message: " + message);
            }
        }

        protected Guid GetEventHash()
        {
            return HashHelper.GenerateHashGuid(Timestamp, Action, Worker, Filename, LineNumber);
        }
    }
}