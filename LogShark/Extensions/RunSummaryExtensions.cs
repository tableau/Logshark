using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LogShark.Containers;
using LogShark.Shared.Extensions;
using LogShark.Writers.Containers;

namespace LogShark.Extensions
{
    public static class RunSummaryExtensions
    {
        public static string ToStringReport(this RunSummary runSummary)
        {
            var sb = new StringBuilder();

            GenerateSummarySection(sb, runSummary);

            if (runSummary.ProcessingNotificationsCollector != null)
            {
                GenerateProcessingErrorsSection(sb, runSummary);
            }

            if (runSummary.PublisherResults != null)
            {
                GeneratePublishingResultsSection(sb, runSummary);
            }

            if (runSummary.WorkbookGeneratorResults != null)
            {
                GenerateWorkbookGeneratorResultsSection(sb, runSummary);
            }

            if (runSummary.ProcessLogSetResult != null)
            {
                GenerateLogReadingStatisticsSection(sb, runSummary);
            }

            return sb.ToString().CleanControlCharacters();
        }

        public static bool LogReadingCompletedSuccessfully(this RunSummary runSummary)
        {
            return runSummary?.ProcessLogSetResult != null &&
                   runSummary.ProcessLogSetResult.IsSuccessful;
        }

        public static bool HasProcessingErrors(this RunSummary runSummary)
        {
            return runSummary?.ProcessingNotificationsCollector != null &&
                   runSummary.ProcessingNotificationsCollector.TotalErrorsReported > 0;
        }
        
        public static bool HasWorkbookGeneratorErrors(this RunSummary runSummary)
        {
            return runSummary?.WorkbookGeneratorResults?.CompletedWorkbooks != null && 
                   runSummary.WorkbookGeneratorResults.CompletedWorkbooks.Any(result => !result.GeneratedSuccessfully);
        }
        
        public static bool HasNonSuccessfulPublishingResults(this RunSummary runSummary)
        {
            if (runSummary.PublisherResults == null)
            {
                return false;
            }
            
            var hasErrorsForIndividualWorkbooks = runSummary.PublisherResults.PublishedWorkbooksInfo != null &&
                                                  runSummary.PublisherResults.PublishedWorkbooksInfo.Any(result => result.PublishState != WorkbookPublishResult.WorkbookPublishState.Success);
            
            return !runSummary.PublisherResults.CreatedProjectSuccessfully ||
                   hasErrorsForIndividualWorkbooks;
        }

        public static string LogSetProcessingStatusReport(this RunSummary runSummary)
        {
            return runSummary.LogReadingCompletedSuccessfully()
                ? "LogShark successfully processed all logs" 
                : $"Error occurred while processing logs: {runSummary.ProcessLogSetResult.ErrorMessage}";
        }

        public static string ProcessingErrorsReport(this RunSummary runSummary)
        {
            if (!runSummary.HasProcessingErrors())
            {
                return "No errors occurred while processing log set";
            }
            
            var sb = new StringBuilder();
            var errorsCollector = runSummary.ProcessingNotificationsCollector;
            sb.AppendLine($"{errorsCollector.TotalErrorsReported} errors occurred while processing log set:");

            if (errorsCollector.TotalErrorsReported > errorsCollector.MaxErrorsWithDetails)
            {
                sb.AppendLine();
                AddErrorsCountByReporterToStringBuilder(errorsCollector, sb);
                sb.AppendLine();
                sb.AppendLine($"Details on first {errorsCollector.MaxErrorsWithDetails} reported errors:");
                AppendDetailedErrorsToStringBuilder(errorsCollector, sb);
            }
            else
            {
                sb.AppendLine("Details of all reported errors:");
                AppendDetailedErrorsToStringBuilder(errorsCollector, sb);
            }
            

            return sb.ToString();
        }

        public static string WorkbookGeneratorErrorsReport(this RunSummary runSummary)
        {
            if (!runSummary.HasWorkbookGeneratorErrors())
            {
                return "All workbooks were generated successfully";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("Errors occurred while generating workbooks:");

            var errors = runSummary.WorkbookGeneratorResults.CompletedWorkbooks
                .Where(result => !result.GeneratedSuccessfully)
                .OrderBy(cwi => cwi.OriginalWorkbookName ?? "null")
                .Select(result => $"Failed to generate workbook `{result.OriginalWorkbookName ?? "(null)"}`. Exception: {result.Exception?.Message ?? "(null)"}");
            sb.AppendLine(string.Join(Environment.NewLine, errors));

            return sb.ToString();
        }

        public static string WorkbookPublisherErrorsReport(this RunSummary runSummary)
        {
            if (!runSummary.HasNonSuccessfulPublishingResults())
            {
                return "All workbooks were published successfully";
            }

            if (!runSummary.PublisherResults.CreatedProjectSuccessfully)
            {
                return $"Publisher failed to connect to the Tableau Server or create project for the results. Exception message: {runSummary.PublisherResults.ExceptionCreatingProject?.Message ?? "(null)"}";
            }
            
            var sb = new StringBuilder();
            var failedWorkbooks = runSummary.PublisherResults.PublishedWorkbooksInfo
                .Where(result => result.PublishState == WorkbookPublishResult.WorkbookPublishState.Fail)
                .OrderBy(result => result.OriginalWorkbookName ?? "null")
                .Select(result => $"Failed to publish workbook `{result.OriginalWorkbookName ?? "(null)"}`. Exception: {result.Exception?.Message ?? "(null)"}")
                .ToList();
            if (failedWorkbooks.Count > 0)
            {
                sb.AppendLine("Workbooks failed to publish:");
                sb.AppendLine(string.Join(Environment.NewLine, failedWorkbooks));
            }
            
            var timedOutWorkbooks = runSummary.PublisherResults.PublishedWorkbooksInfo
                .Where(result => result.PublishState == WorkbookPublishResult.WorkbookPublishState.Timeout)
                .Select(result => result.OriginalWorkbookName)
                .OrderBy(result => result)
                .ToList();
            if (timedOutWorkbooks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Workbook(s) that timed out while publishing:");
                sb.AppendLine(string.Join("; ", timedOutWorkbooks));
                sb.AppendLine("Publishing timeout often happens if workbook is too large and Tableau Server takes a long time to generate thumbnails. In this case workbook will finish publishing on its own and should be available on Tableau Server after some time.");
            }

            return sb.ToString();
        }

        private static void GenerateSummarySection(StringBuilder sb, RunSummary runSummary)
        {
            sb.AppendLine(GenerateTitle("Summary"));
            
            if (runSummary.IsSuccess)
            {
                sb.AppendLine($"Run ID {runSummary.RunId} completed successfully in {runSummary.Elapsed}");
            }
            else
            {
                sb.AppendLine($"Run ID {runSummary.RunId} failed after running for {runSummary.Elapsed}. Reported problem: {runSummary.ReasonForFailure}");
                sb.Append($"This problem is ");
                switch(runSummary.IsTransient)
                {
                    case true:
                        sb.AppendLine("a transient issue that may go away if LogShark is run again.");
                        break;
                    case false:
                        sb.AppendLine("not transient and is unlikely to go away if LogShark is run again.");
                        break;
                    case null:
                    default:
                        sb.AppendLine("unexpected and it is not known whether or not it will go away if LogShark is run again.");
                        break;
                }                
                sb.AppendLine("Information below might contain more details on what exactly failed");
            }
            
            var buildInfo = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "(null)";
            sb.AppendLine($"LogShark version: {buildInfo}");
        }

        private static void GenerateProcessingErrorsSection(StringBuilder sb, RunSummary runSummary)
        {
            sb.AppendLine(GenerateTitle("Processing errors"));
            sb.AppendLine(runSummary.ProcessingErrorsReport());
        }
        
        private static void GeneratePublishingResultsSection(StringBuilder sb, RunSummary runSummary)
        {
            var publisherResults = runSummary.PublisherResults;
            sb.AppendLine(GenerateTitle("Published workbooks"));
            sb.AppendLine($"Used {publisherResults.TableauServerSite} site on {publisherResults.TableauServerUrl}");
            sb.AppendLine($"Link to the published project: {publisherResults.PublishedProjectUrl ?? "N/A"}");
            
            if (runSummary.HasNonSuccessfulPublishingResults())
            {
                sb.AppendLine(runSummary.WorkbookPublisherErrorsReport());
            }
            
            var successfullyPublishedWorkbooks = publisherResults.PublishedWorkbooksInfo?
                .Where(result => result.PublishState == WorkbookPublishResult.WorkbookPublishState.Success)
                .Select(result => result.PublishedWorkbookName)
                .OrderBy(workbookName => workbookName)
                .ToList();
            if (successfullyPublishedWorkbooks?.Count > 0)
            {
                sb.AppendLine($"Successfully published workbooks: {GenerateHorizontalList(successfullyPublishedWorkbooks)}");
            }
        }

        private static void GenerateWorkbookGeneratorResultsSection(StringBuilder sb, RunSummary runSummary)
        {
            sb.AppendLine(GenerateTitle("Generated workbooks"));

            var workbookTemplates = runSummary.WorkbookGeneratorResults.WorkbookTemplates ?? Enumerable.Empty<PackagedWorkbookTemplateInfo>();
            var allAvailableTemplates = workbookTemplates.Select(template => template.Name).OrderBy(name => name);
            sb.AppendLine($"Workbook templates available: {string.Join("; ", allAvailableTemplates)}");

            var completedWorkbooks = runSummary.WorkbookGeneratorResults.CompletedWorkbooks ?? Enumerable.Empty<CompletedWorkbookInfo>();
            var successfullyGenerated = completedWorkbooks.Where(cwi => cwi.GeneratedSuccessfully).ToList();
            var workbooksWithData = successfullyGenerated
                .Where(cwi => cwi.HasAnyData)
                .Select(cwi => cwi.OriginalWorkbookName)
                .OrderBy(workbookName => workbookName)
                .ToList();
            if (workbooksWithData.Count > 0)
            {
                sb.AppendLine($"Successfully generated workbooks with data: {GenerateHorizontalList(workbooksWithData)}");
            }

            var workbooksWithoutData = successfullyGenerated
                .Where(cwi => !cwi.HasAnyData)
                .Select(cwi => cwi.OriginalWorkbookName)
                .OrderBy(workbookName => workbookName)
                .ToList();
            if (workbooksWithoutData.Count > 0)
            {
                sb.AppendLine($"Empty workbooks: {GenerateHorizontalList(workbooksWithoutData)}");
            }

            if (runSummary.HasWorkbookGeneratorErrors())
            {
                sb.AppendLine(runSummary.WorkbookGeneratorErrorsReport());
            }
        }
        
        private static void GenerateLogReadingStatisticsSection(StringBuilder sb, RunSummary runSummary)
        {
            var processLogSetResult = runSummary.ProcessLogSetResult;
            sb.AppendLine(GenerateTitle("Log Reading Statistics"));

            if (!processLogSetResult.IsSuccessful)
            {
                sb.AppendLine($"Log processing did not complete successfully. Error message: {processLogSetResult.ErrorMessage ?? "(null)"}");
            }

            var sizeMb = processLogSetResult.FullLogSetSizeBytes / 1024 / 1024;

            sb.AppendLine(processLogSetResult.IsDirectory 
                ? $"Processed directory full size: {sizeMb:n0} MB"
                : $"Processed zip file compressed size: {sizeMb:n0} MB");
            
            var pluginsExecutionResults = processLogSetResult.PluginsExecutionResults;
            if (pluginsExecutionResults != null)
            {
                var additionalTags = pluginsExecutionResults.GetSortedTagsFromAllPlugins();
                if (additionalTags.Count > 0)
                {
                    var wrappedTags = additionalTags.Select(tag => $"'{tag}'");
                    sb.Append("Additional tags returned by plugins: ");
                    sb.AppendLine(string.Join(";", wrappedTags));
                }
            }

            if (processLogSetResult.PluginsReceivedAnyData.Count > 0)
            {
                var pluginsReceivedAnyData = processLogSetResult.PluginsReceivedAnyData.OrderBy(pluginName => pluginName);
                sb.AppendLine($"Plugins that received any data: {string.Join(", ", pluginsReceivedAnyData)}");
                sb.AppendLine();

                var nonEmptyStatistics = processLogSetResult.LogProcessingStatistics
                    .Where(pair => pair.Value.FilesProcessed > 0)
                    .OrderBy(pair => pair.Key.ToString());
                foreach (var (logType, logProcessingStatistics) in nonEmptyStatistics)
                {
                    sb.AppendLine($"{logType.ToString().PadRight(50, ' ')}: {logProcessingStatistics}");
                }
            }
            else
            {
                sb.AppendLine("No relevant log files found for plugins selected or all log types failed to process");
            }

            GenerateWritersStatisticsSection(sb, processLogSetResult?.PluginsExecutionResults?.GetWritersStatistics());
        }
        
        private static void GenerateWritersStatisticsSection(StringBuilder sb, WritersStatistics writersStatistics)
        {
            sb.AppendLine(GenerateTitle("Data Writers Statistics"));
            if (writersStatistics == null)
            {
                sb.AppendLine("No writer statistics generated");
                return;
            }
            
            var receivedAnything = writersStatistics.DataSets
                .Where(pair => pair.Value.LinesPersisted > 0)
                .OrderBy(pair => pair.Key.ToString())
                .Select(pair => $"{pair.Key.ToString().PadRight(50, ' ')}: {pair.Value.LinesPersisted} lines persisted")
                .ToList();
            if (receivedAnything.Count > 0)
            {
                sb.AppendLine("Lines persisted per data set:");
                sb.AppendLine(string.Join(Environment.NewLine, receivedAnything));
                sb.AppendLine();
            }
            
            var receivedNulls = writersStatistics.DataSets
                .Where(pair => pair.Value.NullLinesIgnored > 0)
                .OrderBy(pair => pair.Key.ToString())
                .Select(pair => $"{pair.Key.ToString().PadRight(40, ' ')}: {pair.Value.NullLinesIgnored} null lines ignored")
                .ToList();
            
            if (receivedNulls.Count > 0)
            {
                sb.AppendLine("Some writer(s) encountered null values. This is not a normal condition. Please contact LogShark owners for help.");
                sb.AppendLine("Counts per data set:");
                sb.AppendLine(string.Join(Environment.NewLine, receivedNulls));
            }
        }

        private static string GenerateTitle(string title)
        {
            return $"{Environment.NewLine}-----{title}-----";
        }

        private static string GenerateHorizontalList(IEnumerable<string> list)
        {
            return string.Join("; ", list);
        }
        
        private static void AddErrorsCountByReporterToStringBuilder(ProcessingNotificationsCollector errorsCollector, StringBuilder report)
        {
            if (errorsCollector.ErrorCountByReporter == null)
            {
                return;
            }
            
            if (errorsCollector.ErrorCountByReporter.Count == 1)
            {
                report.AppendLine($"All errors reported by {errorsCollector.ErrorCountByReporter.First().Key}.");
            }
            else
            {
                report.AppendLine($"Error count by reporter:");
                foreach (var (reporter, count) in errorsCollector.ErrorCountByReporter.OrderBy(kvp => kvp.Key))
                {
                    report.AppendLine($"{reporter?.PadRight(30, ' ') ?? "(null)"}: {count}");
                }
            }
        }

        private static void AppendDetailedErrorsToStringBuilder(ProcessingNotificationsCollector errorsCollector, StringBuilder report)
        {
            foreach (var error in errorsCollector.ProcessingErrorsDetails ?? new List<ProcessingNotification>())
            {
                report.AppendLine($"--> File: `{error.FilePath ?? "(null)"}`. Line: {error.LineNumber}. Reported by: `{error.ReportedBy ?? "(null)"}`. Error: `{error.Message ?? "(null)"}`");
            }
        }
    }
}