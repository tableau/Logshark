using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Tableau.RestApi.Extensions
{
    /// <summary>
    /// Extension methods for the auto-generated tsRequest class.
    /// </summary>
    public static class tsRequestExtensions
    {
        /// <summary>
        /// Serializes the body of a tsRequest into a byte array.
        /// </summary>
        /// <param name="request">The request to serialize.</param>
        /// <param name="encoding">The encoding to use, e.g. ASCII or UTF-8.</param>
        /// <returns>Byte array representation of the body of the tsRequest.</returns>
        public static byte[] SerializeBody(this tsRequest request, string encoding = Constants.DefaultEncoding)
        {
            string requestBody;
            using (StringWriter sw = new StringWriter() { NewLine = "\r\n" })
            {
                XmlSerializer xsSubmit = new XmlSerializer(typeof(tsRequest));
                XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, request);
                    requestBody = sw.ToString();
                }
            }

            return Encoding.GetEncoding(encoding).GetBytes(requestBody);
        }

        /// <summary>
        /// Serializes the body of a tsRequest into string format.
        /// </summary>
        /// <param name="request">The request to serialize.</param>
        /// <param name="encoding">The string encoding to use, e.g. ASCII or UTF-8.</param>
        /// <returns></returns>
        public static string SerializeBodyToString(this tsRequest request, string encoding = Constants.DefaultEncoding)
        {
            return Encoding.GetEncoding(encoding).GetString(SerializeBody(request));
        }
    }
}