using System;
using System.IO;
using FluentAssertions;
using LogShark.Writers.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace LogShark.Tests
{
    public class OutputDirInitializerTests
    {
        private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();
        
        [Fact]
        public void TestThrowingOnExistingOutputDir()
        {
            const string outputDirName = "OutputDirInitializerTest";
            const string runId = "12345";
            var existingDirName = Path.Combine(outputDirName, runId);
            Directory.CreateDirectory(existingDirName);
            
            Action initWithThrow = () => OutputDirInitializer.InitDirs(
                outputDirName,
                runId,
                null,
                "testWriter",
                _loggerFactory,
                true);
            initWithThrow.Should().Throw<ArgumentException>().Which.Message.Should().Contain(OutputDirInitializer.OutputDirAlreadyExistsMessageTail);
            
            // This statement should not throw, as throwing parameter set to false
            OutputDirInitializer.InitDirs(
                outputDirName,
                runId,
                null,
                "testWriter",
                _loggerFactory,
                false);
            
            Directory.Delete(existingDirName, true);
        }
    }
}