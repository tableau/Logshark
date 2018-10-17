using log4net;
using Logshark.ConnectionModel.Postgres;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Xml;

namespace Logshark.Core.Controller.Workbook
{
    /// <summary>
    /// Handles editing of workbook XML.
    /// </summary>
    public sealed class WorkbookXmlEditor
    {
        private const string ConnectionElementXpath = ".//.//connection";
        private const string ThumbnailsElementXpath = ".//thumbnails";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public XmlDocument WorkbookXml { get; private set; }

        public WorkbookXmlEditor(XmlDocument workbookXml)
        {
            WorkbookXml = workbookXml;
        }

        /// <summary>
        /// Update all the Postgres connection XML nodes to point to a different backing database.
        /// </summary>
        /// <param name="postgresConnection">The connection information about the Postgres server.</param>
        /// <param name="databaseName">The database name to link up to.</param>
        public XmlDocument UpdatePostgresConnections(PostgresConnectionInfo postgresConnection, string databaseName)
        {
            if (WorkbookXml == null)
            {
                return WorkbookXml;
            }

            XmlNode root = WorkbookXml.DocumentElement;
            if (root == null)
            {
                return WorkbookXml;
            }

            XmlNodeList connectionElements = root.SelectNodes(ConnectionElementXpath);
            if (connectionElements == null)
            {
                // No connections to edit; nothing to do.
                return WorkbookXml;
            }

            foreach (XmlNode connectionElement in connectionElements)
            {
                UpdatePostgresConnection(connectionElement, postgresConnection, databaseName);
            }

            return WorkbookXml;
        }

        /// <summary>
        /// Remove all cached thumbnail images from the workbook.
        /// </summary>
        public XmlDocument RemoveThumbnails()
        {
            if (WorkbookXml == null)
            {
                return WorkbookXml;
            }

            XmlNode root = WorkbookXml.DocumentElement;
            if (root == null)
            {
                return WorkbookXml;
            }

            XmlNode thumbnailsRootElement = root.SelectSingleNode(ThumbnailsElementXpath);
            if (thumbnailsRootElement != null)
            {
                thumbnailsRootElement.RemoveAll();
            }

            return WorkbookXml;
        }

        /// <summary>
        /// Updates a single workbook connection element in the XML to point to a different Postgres database.
        /// </summary>
        private XmlNode UpdatePostgresConnection(XmlNode connectionElement, PostgresConnectionInfo postgresConnection, string databaseName)
        {
            if (!IsValidPostgresConnectionElement(connectionElement))
            {
                return connectionElement;
            }

            // Construct dictionary of attributes that we need to update.
            var connectionInfoDictionary = new Dictionary<string, string>
            {
                { "dbname", databaseName },
                { "port", postgresConnection.Port.ToString() },
                { "server", postgresConnection.Hostname },
                { "username", postgresConnection.Username },
                { "password", postgresConnection.Password }
            };

            // Workbooks cannot be published to Tableau Server if their datasource is pointing to an explicitly local resource, so we make a good-faith effort to update their connections to a resolved hostname.
            if (postgresConnection.Hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                postgresConnection.Hostname.Equals("127.0.0.1"))
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(postgresConnection.Hostname);
                    if (!String.IsNullOrWhiteSpace(hostEntry.HostName))
                    {
                        connectionInfoDictionary["server"] = hostEntry.HostName;
                    }
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Unable to resolve configured self-referential Postgres hostname '{0}' to an externally-resolvable hostname: {1}.  Resulting workbook cannot be published to Tableau Server.", postgresConnection.Hostname, ex.Message);
                }
            }

            // Update the element to reflect the new connection information.
            foreach (var key in connectionInfoDictionary.Keys)
            {
                if (connectionElement.Attributes[key] != null)
                {
                    // Substitute the old value with the new one.
                    connectionElement.Attributes[key].Value = connectionInfoDictionary[key];
                }
                else
                {
                    // Attribute does not already exist -- create it and append it.
                    var attribute = WorkbookXml.CreateAttribute(key);
                    attribute.Value = connectionInfoDictionary[key];
                    connectionElement.Attributes.Append(attribute);
                }
            }

            return connectionElement;
        }

        private static bool IsValidPostgresConnectionElement(XmlNode connectionElement)
        {
            return connectionElement.Attributes != null && 
                   connectionElement.Attributes["class"] != null &&
                   connectionElement.Attributes["class"].Value == "postgres";
        }
    }
}