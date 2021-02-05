using System;

namespace LogShark.Plugins.TabadminController
{
    public interface IBuildTracker
    {
        void AddBuild(DateTime timestamp, string build);
        string GetBuild(DateTime timestamp);
    }
}