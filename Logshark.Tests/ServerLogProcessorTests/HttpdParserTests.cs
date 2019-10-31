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

        [Test, Description("Parse missing request method and bad content length.")]
        public void ParseHttpdLogLineWithMissingRequestMethodAndBadContentLength()
        {
            const string sampleLogLinev2018 =
                @"10.210.24.3 127.0.0.1 - 2018-05-09T16:07:58.120 ""GMT Daylight Time"" 80 ""/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3 HTTP/1.1"" ""-"" 200 136026 ""<script>alert(Content-Length)</script>"" 2370951 WvMOzgKIhfzh9kFWO@ow2gAAA1Y";
            const string expectedResultv2018 =
                @"{""request_ip"":""10.210.24.3"",""requester"":""127.0.0.1"",""ts"":""2018-05-09T09:07:58.12-07:00"",""ts_offset"":""0100"",""port"":""80"",""resource"":""/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3"",""http_version"":""1.1"",""status_code"":""200"",""response_size"":""136026"",""request_time"":""2370951"",""request_id"":""WvMOzgKIhfzh9kFWO@ow2gAAA1Y"",""line"":1}";
            var result = ParserTestHelpers.ParseSingleLine(sampleLogLinev2018, new HttpdParser());
            Assert.AreEqual(expectedResultv2018, result);

            const string sampleLogLinev9 =
                @"172.16.115.248 - - 2019-01-10 20:07:14.256 Eastern Standard Time 443 ""BASELINE-CONTROL /jqZPZpAJ.htm HTTP/1.1"" ""45.33.86.32"" 405 246 ""<script>alert(Content-Length)</script>"" 15632 XDfsQrLI3QXJ7VR7OZto1QAAA@M";
            const string expectedResultv9 =
                @"{""request_ip"":""172.16.115.248"",""ts"":""2019-01-10T12:07:14.256-08:00"",""ts_offset"":""-0500"",""port"":""443"",""resource"":""BASELINE-CONTROL /jqZPZpAJ.htm"",""http_version"":""1.1"",""xforwarded_for"":""45.33.86.32"",""status_code"":""405"",""response_size"":""246"",""request_time"":""15632"",""request_id"":""XDfsQrLI3QXJ7VR7OZto1QAAA@M"",""line"":1}";
            result = ParserTestHelpers.ParseSingleLine(sampleLogLinev9, new HttpdParser());
            Assert.AreEqual(expectedResultv9, result);

            const string sampleLogLinev8 =
                @"10.17.136.120 - - [25/Feb/2014:02:20:12 -0800] 80 ""/tableau_prefix_local/workbooks/dsvc_test_workbook_8_1_0.twb?language=en HTTP/1.1"" ""10.32.149.80, 10.17.136.120"" 200 1192683 ""<script>alert(Content-Length)</script>"" 218401 UwxuXAoRhHQAACGoBVwAAAGK";
            const string expectedResultv8 =
                @"{""request_ip"":""10.17.136.120"",""ts"":""2014-02-24T18:20:12-08:00"",""ts_offset"":""-0800"",""port"":""80"",""resource"":""/tableau_prefix_local/workbooks/dsvc_test_workbook_8_1_0.twb?language=en"",""http_version"":""1.1"",""xforwarded_for"":""10.32.149.80, 10.17.136.120"",""status_code"":""200"",""response_size"":""1192683"",""request_time"":""218401"",""request_id"":""UwxuXAoRhHQAACGoBVwAAAGK"",""line"":1}";
            result = ParserTestHelpers.ParseSingleLine(sampleLogLinev8, new HttpdParser());
            Assert.AreEqual(expectedResultv8, result);
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