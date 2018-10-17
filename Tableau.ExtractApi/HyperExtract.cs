using com.tableausoftware.hyperextract;
using Optional;
using Optional.Utilities;
using System;
using System.IO;
using Tableau.ExtractApi.Exceptions;
using Tableau.ExtractApi.TableSchema;

namespace Tableau.ExtractApi
{
    public sealed class HyperExtract<T> : IExtract<T> where T : new()
    {
        private readonly Extract extract;
        private readonly ExtractTable<T> table;

        public HyperExtract(string destinationFilename, string customTempDirectoryPath = null, string customLogDirectoryPath = null)
        {
            extract = InitializeExtract(destinationFilename, customTempDirectoryPath, customLogDirectoryPath);
            table = InitializeTable(extract);
        }

        public Option<T, ExtractInsertionException> Insert(T item)
        {
            return Safe.Try(() => table.Insert(item).Map(_ => item))
                       .MapException(ex => new ExtractInsertionException(String.Format("Failed to insert item into extract: {0}", ex.Message), ex)).Flatten();
        }

        public void Dispose()
        {
            try
            {
                if (extract != null)
                {
                    extract.close();
                    ExtractAPI.cleanup();
                }
            }
            catch { }
        }

        private static Extract InitializeExtract(string filename, string customTempDirectoryPath = null, string customLogDirectoryPath = null)
        {
            if (String.IsNullOrWhiteSpace(filename))
            {
                throw new ExtractInitializationException("Failed to initialize extract: Must provide a valid absolute path to an extract file");
            }

            try
            {
                // Set environment variables used by the Extract API
                // If these are not set, the Extract API will utilize the current working directory
                if (!String.IsNullOrWhiteSpace(customTempDirectoryPath) && Directory.Exists(customTempDirectoryPath))
                {
                    Environment.SetEnvironmentVariable("TAB_SDK_TMPDIR", customTempDirectoryPath);
                }
                if (!String.IsNullOrWhiteSpace(customLogDirectoryPath) && Directory.Exists(customLogDirectoryPath))
                {
                    Environment.SetEnvironmentVariable("TAB_SDK_LOGDIR", customLogDirectoryPath);
                }

                ExtractAPI.initialize();

                Directory.CreateDirectory(Directory.GetParent(filename).FullName);
                return new Extract(filename);
            }
            catch (Exception ex)
            {
                throw new ExtractInitializationException(String.Format("Failed to initialize extract '{0}': {1}", filename, ex.Message), ex);
            }
        }

        private static ExtractTable<T> InitializeTable(Extract extract)
        {
            try
            {
                return new ExtractTable<T>(extract);
            }
            catch (Exception ex)
            {
                throw new ExtractTableCreationException(String.Format("Failed to initialize table for model '{0}': {1}", typeof(T).Name, ex.Message), ex);
            }
        }
    }
}