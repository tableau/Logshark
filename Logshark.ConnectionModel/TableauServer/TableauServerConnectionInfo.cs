using Logshark.Config;
using System;

namespace Logshark.ConnectionModel.TableauServer
{
    public class TableauServerConnectionInfo
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Site { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Scheme { get; set; }
        public int PublishingTimeoutSeconds { get; set; }

        public TableauServerConnectionInfo(TableauServerConnection tableauConfig)
        {
            Scheme = tableauConfig.Protocol;
            Hostname = tableauConfig.Server.Server;
            Port = tableauConfig.Server.Port;
            Site = tableauConfig.Server.Site;
            Username = tableauConfig.User.Username;
            Password = tableauConfig.User.Password;
            PublishingTimeoutSeconds = tableauConfig.PublishingTimeoutSeconds;
        }

        public Uri ToUri()
        {
            var builder = new UriBuilder()
            {
                Scheme = Scheme,
                Host = Hostname,
                Port = Port
            };

            return builder.Uri;
        }

        public override string ToString()
        {
            return String.Format(@"{0}@{1}:{2}\{3} [{4}]", Username, Hostname, Port, Site, Scheme);
        }
    }
}