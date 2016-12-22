using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Tableau.RestApi.Extensions;

namespace Tableau.RestApi
{
    public class ApiRequest
    {
        public Uri Uri { get; protected set; }
        public HttpMethod Method { get; protected set; }
        public WebHeaderCollection Headers { get; protected set; }
        public string ContentType { get; protected set; }
        public byte[] Body { get; protected set; }
        public int? Timeout { get; protected set; }

        public ApiRequest(Uri uri, HttpMethod method, WebHeaderCollection headers = null, byte[] body = null, string authToken = null, string contentType = null, int? timeout = null)
        {
            Uri = uri;
            Method = method;
            if (headers == null)
            {
                Headers = new WebHeaderCollection();
            }
            else
            {
                Headers = headers;
            }
            Body = body;
            if (!String.IsNullOrWhiteSpace(authToken))
            {
                Headers.Add(Constants.AuthenticationHeaderKey, authToken);
            }
            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                if (String.IsNullOrWhiteSpace(contentType))
                {
                    ContentType = Constants.DefaultContentType;
                }
                else
                {
                    ContentType = contentType;
                }
            }
            Timeout = timeout;
        }

        public tsResponse TryIssueRequest(string requestFailureMessage, int maxAttempts = Constants.DefaultMaxRequestAttempts)
        {
            try
            {
                return IssueRequest(maxAttempts);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException(String.Format("{0}: {1}", requestFailureMessage, ex.Message), ex);
            }
        }

        public tsResponse IssueRequest(int maxAttempts = Constants.DefaultMaxRequestAttempts)
        {
            int attempt = 1;
            while (attempt <= maxAttempts)
            {
                HttpWebRequest request = this.ToHttpWebRequest();
                try
                {
                    HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                    if (!IsSuccessfulStatusCode(response.StatusCode))
                    {
                        throw new HttpRequestException(String.Format("Received non-successful status code {0} ({1})", response.StatusCode, response.StatusDescription));
                    }

                    if (response.ContentLength == 0)
                    {
                        return new tsResponse();
                    }

                    return response.Deserialize();
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts)
                    {
                        throw new HttpRequestException(String.Format("Failed to retrieve successful response for {0} request to '{1}' after {2} attempts: {3}",
                                                                     Method, Uri, maxAttempts, ex.Message), ex);
                    }
                    attempt++;
                }
            }

            throw new HttpRequestException(String.Format("Failed to retrieve successful response for {0} request to '{1}' after {2} attempts.", Method, Uri, maxAttempts));
        }

        protected HttpWebRequest ToHttpWebRequest()
        {
            var webRequest = WebRequest.Create(Uri) as HttpWebRequest;

            webRequest.Method = Method.ToString();
            webRequest.Headers.Add(Headers);
            webRequest.ContentType = ContentType;

            if (Timeout.HasValue && Timeout > 0)
            {
                webRequest.Timeout = Timeout.Value;
            }

            if (HasBody())
            {
                webRequest.ContentLength = Body.Length;
                using (Stream requestWriter = webRequest.GetRequestStream())
                {
                    foreach (var byteData in Body)
                    {
                        requestWriter.WriteByte(byteData);
                    }
                }
            }

            return webRequest;
        }

        protected bool IsSuccessfulStatusCode(HttpStatusCode code)
        {
            return ((int)code >= 200) && ((int)code <= 299);
        }

        protected bool HasBody()
        {
            return Body != null && (Method == HttpMethod.Post || Method == HttpMethod.Put);
        }
    }
}