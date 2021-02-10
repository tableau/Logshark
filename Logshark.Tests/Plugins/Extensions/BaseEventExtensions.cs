using System;
using FluentAssertions;
using LogShark.Plugins;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Tests.Plugins.Extensions
{
    public static class BaseEventExtensions
    {
        public static void VerifyBaseEventProperties(this BaseEvent baseEvent, DateTime expectedTimestamp, LogLine logLine)
        {
            baseEvent.FileName.Should().Be(logLine.LogFileInfo.FileName);
            baseEvent.FilePath.Should().Be(logLine.LogFileInfo.FilePath);
            baseEvent.LineNumber.Should().Be(logLine.LineNumber);
            baseEvent.Timestamp.Should().Be(expectedTimestamp);
            baseEvent.Worker.Should().Be(logLine.LogFileInfo.Worker);
        }
    }
}