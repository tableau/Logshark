using System;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace LogShark
{
    /// <summary>
    /// The purpose of this class is to provide a "connection pool" to Zip files so instead of opening/closing zip files each time different threads can reuse existing ZipArchive objects
    /// We have to have a separate class instead of using off the shelf components (i.e. BufferBlock<T>) because LogShark has code to handle nested zip files and thus a single run can open multiple different zip files
    /// </summary>
    public class ZipArchivePool : IDisposable
    {
        private readonly object _dictionaryLock;
        private readonly Dictionary<string, Queue<ZipArchive>> _openedZipFiles;
        private readonly ILogger _logger;

        private int _checkedOutArchivesCount;

        public ZipArchivePool(ILogger logger)
        {
            _dictionaryLock = new object();
            _openedZipFiles = new Dictionary<string, Queue<ZipArchive>>();
            _checkedOutArchivesCount = 0;
            
            _logger = logger;
        }

        public ZipArchiveFromPool CheckOut(string zipPath)
        {
            _logger.LogDebug("Got request for ZipArchive for file `{zipPath}`. Currently checked out ZipArchive count is {currentZipArchiveCount}", zipPath, _checkedOutArchivesCount);
            
            lock (_dictionaryLock)
            {
                ++_checkedOutArchivesCount; // Incrementing here as all code paths in this method should return a ZipArchive

                var generatedBefore = _openedZipFiles.ContainsKey(zipPath);

                if (!generatedBefore)
                {
                    _openedZipFiles.Add(zipPath, new Queue<ZipArchive>()); // We're creating empty queue only to "record" the fact that we generated zip archives for this zipPath before. New zipArchive will be handed off to caller right away though, so queue stays empty
                    return new ZipArchiveFromPool(zipPath, ZipFile.Open(zipPath, ZipArchiveMode.Read),this);
                }

                var queueForThisZipPath = _openedZipFiles[zipPath];

                if (queueForThisZipPath.Count > 0)
                {
                    var zipArchive = queueForThisZipPath.Dequeue();
                    return new ZipArchiveFromPool(zipPath, zipArchive, this);
                }
                
                return new ZipArchiveFromPool(zipPath, ZipFile.Open(zipPath, ZipArchiveMode.Read),this);
            }
        }

        private void Return(string zipPath, ZipArchive zipArchive)
        {
            _logger.LogDebug("Received return request for ZipArchive for path `{zipPath}`. Currently checked out ZipArchive count is {currentZipArchiveCount}.", zipPath, _checkedOutArchivesCount);
            
            lock (_dictionaryLock)
            {
                if (!_openedZipFiles.ContainsKey(zipPath))
                {
                    _logger.LogWarning($"ZipArchive object for zipPath `{{zipPath}}` was returned to {nameof(ZipArchivePool)}, but it doesn't appear that this instance generated it in the first place.", zipPath);
                    _openedZipFiles[zipPath] = new Queue<ZipArchive>();
                }
                
                _openedZipFiles[zipPath].Enqueue(zipArchive);
                --_checkedOutArchivesCount;
            }
        }

        public void Dispose()
        {
            lock (_dictionaryLock)
            {
                if (_checkedOutArchivesCount > 0)
                {
                    _logger.LogWarning("Dispose was called while there are checked out ZipArchive(s) still. Current count of checked out ZipArchives is {currentZipArchiveCount}.", _checkedOutArchivesCount);
                }
                
                foreach (var (_, queue) in _openedZipFiles)
                {
                    foreach (var zipArchive in queue)
                    {
                        zipArchive.Dispose();
                    }
                }
            }
        }

        public class ZipArchiveFromPool : IDisposable
        {
            public ZipArchive ZipArchive { get; }
            
            private readonly ZipArchivePool _issuingPool;
            private readonly string _zipPath;
            
            public ZipArchiveFromPool(string zipPath, ZipArchive zipArchive, ZipArchivePool issuingPool)
            {
                _issuingPool = issuingPool;
                _zipPath = zipPath;

                ZipArchive = zipArchive;
            }
            
            public void Dispose()
            {
                _issuingPool.Return(_zipPath, ZipArchive);
            }
        }
    }
}