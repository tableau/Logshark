using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System.Collections.Generic;

namespace Logshark.Plugins.Backgrounder.Model
{
    internal class BackgrounderSubscriptionJobDetail : BackgrounderJobDetail
    {
        public long BackgrounderJobId { get; set; }

        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string SmtpServer { get; set; }
        public string SubscriptionName { get; set; }
        public string VizqlSessionId { get; set; }

        public BackgrounderSubscriptionJobDetail()
        {
        }

        public BackgrounderSubscriptionJobDetail(BackgrounderJob backgrounderJob, IList<BsonDocument> subscriptionJobEvents)
        {
            BackgrounderJobId = backgrounderJob.JobId;

            foreach (var subscriptionJobEvent in subscriptionJobEvents)
            {
                string className = BsonDocumentHelper.GetString("class", subscriptionJobEvent);
                string message = BsonDocumentHelper.GetString("message", subscriptionJobEvent);

                if (className.Equals(BackgrounderConstants.VqlSessionServiceClass))
                {
                    if (message.StartsWith("Created session id:"))
                    {
                        VizqlSessionId = message.Split(':')[1];
                    }
                }

                if (className.Equals(BackgrounderConstants.SubscriptionRunnerClass) || className.Equals(BackgrounderConstants.EmailHelperClass))
                {
                    if (message.StartsWith("Sending email from"))
                    {
                        var chunks = message.Replace("Sending email from", "").Replace(" to ", " ").Replace(" from server ", " ").Trim().Split(' ');
                        SenderEmail = chunks[0];
                        RecipientEmail = chunks[1];
                        SmtpServer = chunks[2];
                    }

                    if (message.StartsWith("Starting subscription"))
                    {
                        SubscriptionName = message.Split('"')[1];
                    }
                }
            }
        }
    }
}