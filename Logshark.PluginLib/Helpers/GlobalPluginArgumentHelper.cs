using log4net;
using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.PluginLib.Helpers
{
    /// <summary>
    /// Helper methods for returning specific global arguments were provided by the user.
    /// </summary>
    public static class GlobalPluginArgumentHelper
    {
        private static readonly string persisterPoolSizeKey = "Global.PersisterPoolSize";
        private static readonly string persisterBatchSizeKey = "Global.PersisterBatchSize";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Returns the user-specified global persister pool setting if one exists, or defaultIfNotFound if the user did not provide one.
        /// </summary>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <param name="defaultIfNotFound">Default persister pool size to return if it wasn't provided as an argument by the user.</param>
        /// <returns></returns>
        public static int GetPersisterPoolSize(IPluginRequest pluginRequest, int defaultIfNotFound = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE)
        {
            try
            {
                return PluginArgumentHelper.GetAsInt(persisterPoolSizeKey, pluginRequest);
            }
            catch (FormatException)
            {
                Log.InfoFormat("{0} was specified but did not contain a valid integer value. Using default value of {1}.", persisterPoolSizeKey, defaultIfNotFound);
                return defaultIfNotFound;
            }
            catch (KeyNotFoundException)
            {
                return defaultIfNotFound;
            }
        }

        /// <summary>
        /// Returns the user-specified global persister pool setting if one exists, or defaultIfNotFound if the user did not provide one.
        /// </summary>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <param name="defaultIfNotFound">Default persister batch size to return if it wasn't provided as an argument by the user.</param>
        /// <returns></returns>
        public static int GetPersisterBatchSize(IPluginRequest pluginRequest, int defaultIfNotFound = PluginLibConstants.DEFAULT_PERSISTER_MAX_BATCH_SIZE)
        {
            try
            {
                return PluginArgumentHelper.GetAsInt(persisterBatchSizeKey, pluginRequest);
            }
            catch (FormatException)
            {
                Log.InfoFormat("{0} was specified but did not contain a valid integer value. Using default value of {1}.", persisterBatchSizeKey);
                return defaultIfNotFound;
            }
            catch (KeyNotFoundException)
            {
                return defaultIfNotFound;
            }
        }
    }
}