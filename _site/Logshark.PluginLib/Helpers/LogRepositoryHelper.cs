using log4net;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System.Linq;

namespace Logshark.PluginLib.Helpers
{
    internal static class LogRepositoryHelper
    {
        internal static ILoggerRepository GetOrCreateRepository(string name)
        {
            if (!RepositoryExists(name))
            {
                // Other logging frameworks harnessing plugin initialization can cause unexpected failures, so we just make best-effort here.
                try
                {
                    InitializeRepository(name);
                }
                catch { }
            }
            return LogManager.GetRepository(name);
        }

        internal static bool RepositoryExists(string name)
        {
            return LogManager.GetAllRepositories().Any(repository => repository.Name == name);
        }

        internal static void InitializeRepository(string name)
        {
            ILoggerRepository repository = LogManager.CreateRepository(name);
            var hierarchy = (Hierarchy)repository;
            hierarchy.Threshold = Level.All;
        }
    }
}