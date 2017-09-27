using System;
using System.IO;
using System.Net;
using System.Xml.Serialization;

namespace Tableau.RestApi.Extensions
{
    /// <summary>
    /// Extension methods for the WebResponse class.
    /// </summary>
    internal static class WebResponseExtensions
    {
        /// <summary>
        /// Deserializes a WebResponse into a tsResponse object.
        /// </summary>
        /// <param name="response">The WebResponse to be deserialized.</param>
        /// <returns>The WebResponse deserialized into an instance of tsResponse.</returns>
        public static tsResponse Deserialize(this WebResponse response)
        {
            var deserializer = new XmlSerializer(typeof(tsResponse));

            var responseStream = response.GetResponseStream();
            if (responseStream == null)
            {
                return null;
            }

            using (var responseReader = new StreamReader(responseStream))
            {
                try
                {
                    return deserializer.Deserialize(responseReader) as tsResponse;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(String.Format("Failed to deserialize response from request to '{0}': {1}", response.ResponseUri, ex.Message), ex);
                }
            }
        }
    }
}