using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LogShark.Progress
{
    /// <summary>
    /// Renders progress to console using Spectre.Console LiveDisplay
    /// </summary>
    public class ConsoleProgressRenderer
    {
        private ProgressSnapshot _latestSnapshot;
        private readonly object _lock = new object();
        private readonly SpectreConsoleLoggerProvider _loggerProvider;

        public ConsoleProgressRenderer(SpectreConsoleLoggerProvider loggerProvider = null)
        {
            _loggerProvider = loggerProvider;
        }

        public IRenderable Render()
        {
            lock (_lock)
            {
                var items = new List<IRenderable>();

                // Show recent logs if available (showing more logs for better visibility)
                if (_loggerProvider != null)
                {
                    var recentLogs = _loggerProvider.GetRecentLogs(50).ToList();
                    if (recentLogs.Any())
                    {
                        var logPanel = new Panel(string.Join("\n", recentLogs))
                            .Header("[bold]Recent Logs (last 50)[/]")
                            .Border(BoxBorder.Rounded)
                            .BorderColor(Color.Grey);
                        items.Add(logPanel);
                        items.Add(new Text("")); // spacer
                    }
                }

                // Progress table
                if (_latestSnapshot == null)
                {
                    items.Add(new Markup("[dim]Initializing...[/]"));
                }
                else
                {
                    var table = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(Color.Grey)
                        .HideHeaders();

                    table.AddColumn(new TableColumn("").Width(15));
                    table.AddColumn(new TableColumn(""));

                    // Overall progress row
                    table.AddRow(
                        new Markup("[bold green]Overall[/]"),
                        BuildOverallRow(_latestSnapshot)
                    );

                    // Current plugin row
                    if (_latestSnapshot.CurrentLogType.HasValue && _latestSnapshot.CurrentLogTypeTotalFiles > 0)
                    {
                        var pluginName = GetLogTypeDisplayName(_latestSnapshot.CurrentLogType.Value);
                        table.AddRow(
                            new Markup($"[bold cyan]{pluginName}[/]"),
                            BuildCurrentPluginRow(_latestSnapshot)
                        );
                    }
                    else
                    {
                        table.AddRow(
                            new Markup("[bold cyan]Current[/]"),
                            new Markup("[dim]Waiting...[/]")
                        );
                    }

                    items.Add(table);
                }

                return new Rows(items);
            }
        }

        public void Update(ProgressSnapshot snapshot)
        {
            lock (_lock)
            {
                _latestSnapshot = snapshot;
            }
        }

        public void Complete()
        {
            // Nothing to do - LiveDisplay will stop automatically
        }

        private IRenderable BuildOverallRow(ProgressSnapshot snapshot)
        {
            var percentage = snapshot.OverallPercentage;
            var progressBarText = BuildProgressBarText(percentage, 40);

            var content =
                $"{progressBarText} [white]{percentage:F0}%[/]\n" +
                $"[white]{snapshot.TotalFilesCompleted}/{snapshot.TotalFilesTotal} files[/] [dim]|[/] " +
                $"[white]{snapshot.TotalLogTypesCompleted}/{snapshot.TotalLogTypesWithFiles} plugins[/] [dim]|[/] " +
                $"[dim]Elapsed:[/] [yellow]{FormatTimeSpan(snapshot.ElapsedTime)}[/]" +
                (snapshot.OverallEta.HasValue ? $" [dim]|[/] [dim]ETA:[/] [yellow]{FormatTimeSpan(snapshot.OverallEta.Value)}[/]" : "") +
                (snapshot.EstimatedCompletionTime.HasValue ? $" [dim]|[/] [dim]Finish:[/] [yellow]{snapshot.EstimatedCompletionTime.Value:HH:mm:ss}[/]" : "");

            return new Markup(content);
        }

        private IRenderable BuildCurrentPluginRow(ProgressSnapshot snapshot)
        {
            var percentage = snapshot.CurrentLogTypeTotalFiles > 0
                ? (snapshot.CurrentLogTypeFilesCompleted * 100.0) / snapshot.CurrentLogTypeTotalFiles
                : 0;

            var progressBarText = BuildProgressBarText(percentage, 40);

            var content =
                $"{progressBarText} [white]{percentage:F0}%[/]\n" +
                $"[white]{snapshot.CurrentLogTypeFilesCompleted}/{snapshot.CurrentLogTypeTotalFiles} files[/] [dim]|[/] " +
                $"[white]{snapshot.CurrentLogTypeSpeedMbPerSec:F1} MB/s[/]" +
                (snapshot.CurrentLogTypeEta.HasValue ? $" [dim]|[/] [dim]ETA:[/] [yellow]{FormatTimeSpan(snapshot.CurrentLogTypeEta.Value)}[/]" : "");

            return new Markup(content);
        }

        private string BuildProgressBarText(double percentage, int width)
        {
            var filled = (int)(percentage / 100.0 * width);
            var empty = width - filled;
            return $"[green]{new string('█', filled)}[/][grey]{new string('░', empty)}[/]";
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1)
            {
                return "<1s";
            }
            else if (timeSpan.TotalMinutes < 1)
            {
                return $"{timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalHours < 1)
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds:D2}s";
            }
            else
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes:D2}m";
            }
        }

        private string GetLogTypeDisplayName(LogType logType)
        {
            return logType switch
            {
                LogType.BackgrounderCpp => "Backgrounder",
                LogType.BackgrounderJava => "BackgrounderJava",
                LogType.DataserverCpp => "Dataserver",
                LogType.DataserverJava => "DataserverJava",
                LogType.NetstatLinux => "Netstat",
                LogType.NetstatWindows => "Netstat",
                LogType.VizqlserverCpp => "Vizqlserver",
                LogType.VizportalCpp => "Vizportal",
                LogType.VizportalJava => "VizportalJava",
                LogType.VizdataCpp => "Vizdata",
                LogType.VizdataJava => "VizdataJava",
                LogType.TabadminControllerJava => "TabadminController",
                LogType.TabadminAgentJava => "TabadminAgent",
                _ => logType.ToString()
            };
        }
    }
}
