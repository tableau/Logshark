namespace LogShark.Tests.Extensions
{
    public static class ProcessingNotificationsCollectorExtensions
    {
        public static bool ReceivedAnything(this ProcessingNotificationsCollector processingNotificationsCollector)
        {
            return processingNotificationsCollector.TotalErrorsReported != 0 ||
                   processingNotificationsCollector.TotalWarningsReported != 0;
        }
    }
}