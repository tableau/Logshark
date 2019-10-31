using log4net;
using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using Tableau.ExtractApi;

namespace Logshark.PluginLib.Persistence.Extract
{
    public class ExtractPersister<T> : IPersister<T> where T : new()
    {
        protected readonly HyperExtract<T> extract;
        protected readonly Action<T> insertionCallback;
        protected readonly ILog Log;

        private bool disposed;

        public long ItemsPersisted { get; protected set; }

        public ExtractPersister(string extractFilePath,
                                Action<T> insertionCallback = null,
                                ILog log = null,
                                string customTempDirectoryPath = null,
                                string customLogDirectoryPath = null)
        {
            extract = new HyperExtract<T>(extractFilePath, customTempDirectoryPath, customLogDirectoryPath);
            this.insertionCallback = insertionCallback ?? (_ => { });
            Log = log ?? LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Enqueue(IEnumerable<T> items)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    Enqueue(item);
                }
            }
        }

        public void Enqueue(T item)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (item != null)
            {
                var insertionResult = extract.Insert(item);
                insertionResult.Match(
                    some: insertedItem =>
                    {
                        insertionCallback(insertedItem);
                        ItemsPersisted++;
                    },
                    none: ex => Log.ErrorFormat("Failed to insert item '{0}' into extract: {1}", item.ToString(), ex.Message)
                );
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Log.Debug("Shutting down extract writer and waiting for insertion queue to flush.  This may take some time..");
                    extract.Dispose();
                    Log.Debug("Extract writer successfully shut down!");
                }

                disposed = true;
            }
        }

        #endregion IDisposable Implementation
    }
}