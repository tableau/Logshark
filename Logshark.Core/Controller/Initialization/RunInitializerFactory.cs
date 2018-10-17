using Logshark.Core.Controller.Initialization.Archive;
using Logshark.Core.Controller.Initialization.Hash;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;

namespace Logshark.Core.Controller.Initialization
{
    internal static class RunInitializerFactory
    {
        public static IRunInitializer GetRunInitializer(LogsharkRequestTarget target, LogsharkConfiguration config)
        {
            switch (target.Type)
            {
                case LogsetTarget.File:
                case LogsetTarget.Directory:
                    return new ArchiveRunInitializer(config.ApplicationTempDirectory);

                case LogsetTarget.Hash:
                    return new HashRunInitializer(config.MongoConnectionInfo);

                default:
                    throw new ArgumentException($"Cannot get run initializer for unknown target type '{target.Type}'", nameof(target));
            }
        }
    }
}