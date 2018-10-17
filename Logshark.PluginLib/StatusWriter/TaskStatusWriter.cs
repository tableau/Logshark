using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.PluginLib.StatusWriter
{
    /// <summary>
    /// Helper class for writing out a progress message about a list of tasks on a timer.
    ///
    /// Available tokens:
    ///     {TotalTasks} - Total tasks in the task list
    ///     {TasksCompleted} - Number of tasks that have completed
    ///     {TasksRunning} - Number of tasks that are in a Running state
    ///     {TasksRemaining} - Number of tasks that have not yet completed
    ///     {PercentComplete} - The percentage of total tasks that have completed
    ///
    /// Sample usage:
    ///     const string progressMessage = "Tasks are {PercentComplete}% complete. {TasksRemaining} tasks remaining..";
    ///     using (new TaskStatusWriter(tasks, progressMessage, pollIntervalSeconds: 20))
    ///     {
    ///         Task.WaitAll(tasks.ToArray());
    ///     }
    /// </summary>
    public sealed class TaskStatusWriter : BaseStatusWriter
    {
        private readonly ICollection<Task> tasks;
        private readonly long totalTasks;

        /// <summary>
        /// Creates a new task progress heartbeat timer with the given parameters.
        /// </summary>
        /// <param name="tasks">The task list to monitor.</param>
        /// <param name="logger">The logger to append messages to.</param>
        /// <param name="progressFormatMessage">The progress message. Can contain tokens (see class summary).</param>
        /// <param name="pollIntervalSeconds">The number of seconds to wait between heartbeats.</param>
        /// <param name="expectedTotalTasks">The number of tasks expected to be executed.  Optional.</param>
        public TaskStatusWriter(ICollection<Task> tasks, ILog logger, string progressFormatMessage, int pollIntervalSeconds, long? expectedTotalTasks = null)
            : base(logger, progressFormatMessage, pollIntervalSeconds)
        {
            this.tasks = tasks;
            totalTasks = tasks.Count;
            if (expectedTotalTasks != null)
            {
                totalTasks = expectedTotalTasks.Value;
            }

            progressHeartbeatTimer.Start();
        }

        protected override string GetStatusMessage()
        {
            long tasksCompleted = tasks.Count(task => task.IsCompleted);
            long tasksRunning = tasks.Count(task => task.Status == TaskStatus.Running);
            long tasksRemaining = totalTasks - tasksCompleted;

            string percentCompleteString;
            if (totalTasks <= 0 || tasksCompleted < 0)
            {
                percentCompleteString = "N/A";
            }
            else
            {
                int percentComplete = (int)Math.Floor(tasksCompleted * 100.0 / totalTasks);
                percentCompleteString = String.Format("{0}%", percentComplete);
            }

            return progressFormatMessage.Replace("{TotalTasks}", totalTasks.ToString())
                                        .Replace("{TasksCompleted}", tasksCompleted.ToString())
                                        .Replace("{TasksRunning}", tasksRunning.ToString())
                                        .Replace("{TasksRemaining}", tasksRemaining.ToString())
                                        .Replace("{PercentComplete}", percentCompleteString);
        }
    }
}