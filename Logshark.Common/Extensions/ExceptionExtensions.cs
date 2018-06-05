using System;
using System.Text;

namespace Logshark.Common.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Retrieves the exception message for the given exception.  Any inner exceptions will be appended.
        /// </summary>
        public static string GetFlattenedMessage(this Exception ex)
        {
            StringBuilder sb = new StringBuilder(ex.Message);

            if (ex is AggregateException)
            {
                var aggregateException = (AggregateException)ex;
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    sb.Append(String.Concat(Environment.NewLine, "\tInner exception: ", innerException.Message));
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendFormat(" ({0})", ex.InnerException.Message);
            }

            return sb.ToString();
        }
    }
}