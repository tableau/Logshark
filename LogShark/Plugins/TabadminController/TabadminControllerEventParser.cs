using System;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminControllerEventParser
    {
        private readonly IBuildTracker _buildTracker;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public TabadminControllerEventParser(IBuildTracker buildTracker, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _buildTracker = buildTracker;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public TabadminControllerEvent ParseEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            if (string.IsNullOrWhiteSpace(javaLineMatchResult.Message))
            {
                _processingNotificationsCollector.ReportError($"Line does not appear to have a message", logLine, nameof(TabadminControllerEventParser));
                return null;
            }
            
            if (javaLineMatchResult.IsWarningPriorityOrHigher())
            {
                return new TabadminControllerEvent("Error - Tabadmin Controller", logLine, javaLineMatchResult, _buildTracker);
            }

            if (javaLineMatchResult.Class.Equals("com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder", StringComparison.InvariantCulture))
            {
                return ParseVersionInfo(logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Class.StartsWith("com.tableausoftware.tabadmin.webapp.asyncjobs.", StringComparison.InvariantCulture))
            {
                return ParseAsyncJobServiceMessages(logLine, javaLineMatchResult);
            }
            
            if (javaLineMatchResult.Class.StartsWith("com.tableausoftware.tabadmin.webapp.config."))
            {
                return ParseConfigChangeRequests(logLine, javaLineMatchResult);
            }
            
            if (javaLineMatchResult.Class.StartsWith("com.tableausoftware.tabadmin.webapp."))
            {
                return ParseAuthenticationRequests(logLine, javaLineMatchResult);
            }

            return null; // Line did not match any known events
        }

        // Example - Loading topology settings from C:\ProgramData\Tableau\Tableau Server\data\tabsvc\config\tabadmincontroller_0.20192.19.0718.1543\topology.yml
        private static readonly Regex LoadingTopologyLine = new Regex(@"^Loading topology settings from .+tabadmincontroller_(?<build>[\d\.]+)[\\\/]topology.yml$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseVersionInfo(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = LoadingTopologyLine.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }
            
            var rawBuild = match.GetNullableString("build");

            if (string.IsNullOrWhiteSpace(rawBuild))
            {
                _processingNotificationsCollector.ReportError("Line looks like a loading topology settings, but build string cannot be parsed", logLine, nameof(TabadminControllerEventParser));
                return null;
            }
            
            var build = rawBuild.StartsWith("0.")
                ? rawBuild.Substring(2)
                : rawBuild;

            _buildTracker.AddBuild(javaLineMatchResult.Timestamp, build);
            
            return new TabadminControllerEvent("Loading Topology", logLine, javaLineMatchResult, _buildTracker);
        }

        private TabadminControllerEvent ParseAsyncJobServiceMessages(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            if (javaLineMatchResult.Message.StartsWith("Running job"))
            {
                return ParseStartJobEvent(logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Message.StartsWith("Updated status for job"))
            {
                return ParseJobStatusUpdateEvent(logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Message.StartsWith("Progress update for job"))
            {
                return ParseJobProgressUpdate(logLine, javaLineMatchResult);
            }

            return null; // Line did not match any known events
        }

        // Example - Running job 12 of type RestartServerJob
        private static readonly Regex JobStartMessage = new Regex(@"^Running job (?<jobId>\d+) of type (?<jobType>\w+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseStartJobEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = JobStartMessage.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }

            return new TabadminControllerEvent("Job Start", logLine, javaLineMatchResult, _buildTracker)
            {
                JobId = match.GetNullableLong("jobId"),
                JobType = match.GetNullableString("jobType")
            };
        }
        
        // Example - Updated status for job 12 of type RestartServerJob to Succeeded
        private static readonly Regex JobStatusUpdateEvent = new Regex(@"^Updated status for job (?<jobId>\d+) of type (?<jobType>\w+) to (?<jobStatus>\w+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseJobStatusUpdateEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = JobStatusUpdateEvent.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }

            return new TabadminControllerEvent("Job Status Update", logLine, javaLineMatchResult, _buildTracker)
            {
                JobId = match.GetNullableLong("jobId"),
                JobStatus = match.GetNullableString("jobStatus"),
                JobType = match.GetNullableString("jobType")
            };
        }
        
        // Example - Progress update for job RestartServerJob, id: 12, step: DisableAllServices, status: Running, message key: job.stop_server.step.disable_all_services, message data: 
        private static readonly Regex JobProgressUpdate = new Regex(@"^Progress update for job (?<jobType>\w+), id: (?<jobId>\d+), step: (?<stepName>\w+), status: (?<stepStatus>\w+), (?<stepMessage>.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseJobProgressUpdate(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = JobProgressUpdate.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }
            
            return new TabadminControllerEvent("Job Progress Update", logLine, javaLineMatchResult, _buildTracker)
            {
                JobId = match.GetNullableLong("jobId"),
                JobType = match.GetNullableString("jobType"),
                StepMessage = match.GetNullableString("stepMessage"),
                StepName = match.GetNullableString("stepName"),
                StepStatus = match.GetNullableString("stepStatus")
            };
        }
        
        private TabadminControllerEvent ParseAuthenticationRequests(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            if (javaLineMatchResult.Message.StartsWith("Password-less login request from user"))
            {
                return ParsePasswordLessLoginRequest(logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Message.StartsWith("User with"))
            {
                return ParsePasswordLessLoginSuccess(logLine, javaLineMatchResult);
            }
            
            if (javaLineMatchResult.Message.StartsWith("Login request from client"))
            {
                return ParseLoginRequestFromClientEvent(logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Message.StartsWith("User"))
            {
                return ParseClientUserAuthorizedEvent(logLine, javaLineMatchResult);
            }

            return null; // Line did not match any known events
        }
        
        // Example Windows - Password-less login request from user 'domain\user1' with SID 'S-1-5-21-16448756584-8954878-355445162-16809'
        // Example Linux - Password-less login request from user 'root' with uid '1234'
        private static readonly Regex PasswordLessLoginRequest = new Regex(@"^Password-less login request from user '(?<username>[^']+)' with (SID|uid) '(?<userId>[^']+)'$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParsePasswordLessLoginRequest(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = PasswordLessLoginRequest.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }
            
            return new TabadminControllerEvent("Password-less Login Request", logLine, javaLineMatchResult, _buildTracker)
            {
                LoginUserId = match.GetNullableString("userId"),
                LoginUsername = match.GetNullableString("username")
            };
        }
        
        // Example Windows - User with username 'domain\user1' is logged in via password-less auth
        // Example Linux - User with uid '1234' and username 'root' is logged in via password-less auth
        private static readonly Regex PasswordLessLoginSuccess = new Regex(@"^User with (uid '(?<userId>[^']+)' and )?username '(?<username>[^']+)' is logged in via password-less auth$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParsePasswordLessLoginSuccess(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = PasswordLessLoginSuccess.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }

            return new TabadminControllerEvent("Password-less Login Success", logLine, javaLineMatchResult, _buildTracker)
            {
                LoginUserId = match.GetNullableString("userId"),
                LoginUsername = match.GetNullableString("username")
            };
        }

        // Example - Login request from client 'webui' at '0:0:0:0:0:0:0:1' for user 'domain.com\hacker' 
        private static readonly Regex LoginRequestFromClient = new Regex(@"^Login request from client '(?<clientId>[^']+)' at '(?<clientIp>[^']+)' for user '(?<username>[^']+)'", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseLoginRequestFromClientEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = LoginRequestFromClient.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }

            return new TabadminControllerEvent("Login Request From Client", logLine, javaLineMatchResult, _buildTracker)
            {
                LoginClientId = match.GetNullableString("clientId"),
                LoginClientIp = match.GetNullableString("clientIp"),
                LoginUsername = match.GetNullableString("username")
            };
        }
        
        // Example - User _user1 is authorized
        private static readonly Regex ClientUserAuthorized = new Regex(@"^User (?<username>[^\s]+) is authorized$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseClientUserAuthorizedEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = ClientUserAuthorized.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }
            
            return new TabadminControllerEvent("Login From Client Success", logLine, javaLineMatchResult, _buildTracker)
            {
                LoginUsername = match.GetNullableString("username")
            };
        }

        private TabadminControllerEvent ParseConfigChangeRequests(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            if (javaLineMatchResult.Message.StartsWith("Found"))
            {
                return ParseColdConfigChangesFoundEvent(logLine, javaLineMatchResult);
            }

            return null; // Line did not match any known events
        }

        // Example - Found 3 cold config changes: [features.TDSNativeServiceDeploy, features.TDSServiceDeploy, features.NLServices]
        private static readonly Regex ColdConfigChangesFound = new Regex(@"^Found \d+ cold config changes: \[(?<configParametersList>[^\]]+)\]$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseColdConfigChangesFoundEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = ColdConfigChangesFound.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }
            
            return new TabadminControllerEvent("Cold Config Changes Found", logLine, javaLineMatchResult, _buildTracker)
            {
                ConfigParametersChanging = match.GetNullableString("configParametersList")
            };
        }
    }
}