using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Containers;
using LogShark.Writers.Containers;

namespace LogShark
{
    public class PluginsExecutionResults
    {
        private readonly Dictionary<string, SinglePluginExecutionResults> _results;

        public PluginsExecutionResults()
        {
            _results = new Dictionary<string, SinglePluginExecutionResults>();
        }

        public void AddSinglePluginResults(string pluginName, SinglePluginExecutionResults results)
        {
            if (_results.ContainsKey(pluginName))
            {
                throw new ArgumentException($"{nameof(PluginsExecutionResults)} already contains results for '{pluginName}' plugin");
            }
            
            _results.Add(pluginName, results);
        }

        public IList<string> GetSortedTagsFromAllPlugins()
        {
            return _results.Values
                .Where(result => result.HasAdditionalTags)
                .SelectMany(result => result.AdditionalTags)
                .OrderBy(tag => tag)
                .ToList();
        }

        public WritersStatistics GetWritersStatistics()
        {
            var dict = _results.Values
                .SelectMany(result => result.WritersStatistics)
                .ToDictionary(stat => stat.DataSetInfo, stat => stat);
            
            return new WritersStatistics(dict);
        }
    }
}