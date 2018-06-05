using System;
using System.IO;
using System.Text;
using Tableau.RestApi.Extensions;

namespace Tableau.RestApi.Helpers
{
    /// <summary>
    /// Handles constructing requests for publishing workbooks.
    /// </summary>
    internal static class PublishRequestBuilder
    {
        /// <summary>
        /// Builds the ugly multi-part request body for a Publish Workbook request. These requests are limited to 64Mb TWB files.
        /// </summary>
        /// <param name="workbookFilePath">The file path to the workbook.</param>
        /// <param name="requestMetadata">The tsRequest element containing metadata about the workbook.</param>
        /// <param name="boundaryString">A unique string to be used to separate the sections of the request bdoy.</param>
        /// <returns></returns>
        public static byte[] BuildFileUploadBody(string workbookFilePath, tsRequest requestMetadata, string boundaryString)
        {
            // Sanity check.
            if (!File.Exists(workbookFilePath))
            {
                throw new ArgumentException(String.Format("File '{0}' does not exist!", workbookFilePath));
            }

            using (FileStream fs = new FileStream(workbookFilePath, FileMode.Open, FileAccess.Read))
            using (MemoryStream rs = new MemoryStream())
            {
                // Compose the prefix to the actual workbook data.
                StringBuilder prefix = new StringBuilder();
                prefix.Append(BuildPayloadHeader(boundaryString));
                prefix.AppendLine(requestMetadata.SerializeBodyToString());
                prefix.AppendLine(String.Format("--{0}", boundaryString));
                prefix.AppendLine(String.Format("Content-Disposition: name=\"tableau_workbook\"; filename=\"{0}\"", Path.GetFileName(workbookFilePath)));
                prefix.AppendLine("Content-Type: application/octet-stream");
                prefix.AppendLine();

                byte[] prefixBytes = Encoding.UTF8.GetBytes(prefix.ToString());
                rs.Write(prefixBytes, 0, prefixBytes.Length);

                // Write workbook data.
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                {
                    rs.Write(buffer, 0, bytesRead);
                }

                // Compose suffix region that indicates the end of the request.
                byte[] suffix = Encoding.ASCII.GetBytes(String.Format("\r\n--{0}--", boundaryString));
                rs.Write(suffix, 0, suffix.Length);

                return rs.ToArray();
            }
        }

        public static byte[] BuildMultiPartAppendBody(string workbookFilePath, string boundaryString, FileStream fileStream)
        {
            using (MemoryStream rs = new MemoryStream())
            {
                // Compose the prefix to the actual workbook data.
                StringBuilder prefix = new StringBuilder();
                prefix.Append(BuildPayloadHeader(boundaryString));
                prefix.AppendLine();
                prefix.AppendLine(String.Format("--{0}", boundaryString));
                prefix.AppendLine(String.Format("Content-Disposition: name=\"tableau_file\"; filename=\"{0}\"", Path.GetFileName(workbookFilePath)));
                prefix.AppendLine("Content-Type: application/octet-stream");
                prefix.AppendLine();

                byte[] prefixBytes = Encoding.UTF8.GetBytes(prefix.ToString());
                rs.Write(prefixBytes, 0, prefixBytes.Length);

                // Write workbook data.
                var buffer = new byte[4096];
                int bytesRead;
                int totalBytesRead = 0;
                while (totalBytesRead + buffer.Length < Constants.MaxFilePartSize &&
                    (bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    totalBytesRead += bytesRead;
                    rs.Write(buffer, 0, bytesRead);
                }

                // Compose suffix region that indicates the end of the request.
                byte[] suffix = Encoding.ASCII.GetBytes(String.Format("\r\n--{0}--", boundaryString));
                rs.Write(suffix, 0, suffix.Length);

                return rs.ToArray();
            }
        }

        public static byte[] BuildFinishUploadBody(string workbookFilePath, tsRequest requestMetadata, string boundaryString)
        {
            using (MemoryStream rs = new MemoryStream())
            {
                // Compose the prefix to the actual workbook data.
                StringBuilder prefix = new StringBuilder();
                prefix.Append(BuildPayloadHeader(boundaryString));
                prefix.AppendLine(requestMetadata.SerializeBodyToString());
                prefix.AppendLine(String.Format("--{0}--", boundaryString));

                byte[] prefixBytes = Encoding.UTF8.GetBytes(prefix.ToString());
                rs.Write(prefixBytes, 0, prefixBytes.Length);

                return rs.ToArray();
            }
        }

        private static StringBuilder BuildPayloadHeader(string boundaryString)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("--{0}", boundaryString));
            sb.AppendLine("Content-Disposition: name=\"request_payload\"");
            sb.AppendLine("Content-Type: text/xml");
            sb.AppendLine();
            return sb;
        }
    }
}