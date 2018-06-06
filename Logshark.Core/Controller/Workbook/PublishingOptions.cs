using Logshark.Common.Extensions;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Workbook
{
    internal class PublishingOptions
    {
        public bool PublishWorkbooks { get; protected set; }

        public string ProjectName { get; protected set; }

        public string ProjectDescription { get; protected set; }

        public ISet<string> Tags { get; protected set; }

        public bool OverwriteExistingWorkbooks { get; protected set; }

        public PublishingOptions(bool publishWorkbooks, string projectName, string projectDescription = null, IEnumerable<string> tags = null, bool overwriteExistingWorkbooks = true)
        {
            PublishWorkbooks = publishWorkbooks;

            if (String.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Must supply a non-empty Tableau project name!", "projectName");
            }

            ProjectName = projectName;
            ProjectDescription = String.IsNullOrWhiteSpace(projectDescription) ? "" : projectDescription;
            Tags = new HashSet<string>();
            if (tags != null)
            {
                Tags.AddRange(tags);
            }

            OverwriteExistingWorkbooks = overwriteExistingWorkbooks;
        }
    }
}