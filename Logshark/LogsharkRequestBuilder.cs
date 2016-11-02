using Logshark.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark
{
    public interface ILogsharkRequestBuilder
    {
        ILogsharkRequestBuilder WithPostgresDatabaseName(string databaseName);
        ILogsharkRequestBuilder WithProjectName(string projectName);
        ILogsharkRequestBuilder WithSiteName(string siteName);
        ILogsharkRequestBuilder WithRunId(string runId);
        ILogsharkRequestBuilder WithForceParse(bool forceParse);
        ILogsharkRequestBuilder WithProcessDebug(bool processDebug);
        ILogsharkRequestBuilder WithDropParsedLogset(bool keepParsedLogset);
        ILogsharkRequestBuilder WithPublishWorkbooks(bool publishWorkbooks);
        ILogsharkRequestBuilder WithMetadata(IDictionary<string, object> metadataDictionary);
        ILogsharkRequestBuilder WithWorkbookTags(IEnumerable<string> workbookTags);
        ILogsharkRequestBuilder WithPluginCustomArguments(IDictionary<string, object> customArgDictionary);
        ILogsharkRequestBuilder WithPluginsToExecute(ICollection<string> pluginsToExecute);
        ILogsharkRequestBuilder WithProcessFullLogset(bool processFullLogset);
        ILogsharkRequestBuilder WithStartLocalMongo(bool startLocalMongo);
        ILogsharkRequestBuilder WithSource(string sourceName);
        ILogsharkRequestBuilder WithLocalMongoPort(int localMongoPort);

        LogsharkRequest GetRequest();
    }

    public class LogsharkRequestBuilder : ILogsharkRequestBuilder
    {
        private readonly LogsharkRequest request;

        public LogsharkRequestBuilder(string target, LogsharkConfiguration configuration)
        {
            request = new LogsharkRequest(target, configuration);
        }

        public ILogsharkRequestBuilder WithPostgresDatabaseName(string postgresDatabaseName)
        {
            if (LogsharkConstants.PROTECTED_DATABASE_NAMES.Contains(postgresDatabaseName, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException(String.Format("{0} is a protected database name and cannot be used as a Logshark destination!", postgresDatabaseName));
            }

            if (!String.IsNullOrWhiteSpace(postgresDatabaseName))
            {
                request.PostgresDatabaseName = postgresDatabaseName;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithProjectDescription(string projectDescription)
        {
            if (!String.IsNullOrWhiteSpace(projectDescription))
            {
                request.ProjectDescription = projectDescription;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithProjectName(string projectName)
        {
            if (!String.IsNullOrWhiteSpace(projectName))
            {
                request.ProjectName = projectName;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithSiteName(string siteName)
        {
            if (!String.IsNullOrWhiteSpace(siteName))
            {
                request.Configuration.TableauConnectionInfo.Site = siteName;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithProcessDebug(bool processDebug)
        {
            request.ProcessDebug = processDebug;
            return this;
        }

        public ILogsharkRequestBuilder WithRunId(string runId)
        {
            if (!String.IsNullOrWhiteSpace(runId))
            {
                request.RunId = runId;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithForceParse(bool forceParse)
        {
            request.ForceParse = forceParse;
            return this;
        }

        public ILogsharkRequestBuilder WithDropParsedLogset(bool dropParsedLogset)
        {
            request.DropMongoDBPostRun = dropParsedLogset;
            return this;
        }

        public ILogsharkRequestBuilder WithPublishWorkbooks(bool publishWorkbooks)
        {
            request.PublishWorkbooks = publishWorkbooks;
            return this;
        }

        public ILogsharkRequestBuilder WithMetadata(IDictionary<string, object> metadataDictionary)
        {
            if (metadataDictionary != null)
            {
                foreach (var item in metadataDictionary)
                {
                    request.Metadata.Add(item);
                }
            }
            return this;
        }

        public ILogsharkRequestBuilder WithWorkbookTags(IEnumerable<string> workbookTags)
        {
            if (workbookTags != null)
            {
                foreach (var tag in workbookTags)
                {
                    request.WorkbookTags.Add(tag);
                }
            }
            return this;
        }

        public ILogsharkRequestBuilder WithPluginCustomArguments(IDictionary<string, object> customArgDictionary)
        {
            if (customArgDictionary != null)
            {
                foreach (var customArg in customArgDictionary)
                {
                    request.PluginCustomArguments.Add(customArg);
                }
            }
            return this;
        }

        public ILogsharkRequestBuilder WithPluginsToExecute(ICollection<string> pluginsToExecute)
        {
            if (pluginsToExecute != null)
            {
                foreach (var pluginToExecute in pluginsToExecute)
                {
                    request.PluginsToExecute.Add(pluginToExecute);
                }
            }
            return this;
        }

        public ILogsharkRequestBuilder WithProcessFullLogset(bool processFullLogset)
        {
            request.ProcessFullLogset = processFullLogset;
            return this;
        }

        public ILogsharkRequestBuilder WithStartLocalMongo(bool startLocalMongo)
        {
            if (!request.Configuration.LocalMongoOptions.AlwaysUseLocalMongo)
            {
                request.StartLocalMongo = startLocalMongo;
            }
            return this;
        }

        public ILogsharkRequestBuilder WithSource(string sourceName)
        {
            request.Source = sourceName;
            return this;
        }

        public ILogsharkRequestBuilder WithLocalMongoPort(int localMongoPort)
        {
            request.LocalMongoPort = localMongoPort;
            return this;
        }

        public LogsharkRequest GetRequest()
        {
            return request;
        }
    }
}
