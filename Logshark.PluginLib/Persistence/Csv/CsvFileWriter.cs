using CsvHelper;
using CsvHelper.Configuration;
using Logshark.PluginLib.Model;
using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Logshark.PluginLib.Persistence.Csv
{
    public class CsvFileWriter<T> : IDisposable
    {
        protected readonly CsvWriter csvWriter;
        protected object writeLock = new object();
        private bool disposed;

        public string OutputFileLocation { get; private set; }

        public CsvFileWriter(string outputFileName, IPluginRequest pluginRequest, bool writeHeader = true, CsvClassMap<T> csvClassMap = null)
        {
            OutputFileLocation = Path.Combine(pluginRequest.OutputDirectory, outputFileName);
            csvWriter = new CsvWriter(new StreamWriter(OutputFileLocation));

            if (csvClassMap != null)
            {
                csvWriter.Configuration.RegisterClassMap(csvClassMap);
            }

            if (writeHeader)
            {
                csvWriter.WriteHeader(typeof(T));
            }
        }

        public void WriteRecords(IEnumerable<T> records)
        {
            lock (writeLock)
            {
                csvWriter.WriteRecords(records);
            }
        }

        public void WriteRecord(T record)
        {
            lock (writeLock)
            {
                csvWriter.WriteRecord(record);
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
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                csvWriter.Dispose();
            }

            disposed = true;
        }

        ~CsvFileWriter()
        {
            Dispose(false);
        }

        #endregion IDisposable Implementation
    }
}