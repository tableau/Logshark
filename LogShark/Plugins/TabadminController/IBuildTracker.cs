using System;
using System.Collections.Generic;

namespace LogShark.Plugins.TabadminController
{
    public interface IBuildTracker
    {
        void AddBuild(DateTime timestamp, string build);
        IEnumerable<TabadminControllerBuildRecord> GetBuildRecords();
    }
}