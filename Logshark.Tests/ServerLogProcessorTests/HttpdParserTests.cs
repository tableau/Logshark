using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering Apache log parsing.")]
    public class HttpdParserTests
    {
        [Test, Description("Parses a sample 2018.2+ Httpd log line to a JObject.  This parse must not yield any properties which are empty or equal to '-', and also standardize the timestamp.")]
        public void ParseHttpdLogLine_v2018Q2()
        {
            const string sampleLogLine =
                @"10.210.24.3 127.0.0.1 - 2018-05-09T16:07:58.120 ""GMT Daylight Time"" 80 ""POST /vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3 HTTP/1.1"" ""-"" 200 136026 ""784"" 2370951 WvMOzgKIhfzh9kFWO@ow2gAAA1Y";
            const string expectedResult =
                @"{""request_ip"":""10.210.24.3"",""requester"":""127.0.0.1"",""ts"":""2018-05-09T09:07:58.12-07:00"",""ts_offset"":""0100"",""port"":""80"",""request_method"":""POST"",""resource"":""/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3"",""http_version"":""1.1"",""status_code"":""200"",""response_size"":""136026"",""content_length"":""784"",""request_time"":""2370951"",""request_id"":""WvMOzgKIhfzh9kFWO@ow2gAAA1Y"",""line"":1}";

            var actualResult = ParserTestHelpers.ParseSingleLine(sampleLogLine, new HttpdParser());
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test, Description("Parses a sample 9.x Httpd log line to a JObject.  This parse must not yield any properties which are empty or equal to '-', and also standardize the timestamp.")]
        public void ParseHttpdLogLine_v9()
        {
            const string sampleLogLine =
                @"3.209.152.107 - - 2015-07-17 00:01:04.222 Eastern Daylight Time 80 ""GET /t/Nuclear/views/IOUClosurereport/IOUCLosureReport?:iid=1&:embed=y HTTP/1.1"" ""-"" 200 18388 ""-"" 11341724 Vah@AAMoOaUAADKU7-cAAAMO";
            const string expectedResult =
                @"{""request_ip"":""3.209.152.107"",""ts"":""2015-07-16T17:01:04.222-07:00"",""ts_offset"":""-0400"",""port"":""80"",""request_method"":""GET"",""resource"":""/t/Nuclear/views/IOUClosurereport/IOUCLosureReport?:iid=1&:embed=y"",""http_version"":""1.1"",""status_code"":""200"",""response_size"":""18388"",""request_time"":""11341724"",""request_id"":""Vah@AAMoOaUAADKU7-cAAAMO"",""line"":1}";

            var actualResult = ParserTestHelpers.ParseSingleLine(sampleLogLine, new HttpdParser());
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test, Description("Parses a sample 8.x Httpd log line to a JObject.  This parse must not yield any properties which are empty or equal to '-', and also standardize the timestamp.")]
        public void ParseHttpdLogLine_v8()
        {
            const string sampleLogLine =
                @"10.17.136.120 - - [25/Feb/2014:02:20:12 -0800] 80 ""GET /tableau_prefix_local/workbooks/dsvc_test_workbook_8_1_0.twb?language=en HTTP/1.1"" ""10.32.149.80, 10.17.136.120"" 200 1192683 ""-"" 218401 UwxuXAoRhHQAACGoBVwAAAGK";
            const string expectedResult =
                @"{""request_ip"":""10.17.136.120"",""ts"":""2014-02-24T18:20:12-08:00"",""ts_offset"":""-0800"",""port"":""80"",""request_method"":""GET"",""resource"":""/tableau_prefix_local/workbooks/dsvc_test_workbook_8_1_0.twb?language=en"",""http_version"":""1.1"",""xforwarded_for"":""10.32.149.80, 10.17.136.120"",""status_code"":""200"",""response_size"":""1192683"",""request_time"":""218401"",""request_id"":""UwxuXAoRhHQAACGoBVwAAAGK"",""line"":1}";

            var actualResult = ParserTestHelpers.ParseSingleLine(sampleLogLine, new HttpdParser());
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test, Description("Parses a sample httpd access logfile and ensures that all documents were parsed correctly.")]
        [TestCase("httpd_access_9x.txt", Description = "access log, 9.x+ Format")]
        public void ParseHttpdAccessFullFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new HttpdParser());

            var lineCount = File.ReadAllLines(logPath).Length;
            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }

        [Test, Description("Parses a sample httpd error logfile and ensures that all documents were parsed correctly.")]
        [TestCase("httpd_error_9x.txt", Description = "error log, 9.x+ Format")]
        public void ParseHttpdErrorFullFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new HttpdErrorParser());

            var lineCount = File.ReadAllLines(logPath).Length;
            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}