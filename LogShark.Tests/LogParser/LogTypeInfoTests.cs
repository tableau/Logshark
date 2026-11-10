using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Shared.LogReading.Readers;
using Xunit;

namespace LogShark.Tests.LogParser
{
    public class LogTypeInfoTests : InvariantCultureTestsBase
    {
        private readonly Func<Stream, string, ILogReader> _testFunc = (stream, __) => new SimpleLinePerLineReader(stream);

        [Fact]
        public void VerifyConstructorChecks()
        {
            Action creatingMultilineWithoutLogReader = () => new LogTypeInfo(LogType.Apache, null, new List<Regex>());
            Action creatingWithNullFileLocations = () => new LogTypeInfo(LogType.Apache, _testFunc, null);
            Action creatingWithEmptyFileLocations = () => new LogTypeInfo(LogType.Apache, _testFunc, new List<Regex>());

            creatingMultilineWithoutLogReader.Should().Throw<ArgumentException>();
            creatingWithNullFileLocations.Should().Throw<ArgumentException>();
            creatingWithEmptyFileLocations.Should().Throw<ArgumentException>();
        }
        
        [Theory]
        [InlineData("", false)]
        [InlineData("fileOne.txt", true)]
        [InlineData("folder/fileOne.txt", true)]
        [InlineData("FILEONE.txt", false)] // Regex is case sensitive by default 
        [InlineData("fileTwo.txt", false)]
        public void TestSingleLocationInfo(string filename, bool expectedResult)
        {
            var singleLocationInfo = new LogTypeInfo(LogType.Apache, (stream, _) => new SimpleLinePerLineReader(stream), new List<Regex>
            {
                new Regex(@"fileOne\.txt", RegexOptions.Compiled)
            });
            
            var result = singleLocationInfo.FileBelongsToThisType(filename);
            
            result.Should().Be(expectedResult);
        }
        
        [Theory]
        [InlineData("", false)]
        [InlineData("fileOne.txt", true)]
        [InlineData("folder/fileOne.txt", true)]
        [InlineData("FILEONE.txt", false)] // Regex is case sensitive by default 
        [InlineData("fileTwo.txt", true)]
        [InlineData("fileThree.txt", false)]
        public void TestMultiLocationInfo(string filename, bool expectedResult)
        {
            var singleLocationInfo = new LogTypeInfo(LogType.Apache, _testFunc, new List<Regex>
            {
                new Regex(@"fileOne\.txt", RegexOptions.Compiled),
                new Regex(@"fileTwo\.txt", RegexOptions.Compiled)
            });
            
            var result = singleLocationInfo.FileBelongsToThisType(filename);

            result.Should().Be(expectedResult);
        }

        [Theory]
        // adhocMode=false uses FileLocations (structured path patterns)
        [InlineData("folder/fileOne.txt", false, true)]
        [InlineData("adhocFile.log", false, false)]
        // adhocMode=true uses AdhocFileLocations (filename-only patterns)
        [InlineData("adhocFile.log", true, true)]
        [InlineData("folder/fileOne.txt", true, false)] // structured pattern not consulted in adhoc mode
        public void FileBelongsToThisType_AdhocMode(string filename, bool adhocMode, bool expectedResult)
        {
            var logTypeInfo = new LogTypeInfo(
                LogType.Apache,
                _testFunc,
                fileLocations: new List<Regex> { new Regex(@"fileOne\.txt", RegexOptions.Compiled) },
                adhocFileLocations: new List<Regex> { new Regex(@"^adhocFile\.log$", RegexOptions.Compiled) });

            logTypeInfo.FileBelongsToThisType(filename, adhocMode).Should().Be(expectedResult);
        }

        [Fact]
        public void FileBelongsToThisType_AdhocMode_NoAdhocPatterns_NeverMatches()
        {
            // Log types without adhoc patterns cannot be classified from a flat file dump
            var logTypeInfo = new LogTypeInfo(
                LogType.Apache,
                _testFunc,
                fileLocations: new List<Regex> { new Regex(@"fileOne\.txt", RegexOptions.Compiled) });

            logTypeInfo.FileBelongsToThisType("fileOne.txt", adhocMode: true).Should().BeFalse();
            logTypeInfo.FileBelongsToThisType("fileOne.txt", adhocMode: false).Should().BeTrue();
        }
    }
}