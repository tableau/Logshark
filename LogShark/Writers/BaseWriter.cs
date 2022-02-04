using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers
{
    public abstract class BaseWriter<T> : IWriter<T>
    {
        protected readonly ILogger Logger;
        
        private readonly DataSetInfo _dataSetInfo;
        private readonly string _writerName;
        private readonly object _writeLock;
        
        private long _linesPersisted = 0;
        private long _nullLinesIgnored = 0;
        private bool _closed = false;

        protected BaseWriter(DataSetInfo dataSetInfo, ILogger logger, string writerName)
        {
            _dataSetInfo = dataSetInfo;
            Logger = logger;
            _writerName = writerName;
            _writeLock = new object();
        }
        
        protected abstract void InsertNonNullLineLogic(T objectToWrite);
        protected abstract void CloseLogic();

        protected virtual long InsertMultipleLinesLogic(IEnumerable<T> objectsToWrite)
        {
            var counter = 0L;
            foreach(var line in objectsToWrite)
            {
                if (line == null)
                {
                    ++_nullLinesIgnored;
                    continue;
                }
                
                InsertNonNullLineLogic(line);
                ++counter;
            }

            return counter;
        }

        public void AddLine(T objectToWrite)
        {
            if (_closed)
            {
                throw new LogSharkProgramLogicException($"{nameof(AddLine)} method was called after {nameof(Close)}. This happened for writer for '{_dataSetInfo}' data set");
            }

            if (objectToWrite == null)
            {
                ++_nullLinesIgnored;
                return;
            }

            lock (_writeLock)
            {
                InsertNonNullLineLogic(objectToWrite);
                ++_linesPersisted;
            }
        }

        public void AddLines(IEnumerable<T> objectsToWrite)
        {
            if (_closed)
            {
                throw new LogSharkProgramLogicException($"{nameof(AddLines)} method was called after {nameof(Close)}. This happened for writer for '{_dataSetInfo}' data set");
            }
            
            if (objectsToWrite == null)
            {
                ++_nullLinesIgnored;
                return;
            }

            lock (_writeLock)
            {
                var linesInserted = InsertMultipleLinesLogic(objectsToWrite);
                _linesPersisted += linesInserted;
            }
        }

        public WriterLineCounts Close()
        {
            CloseLogic();
            
            var writerFullName = $"{_writerName}<{typeof(T)}>";
            if (_linesPersisted == 0)
            {
                Logger.LogDebug("No items written to {writerName}", writerFullName);
            }
            else
            {
                Logger.LogDebug("{writerName} wrote {linesPersisted} items", writerFullName, _linesPersisted);
            }
            
            if (_nullLinesIgnored > 0)
            {
                Logger.LogWarning("{writerName} ignored {numberOfNullRecords} null lines", writerFullName, _nullLinesIgnored);
            }

            _closed = true;
            
            return new WriterLineCounts(_dataSetInfo, _linesPersisted, _nullLinesIgnored);
        }

        public virtual void Dispose()
        {
            if (!_closed)
            {
                throw new LogSharkProgramLogicException($"Dispose was called on writer before {nameof(Close)} was called. This happened for writer for '{_dataSetInfo}' data set");
            }
        }
    }
}