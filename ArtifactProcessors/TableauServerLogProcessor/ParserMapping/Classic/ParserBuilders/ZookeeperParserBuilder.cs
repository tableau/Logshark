using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "zookeeper" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class ZookeeperParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^zookeeper-.*log.*", typeof(ZookeeperParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}