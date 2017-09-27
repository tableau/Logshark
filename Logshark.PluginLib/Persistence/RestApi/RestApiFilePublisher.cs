using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace Logshark.PluginLib.Persistence.RestApi
{
    public class RestApiFilePublisher
    {
        private readonly RestClient client;

        public RestApiFilePublisher(string baseUri)
        {
            client = new RestClient(baseUri);
        }

        public IRestResponse PostFile(string filePath, string resourceUri, string contentType, IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null)
        {
            // Validate arguments.
            if (!File.Exists(filePath))
            {
                throw new ArgumentException(String.Format("File '{0}' does not exist!", filePath), "filePath");
            }

            if (String.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Must set a request content type!", "contentType");
            }

            // Construct request.
            var request = new RestRequest(resourceUri, Method.POST);

            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> parameter in parameters)
                {
                    request.AddParameter(parameter.Key, parameter.Value);
                }
            }
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            request.AddHeader("Content-Type", contentType);
            request.AddParameter(contentType, File.ReadAllBytes(filePath), ParameterType.RequestBody);

            // Execute request.
            return client.Execute(request);
        }
    }
}