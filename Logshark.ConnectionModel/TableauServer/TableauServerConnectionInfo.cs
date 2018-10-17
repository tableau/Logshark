using Logshark.Config;
using System;

namespace Logshark.ConnectionModel.TableauServer
{
    public class TableauServerConnectionInfo : ITableauServerConnectionInfo
    {
        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public string Site { get; set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public int PublishingTimeoutSeconds { get; private set; }
        public string Scheme { get; private set; }
        public Uri Uri { get; private set; }

        public TableauServerConnectionInfo(TableauServerConnection tableauConfig)
        {
            Scheme = tableauConfig.Protocol;
            Hostname = tableauConfig.Server.Server;
            Port = tableauConfig.Server.Port;
            Site = tableauConfig.Server.Site;
            Username = tableauConfig.User.Username;
            Password = tableauConfig.User.Password;
            PublishingTimeoutSeconds = tableauConfig.PublishingTimeoutSeconds;
            Uri = GetUri();
        }

        public string BuildTableauProjectSearchLink(string searchParameter)
        {
            if (String.IsNullOrWhiteSpace(searchParameter))
            {
                throw new ArgumentException("Must supply a valid project search parameter!", "searchParameter");
            }

            if (Site.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                return String.Format("{0}#/projects?search={1}", GetUri(), searchParameter);
            }
            else
            {
                return String.Format("{0}#/site/{1}/projects?search={2}", GetUri(), Site, searchParameter);
            }
        }

        public override string ToString()
        {
            return String.Format(@"{0}@{1}:{2}\{3} [{4}]", Username, Hostname, Port, Site, Scheme);
        }
        
        private Uri GetUri()
        {
            var builder = new UriBuilder
            {
                Scheme = Scheme,
                Host = Hostname,
                Port = Port
            };

            return builder.Uri;
        }
    }
}