using System;

namespace LogShark.Plugins.Backgrounder.Model
{
    public class BackgrounderSubscriptionJobDetail
    {
        public string BackgrounderJobId { get; set; }
        // Worker (node) that ran this job, stamped from the matched BackgrounderJob at drain time. Lets
        // subscription-job rows be linked to the rest of a worker's activity, consistent with BackgrounderJob.WorkerId.
        public string WorkerId { get; set; }
        public string RecipientEmail { get; set; }
        public string SenderEmail { get; set; }
        public string SmtpServer { get; set; }
        public string SubscriptionName { get; set; }
        public string VizqlSessionId { get; set; }
        public DateTime Timestamp { get; set; }

        public void MergeInfo(BackgrounderSubscriptionJobDetail otherEvent)
        {
            if (otherEvent == null)
            {
                return;
            }
            
            RecipientEmail = RecipientEmail ?? otherEvent.RecipientEmail;
            SenderEmail = SenderEmail ?? otherEvent.SenderEmail;
            SmtpServer = SmtpServer ?? otherEvent.SmtpServer;
            SubscriptionName = SubscriptionName ?? otherEvent.SubscriptionName;
            VizqlSessionId = VizqlSessionId ?? otherEvent.VizqlSessionId;
        }
    }
}