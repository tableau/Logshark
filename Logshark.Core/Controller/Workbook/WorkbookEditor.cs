using log4net;
using Logshark.ConnectionModel.Postgres;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;

namespace Logshark.Core.Controller.Workbook
{
    /// <summary>
    /// Handles editing of workbook XML.
    /// </summary>
    public sealed class WorkbookEditor
    {
        private const string ConnectionElementXpath = ".//.//connection";
        private const string ThumbnailsElementXpath = ".//thumbnails";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string WorkbookName { get; private set; }
        public XmlDocument WorkbookXml { get; private set; }

        public WorkbookEditor(string workbookName, XmlDocument workbookXml)
        {
            WorkbookName = workbookName;
            WorkbookXml = workbookXml;
        }

        /// <summary>
        /// Update all the Postgres connection XML nodes to point to a different backing database.
        /// </summary>
        /// <param name="postgresConnection">The connection information about the Postgres server.</param>
        /// <param name="databaseName">The database name to link up to.</param>
        public void ReplacePostgresConnections(PostgresConnectionInfo postgresConnection, string databaseName)
        {
            XmlNode root = WorkbookXml.DocumentElement;
            if (root == null)
            {
                return;
            }

            XmlNodeList connectionElements = root.SelectNodes(ConnectionElementXpath);
            if (connectionElements == null)
            {
                // No connections to edit; nothing to do.
                return;
            }
            foreach (XmlNode connectionElement in connectionElements)
            {
                ReplaceSinglePostgresConnection(connectionElement, postgresConnection, databaseName);
            }
        }

        /// <summary>
        /// Remove all cached thumbnail images from the workbook.
        /// </summary>
        public void RemoveThumbnails()
        {
            XmlNode root = WorkbookXml.DocumentElement;
            if (root == null)
            {
                return;
            }

            XmlNode thumbnailsRootElement = root.SelectSingleNode(ThumbnailsElementXpath);
            if (thumbnailsRootElement != null)
            {
                thumbnailsRootElement.RemoveAll();
            }
        }

        /// <summary>
        /// Saves the workbook XML to a specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory to save the workbook to.</param>
        /// <returns>Path to outputted workbook.</returns>
        public string Save(string directoryPath)
        {
            string workbookFilePath = Path.Combine(directoryPath, WorkbookName);
            WorkbookXml.Save(workbookFilePath);
            return workbookFilePath;
        }

        /// <summary>
        /// Updates a single workbook connection element in the XML to point to a different Postgres database.
        /// </summary>
        /// <param name="connectionElement"></param>
        /// <param name="postgresConnection"></param>
        /// <param name="databaseName"></param>
        private void ReplaceSinglePostgresConnection(XmlNode connectionElement, PostgresConnectionInfo postgresConnection, string databaseName)
        {
            if (!IsValidPostgresConnectionElement(connectionElement))
            {
                return;
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
        }

        private bool IsValidPostgresConnectionElement(XmlNode connectionElement)
        {
            if (connectionElement.Attributes == null || connectionElement.Attributes["class"].Value != "postgres")
            {
                return false;
            }
            return true;
        }
    }
}