using log4net;
using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Logshark.Common.Extensions;
using Logshark.Common.TaskSchedulers;
using Logshark.Core.Controller.Parsing.Preprocessing;
using Logshark.Core.Helpers.StatusWriter;
using Logshark.Core.Helpers.Timers;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Logshark.Core.Controller.Parsing
{
    /// <summary>
    /// Handles the processing of a logset into MongoDB.
    /// </summary>
    internal abstract class LogsetParser
    {
        protected readonly LogsharkTuningOptions tuningOptions;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected LogsetParser(LogsharkTuningOptions tuningOptions)
        {
            this.tuningOptions = tuningOptions;
        }

        #region Public Methods

        /// <summary>
        /// Processes an entire directory of log files.
        /// </summary>
        public LogsetParsingResult ParseLogset(LogsetParsingRequest request)
        {
            Log.InfoFormat($"Processing log directory '{request.Target}'..");

            LogsetParsingResult result;
            using (var parseTimer = new LogsharkTimer("Parsed Files", request.LogsetHash, GlobalEventTimingData.Add))
            {
                var logsetPreprocessor = new LogsetPreprocessor(tuningOptions);
                Queue<LogFileContext> logFiles = logsetPreprocessor.Preprocess(request.Target, request.ArtifactProcessor, request.CollectionsToParse);

                Initialize(request);

                using (GetProcessingWrapper(request))
                {
                    result = ProcessFiles(logFiles, request.ArtifactProcessor.GetParserFactory(request.Target), request.LogsetHash);
                }

                Log.InfoFormat($"Finished processing log directory '{request.Target}'! [{parseTimer.Elapsed.Print()}]");
            }

            Finalize(request, result);

            var validator = GetValidator();
            validator.ValidateDataExists(request.LogsetHash);

            return result;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Handles any initialization tasks for the derived LogsetParser.
        /// </summary>
        protected abstract void Initialize(LogsetParsingRequest request);

        /// <summary>
        /// Retrieves an instance of a writer that will be used to write the parsed log events.
        /// </summary>
        /// <returns></returns>
        protected abstract IDocumentWriter GetDocumentWriter(LogFileContext file, string collectionName, string logsetHash);

        /// <summary>
        /// Retrieves an instance of a disposable resource that will be wrapped around the main file processing phase.
        /// This is a hook that can be used for such things as heartbeat timers or custom status writers.
        /// </summary>
        protected abstract IDisposable GetProcessingWrapper(LogsetParsingRequest request);

        /// <summary>
        /// Retrieves an instance of a validator that can be used that the resulting processed logset yielded valid data.
        /// </summary>
        protected abstract IParsedLogsetValidator GetValidator();

        /// <summary>
        /// Spins off processing tasks for a collection of log files.
        /// </summary>
        protected LogsetParsingResult ProcessFiles(Queue<LogFileContext> files, IParserFactory parserFactory, string logsetHash)
        {
            var failedFileParses = new ConcurrentBag<string>();
            var totalSizeBytes = files.Sum(file => file.FileSize);

            var taskFactory = GetFileProcessingTaskFactory();
            var tasks = files.Select(file => taskFactory
                             .StartNew(() =>
                             {
                                 bool processedSuccessfully = ProcessFile(file, parserFactory, logsetHash);
                                 if (!processedSuccessfully)
                                 {
                                     failedFileParses.Add(file.FilePath);
                                 }
                             }))
                             .ToList();

            const string progressMessage = "Logset processing is approximately {PercentComplete} complete. {TasksRemaining} files remaining..";
            using (new TaskStatusWriter(tasks, Log, progressMessage, pollIntervalSeconds: 15))
            {
                Task.WaitAll(tasks.ToArray());
            }

            return new LogsetParsingResult(failedFileParses, totalSizeBytes);
        }

        /// <summary>
        /// Process a single log file.
        /// </summary>
        protected bool ProcessFile(LogFileContext file, IParserFactory parserFactory, string logsetHash)
        {
            try
            {
                Log.InfoFormat($"Processing {file}.. ({file.FileSize.ToPrettySize()})");
                using (var parseTimer = new LogsharkTimer("Parse File", file.ToString(), GlobalEventTimingData.Add))
                {
                    IParser parser = parserFactory.GetParser(file);
                    if (parser == null)
                    {
                        Log.ErrorFormat($"Failed to locate a parser for file '{file.FilePath}'.  Skipping this file..");
                        return false;
                    }

                    IDocumentWriter writer = GetDocumentWriter(file, parser.CollectionSchema.CollectionName, logsetHash);

                    // Attempt to process the file; register a failure if we don't yield at least one document for a file
                    // with at least one byte of content.
                    var fileProcessor = new LogFileParser(parser, writer);
                    long documentsSuccessfullyParsed = fileProcessor.Parse(file);
                    if (file.FileSize > 0 && documentsSuccessfullyParsed == 0)
                    {
                        Log.WarnFormat($"Failed to parse any log events from {file}!");
                        return false;
                    }

                    Log.InfoFormat($"Completed processing of {file} ({file.FileSize.ToPrettySize()}) [{parseTimer.Elapsed.Print()}]");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to process file '{file}': {ex.Message}");
                Log.Debug(ex.StackTrace);
                return false;
            }
            finally
            {
                Cleanup(file);
            }
        }

        /// <summary>
        /// Creates a task factory to handle all file processing tasks.
        /// </summary>
        /// <returns></returns>
        protected virtual TaskFactory GetFileProcessingTaskFactory()
        {
            int maxFileProcessingConcurrency = Environment.ProcessorCount * tuningOptions.FileProcessorConcurrencyLimitPerCore;

            Log.InfoFormat($"Setting file processing concurrency limit to {maxFileProcessingConcurrency} concurrent files. " +
                           $"({Environment.ProcessorCount} logical {"core".Pluralize(Environment.ProcessorCount)} present)");

            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxFileProcessingConcurrency);

            return new TaskFactory(scheduler);
        }

        /// <summary>
        /// Handles any resource cleanup associated with processing this file.
        /// </summary>
        protected virtual bool Cleanup(LogFileContext fileContext)
        {
            // Now that we've processed the file, we can delete it.
            try
            {
                File.Delete(fileContext.FilePath);
                return true;
            }
            catch (Exception ex)
            {
                // Log & swallow exception; cleanup is a nice-to-have, not a need-to-have.
                Log.DebugFormat($"Failed to remove processed file '{fileContext.FilePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles any finalization and/or cleanup tasks for the derived LogsetParser.
        /// </summary>
        protected abstract void Finalize(LogsetParsingRequest request, LogsetParsingResult result);

        #endregion Protected Methods
    }
}