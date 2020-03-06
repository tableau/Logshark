using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogShark.Containers;
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

            if (runSummary.LogReadingResults != null)
            {
                GenerateLogReadingStatisticsSection(sb, runSummary);
            }

            return sb.ToString();
        }

        public static bool HasLogReadingErrors(this RunSummary runSummary)
        {
            return runSummary?.LogReadingResults?.Errors != null && 
                   runSummary.LogReadingResults.Errors.Count > 0;
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
        
        public static bool HasWorkbookPublisherErrors(this RunSummary runSummary)
        {
            if (runSummary.PublisherResults == null)
            {
                return false;
            }
            
            var hasErrorsForIndividualWorkbooks = runSummary.PublisherResults.PublishedWorkbooksInfo != null &&
                                                  runSummary.PublisherResults.PublishedWorkbooksInfo.Any(result => !result.PublishedSuccessfully);
            
            return !runSummary.PublisherResults.CreatedProjectSuccessfully ||
                   hasErrorsForIndividualWorkbooks;
        }

        public static string LogReadingErrorsReport(this RunSummary runSummary)
        {
            if (!runSummary.HasLogReadingErrors())
            {
                return "No errors occurred while reading logs";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("Errors occurred while reading logs:");
            sb.AppendLine(string.Join(Environment.NewLine, runSummary.LogReadingResults.Errors));
            return sb.ToString();
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
            if (!runSummary.HasWorkbookPublisherErrors())
            {
                return "All workbooks were published successfully";
            }

            if (!runSummary.PublisherResults.CreatedProjectSuccessfully)
            {
                return $"Publisher failed to connect to the Tableau Server or create project for the results. Exception message: {runSummary.PublisherResults.ExceptionCreatingProject?.Message ?? "(null)"}";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("Workbooks failed to publish:");
            var failedWorkbooks = runSummary.PublisherResults.PublishedWorkbooksInfo
                .Where(result => !result.PublishedSuccessfully)
                .OrderBy(result => result.PublishedWorkbookName ?? "null")
                .Select(result => $"Failed to publish workbook `{result.PublishedWorkbookName ?? "(null)"}`. Exception: {result.Exception?.Message ?? "(null)"}");
            sb.AppendLine(string.Join(Environment.NewLine, failedWorkbooks));
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
            
            if (runSummary.HasWorkbookPublisherErrors())
            {
                sb.AppendLine(runSummary.WorkbookPublisherErrorsReport());
            }
            
            var successfullyPublishedWorkbooks = publisherResults.PublishedWorkbooksInfo?
                .Where(result => result.PublishedSuccessfully)
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
            var logReadingResults = runSummary.LogReadingResults;
            sb.AppendLine(GenerateTitle("Log Reading Statistics"));

            var sizeMb = logReadingResults.FullLogSetSizeBytes / 1024 / 1024;

            sb.AppendLine(logReadingResults.IsDirectory 
                ? $"Processed directory full size: {sizeMb:n0} MB"
                : $"Processed zip file compressed size: {sizeMb:n0} MB");
            
            var pluginsExecutionResults = logReadingResults.PluginsExecutionResults;
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

            if (logReadingResults.PluginsReceivedAnyData.Count > 0)
            {
                var pluginsReceivedAnyData = logReadingResults.PluginsReceivedAnyData.OrderBy(pluginName => pluginName);
                sb.AppendLine($"Plugins that received any data: {string.Join(", ", pluginsReceivedAnyData)}");
                sb.AppendLine();

                var nonEmptyStatistics = logReadingResults.LogProcessingStatistics
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

            if (runSummary.HasLogReadingErrors())
            {
                sb.AppendLine();
                sb.AppendLine(runSummary.LogReadingErrorsReport());
            }

            GenerateWritersStatisticsSection(sb, logReadingResults.PluginsExecutionResults.GetWritersStatistics());
        }
        
        private static void GenerateWritersStatisticsSection(StringBuilder sb, WritersStatistics writersStatistics)
        {
            sb.AppendLine(GenerateTitle("Data Writers Statistics"));
            
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