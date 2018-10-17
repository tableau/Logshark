using Logshark.PluginLib.Extensions;
using Logshark.Plugins.ClusterController.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ClusterControllerPostgresAction : BaseClusterControllerEvent
    {
        public string Action { get; set; }

        public ClusterControllerPostgresAction()
        {
        }

        public ClusterControllerPostgresAction(BsonDocument document) : base(document)
        {
            Action = GetActionValue(document);
        }

        protected string GetActionValue(BsonDocument document)
        {
            string message = document.GetString("message");

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
    }
}