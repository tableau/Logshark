namespace LogShark.Plugins.Backgrounder.Model
{
    public class BackgrounderSubscriptionJobDetail
    {
        public long BackgrounderJobId { get; set; }
        public string RecipientEmail { get; set; }
        public string SenderEmail { get; set; }
        public string SmtpServer { get; set; }
        public string SubscriptionName { get; set; }
        public string VizqlSessionId { get; set; }

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