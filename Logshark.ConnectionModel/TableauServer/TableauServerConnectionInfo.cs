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
        public static string TableauFormUser = null;
        public static string TableauFormPass = null;
        public TableauServerConnectionInfo(TableauServerConnection tableauConfig)
        {
            Scheme = tableauConfig.Protocol;
            Hostname = tableauConfig.Server.Server;
            Port = tableauConfig.Server.Port;
            Site = tableauConfig.Server.Site;
            if (tableauConfig.User.Username == " " || tableauConfig.User.Password == " ")
            {
               
                Form1 passwordPopup = new Form1();
                /* In case a value is maintained in the configuration xml, display the value of the logon details in the textboxes */
                passwordPopup.textBox1.Text = tableauConfig.User.Username;
                passwordPopup.textBox2.Text = tableauConfig.User.Password;

                passwordPopup.ShowDialog();
                
                Username = TableauFormUser;
                Password = TableauFormPass;
            }
            else {
                Username = tableauConfig.User.Username;
                Password = tableauConfig.User.Password;
            }
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