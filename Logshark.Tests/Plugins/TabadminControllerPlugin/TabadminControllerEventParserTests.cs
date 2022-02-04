using System;
using System.Collections.Generic;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.TabadminController;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Extensions;
using LogShark.Tests.Plugins.Helpers;
using Moq;
using Xunit;

namespace LogShark.Tests.Plugins.TabadminControllerPlugin
{
    public class TabadminControllerEventParserTests
    {
        private readonly Mock<IBuildTracker> _buildTrackerMock;
        private readonly LogLine _testLogLine;
        private readonly Mock<IProcessingNotificationsCollector> _processingNotificationCollectorMock;
        private readonly TabadminControllerEventParser _eventParser;

        #region Fixed values for "TestDifferentEvents"

        private const string FileName = "file1"; 
        private const string FilePath = "dir1/file1"; 
        private const int LineNumber = 123456; 
        private const int ProcessId = 4321;
        private const string Thread = "testThread";
        private static readonly DateTime Timestamp = new DateTime(2020, 10, 20, 10, 0, 0); 
        private const string Worker = "node0"; 
        
        #endregion Fixed values for "TestDifferentEvents"
        
        public TabadminControllerEventParserTests()
        {
            _buildTrackerMock = new Mock<IBuildTracker>();
            _processingNotificationCollectorMock = new Mock<IProcessingNotificationsCollector>();
            
            _eventParser = new TabadminControllerEventParser(_buildTrackerMock.Object, _processingNotificationCollectorMock.Object);
            
            _testLogLine = new LogLine(
                new ReadLogLineResult(LineNumber, null), // line content should not be used 
                new LogFileInfo(FileName, FilePath, Worker, DateTime.MinValue) ); // Date modified should not be used
        }

        [Fact]
        public void MakingSureBuildEventCallsBuildTracker()
        {
            const string buildNumber = "202001.20.1020.1234";
            
            var testLine = GetTestLine(
                "com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder",
                $"Loading topology settings from /etc/Tableau/Tableau Server/data/tabsvc/config/tabadmincontroller_0.{buildNumber}/topology.yml",
                "INFO");

            var result = _eventParser.ParseEvent(_testLogLine, testLine);

            result.Should().BeNull();
            _buildTrackerMock.Verify(m => m.AddBuild(Timestamp, buildNumber), Times.Once);
            _buildTrackerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(EventTestCases))]
        public void TestDifferentEvents(string testName, string @class, string message, string severity, IDictionary<string, object> nonNullProps)
        {
            var testLine = GetTestLine(@class, message, severity);

            var result = _eventParser.ParseEvent(_testLogLine, testLine);

            if (nonNullProps == null)
            {
                result.Should().BeNull();
                return;
            }
            
            result.Should().NotBeNull();
            var expectedPropValues = AddFixedPropValues(nonNullProps, @class, message, severity);
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(result, expectedPropValues, testName);
            result.VerifyBaseEventProperties(Timestamp, _testLogLine);
        }
        
        [Theory]
        [MemberData(nameof(BuildTestCases))]
        public void TestBuildEvents(string message, string expectedBuild)
        {
            var testLine = GetTestLine("com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder", message, "INFO");

            var result = _eventParser.ParseEvent(_testLogLine, testLine);
            result.Should().BeNull();

            if (expectedBuild != null)
            {
                _buildTrackerMock.Verify(m => m.AddBuild(Timestamp, expectedBuild));
            }
            
            _buildTrackerMock.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> EventTestCases => new List<object[]>
        {
            new object[]
            {
                "No Message",
                "com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder",
                "",
                "INFO",
                null
            },
            
            new object[]
            {
                "Class we don't parse",
                "some.other.class",
                @"Loading topology settings from C:\ProgramData\Tableau\Tableau Server\data\tabsvc\config\tabadmincontroller_0.20192.19.0718.1543\topology.yml",
                "INFO",
                null
            },

            new object[]
            {
                "Start Job Event - Good",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Running job 114 of type StopServerJob",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Job Start" },
                    { "JobId", 114 },
                    { "JobType", "StopServerJob" }
                }
            },
            
            new object[]
            {
                "Start Job Event - Bad JobId",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Running job abc of type StopServerJob",
                "INFO",
                null
            },
            
            new object[]
            {
                "Start Job Event - No JobType",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Running job 114 of type ",
                "INFO",
                null
            },
            
            new object[]
            {
                "Job Status Update - Good",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Updated status for job 114 of type StopServerJob to Running",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Job Status Update" },
                    { "JobId", 114 },
                    { "JobStatus", "Running" },
                    { "JobType", "StopServerJob" }
                }
            },
            
            new object[]
            {
                "Job Status Update - No JobId",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Updated status for job abc of type StopServerJob to Running",
                "INFO",
                null
            },
            
            new object[]
            {
                "Job Status Update - No JobType",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Updated status for job 114 of type  to Running",
                "INFO",
                null
            },
            
            new object[]
            {
                "Job Status Update - No Status",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService",
                "Updated status for job 114 of type StopServerJob to ",
                "INFO",
                null
            },
            
            new object[]
            {
                "Job Progress Update - Good",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.JobStepRunner",
                "Progress update for job StopServerJob, id: 114, step: DisableAllServices, status: Running, message key: job.stop_server.step.disable_all_services, message data:",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Job Progress Update" },
                    { "JobId", 114 },
                    { "JobType", "StopServerJob" },
                    { "StepMessage", "message key: job.stop_server.step.disable_all_services, message data:" },
                    { "StepName", "DisableAllServices" },
                    { "StepStatus", "Running" }
                }
            },
            
            new object[]
            {
                "Job Progress Update - Bad JobId",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.JobStepRunner",
                "Progress update for job StopServerJob, id: abc, step: DisableAllServices, status: Running, message key: job.stop_server.step.disable_all_services, message data:",
                "INFO",
                null
            },
            
            new object[]
            {
                "Job Progress Update - Missing step name",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.JobStepRunner",
                "Progress update for job StopServerJob, id: 114, step: , status: Running, message key: job.stop_server.step.disable_all_services, message data:",
                "INFO",
                null
            },
            
            new object[]
            {
                "Password-less login request - Good - Windows",
                "com.tableausoftware.tabadmin.webapp.impl.windows.WindowsPasswordLessLoginManager",
                @"Password-less login request from user 'domain.com\hacker' with SID 'S-1-5-21-16448756584-8954878-355445162-16809'",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Password-less Login Request" },
                    { "LoginUserId", "S-1-5-21-16448756584-8954878-355445162-16809" },
                    { "LoginUsername", @"domain.com\hacker" }
                }
            },
            
            new object[]
            {
                "Password-less login request - Good - Linux",
                "com.tableausoftware.tabadmin.webapp.impl.linux.LinuxPasswordLessLoginManager",
                "Password-less login request from user 'root' with uid '1234'",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Password-less Login Request" },
                    { "LoginUserId", "1234" },
                    { "LoginUsername", "root" }
                }
            },
            
            new object[]
            {
                "Password-less login request - No Username - Linux",
                "com.tableausoftware.tabadmin.webapp.impl.linux.LinuxPasswordLessLoginManager",
                "Password-less login request from user '' with uid '1234'",
                "INFO",
                null
            },
            
            new object[]
            {
                "Password-less login success - Good - Windows",
                "com.tableausoftware.tabadmin.webapp.impl.windows.WindowsPasswordLessLoginManager",
                @"User with username 'domain\user1' is logged in via password-less auth",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Password-less Login Success" },
                    { "LoginUsername", @"domain\user1" }
                }
            },
            
            new object[]
            {
                "Password-less login success - Good - Linux",
                "com.tableausoftware.tabadmin.webapp.impl.linux.LinuxPasswordLessLoginManager",
                "User with uid '1234' and username 'root' is logged in via password-less auth",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Password-less Login Success" },
                    { "LoginUserId", "1234" },
                    { "LoginUsername", "root" }
                }
            },
            
            new object[]
            {
                "Password-less login success - No Username - Linux",
                "com.tableausoftware.tabadmin.webapp.impl.linux.LinuxPasswordLessLoginManager",
                "User with uid '1234' and username '' is logged in via password-less auth",
                "INFO",
                null
            },
            
            new object[]
            {
                "Login Request From Client - Good - IPv4",
                "com.tableausoftware.tabadmin.webapp.api.v1.LoginController",
                @"Login request from client 'webui' at '192.193.194.195' for user 'domain.com\hacker'",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Login Request From Client" },
                    { "LoginClientId", "webui" },
                    { "LoginClientIp", "192.193.194.195" },
                    { "LoginUsername", @"domain.com\hacker" }
                }
            },
            
            new object[]
            {
                "Login Request From Client - Good - IPv6",
                "com.tableausoftware.tabadmin.webapp.api.v1.LoginController",
                @"Login request from client 'webui' at '2001:0db8:85a3:0000:0000:8a2e:0370:7334' for user 'domain.com\hacker'",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Login Request From Client" },
                    { "LoginClientId", "webui" },
                    { "LoginClientIp", "2001:0db8:85a3:0000:0000:8a2e:0370:7334" },
                    { "LoginUsername", @"domain.com\hacker" }
                }
            },
            
            new object[]
            {
                "Login Request From Client - No Username",
                "com.tableausoftware.tabadmin.webapp.api.v1.LoginController",
                @"Login request from client 'webui' at '2001:0db8:85a3:0000:0000:8a2e:0370:7334' for user ''",
                "INFO",
                null
            },
            
            new object[]
            {
                "Login Success - Good",
                "com.tableausoftware.tabadmin.webapp.impl.windows.WindowsAuthenticationManager",
                @"User domain.com\hacker is authorized",
                "INFO",
                new Dictionary<string, object>
                {
                    { "EventType", "Login From Client Success" },
                    { "LoginUsername", @"domain.com\hacker" }
                }
            },
            
            new object[]
            {
                "Login Success - No Username",
                "com.tableausoftware.tabadmin.webapp.impl.windows.WindowsAuthenticationManager",
                @"User  is authorized",
                "INFO",
                null
            },
            
            new object[]
            {
                "Config Change request - Good",
                "com.tableausoftware.tabadmin.webapp.config.HotConfiguration",
                @"Found 3 cold config changes: [features.TDSNativeServiceDeploy, features.TDSServiceDeploy, features.NLServices]",
                "INFO",
                new Dictionary<string, object>
                {
                    { "ConfigParametersChanging", "features.TDSNativeServiceDeploy, features.TDSServiceDeploy, features.NLServices" },
                    { "EventType", "Cold Config Changes Found" }
                }
            },
            
            new object[]
            {
                "Config Change request - Missing config list",
                "com.tableausoftware.tabadmin.webapp.config.HotConfiguration",
                @"Found 3 cold config changes: ",
                "INFO",
                null
            },
            
            new object[]
            {
                "Warning Severity",
                "doesn't matter",
                "Something went wrong",
                "WARN",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" }
                }
            },
            
            new object[]
            {
                "Error Severity",
                "doesn't matter",
                "Something went very wrong",
                "ERROR",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" }
                }
            },
            
            new object[]
            {
                "Fatal Severity",
                "doesn't matter",
                "Something went very very wrong",
                "ERROR",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" }
                }
            },
            
            // These few tests are very artificial. Just documenting the "error" severity takes precedence over message contents
            
            new object[]
            {
                "Parse Version info - With Error Severity",
                "com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder",
                @"Loading topology settings from C:\ProgramData\Tableau\Tableau Server\data\tabsvc\config\tabadmincontroller_0.20192.19.0718.1543\topology.yml",
                "error",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" }
                }
            },
            
            new object[]
            {
                "Job Progress Update - With Warn Severity",
                "com.tableausoftware.tabadmin.webapp.asyncjobs.JobStepRunner",
                "Progress update for job StopServerJob, id: 114, step: DisableAllServices, status: Running, message key: job.stop_server.step.disable_all_services, message data:",
                "warn",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" },
                }
            },
            
            new object[]
            {
                "Config Change request - With Fatal Severity",
                "com.tableausoftware.tabadmin.webapp.config.HotConfiguration",
                @"Found 3 cold config changes: [features.TDSNativeServiceDeploy, features.TDSServiceDeploy, features.NLServices]",
                "FATAL",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" }
                }
            },
            
            new object[]
            {
                "Password-less login request - With Warn Severity - Linux",
                "com.tableausoftware.tabadmin.webapp.impl.linux.LinuxPasswordLessLoginManager",
                "Password-less login request from user 'root' with uid '1234'",
                "WARN",
                new Dictionary<string, object>
                {
                    { "EventType", "Error - Tabadmin Controller" },
                }
            },
        };

        public static IEnumerable<object[]> BuildTestCases => new List<object[]>
        {
            new object[]
            {
                // Parse Version info - Good
                @"Loading topology settings from C:\ProgramData\Tableau\Tableau Server\data\tabsvc\config\tabadmincontroller_0.20192.19.0718.1543\topology.yml",
                "20192.19.0718.1543"
            },

            new object[]
            {
                // Parse Version info - Bad Build
                @"Loading topology settings from C:\ProgramData\Tableau\Tableau Server\data\tabsvc\config\tabadmincontroller_test\topology.yml",
                null
            },
        };

        private static JavaLineMatchResult GetTestLine(string @class, string message, string severity)
        {
            return new JavaLineMatchResult(true)
            {
                Class = @class,
                Message = message,
                ProcessId = ProcessId,
                Severity = severity,
                Thread = Thread,
                Timestamp = Timestamp
            };
        }

        private static IDictionary<string, object> AddFixedPropValues(IDictionary<string, object> nonNullProps, string @class, string message, string severity)
        {
            nonNullProps.Add("Class", @class);
            nonNullProps.Add("Message", message);
            nonNullProps.Add("ProcessId", ProcessId);
            nonNullProps.Add("Severity", severity);
            nonNullProps.Add("Thread", Thread);

            return nonNullProps;
        }
    }
}