using FluentAssertions;
using Logshark.PluginLib.Persistence.Extract;
using Logshark.Tests.Helpers;
using NUnit.Framework;
using System;
using System.IO;
using Tableau.ExtractApi;

namespace Logshark.Tests
{
    [TestFixture]
    internal class ExtractTests
    {
        private static readonly string testDataDirectory = TestDataHelper.GetDataDirectory();

        // Test model that contains all supported types
        private class Widget
        {
            public bool Bool { get; set; }
            public char Char { get; set; }
            public bool? NullableBool { get; set; }
            public DateTime DateTime { get; set; }
            public DateTime? NullableDateTime { get; set; }
            public decimal Decimal { get; set; }
            public decimal? NullableDecimal { get; set; }
            public float Float { get; set; }
            public float? NullableFloat { get; set; }
            public int Integer { get; set; }
            public int? NullableInteger { get; set; }
            public long Long { get; set; }
            public long? NullableLong { get; set; }
            public string String { get; set; }
        }

        [OneTimeSetUp]
        public void Init()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        [Test]
        [TestCase(@"SampleExtractCreate.hyper")]
        public void CreateExtract(string filename)
        {
            var extractPath = InitializeTestFile(filename);

            using (var extract = new HyperExtract<Widget>(extractPath))
            {
                var widget = new Widget { String = "foo", NullableBool = true };
                var insertionResult = extract.Insert(widget);

                insertionResult.HasValue.Should().BeTrue("Inserting an item should not return None");
            }
        }

        [Test]
        [TestCase(@"SampleExtractAppend.hyper")]
        public void AppendToExtract(string filename)
        {
            var extractPath = InitializeTestFile(filename);

            using (var extract = new HyperExtract<Widget>(extractPath))
            {
                var widget = new Widget { String = "foo", NullableBool = false };
                var insertionResult = extract.Insert(widget);
            }

            using (var extract = new HyperExtract<Widget>(extractPath))
            {
                var widget = new Widget { String = "bar", NullableBool = true };
                var insertionResult = extract.Insert(widget);

                insertionResult.HasValue.Should().BeTrue("Inserting an item to an existing extract should not return None");
            }
        }

        [Test]
        [TestCase(@"SampleExtractCreatedWithPersister.hyper")]
        public void CreateExtractWithPersister(string filename)
        {
            string extractPath = InitializeTestFile(filename);

            var persisterFactory = new ExtractPersisterFactory(testDataDirectory);
            using (var persister = persisterFactory.CreateExtract<Widget>(filename))
            {
                for (int i = 1; i <= 100000; i++)
                {
                    var widget = new Widget { String = "foo", Integer = i };
                    persister.Enqueue(widget);
                }
            }
        }

        private string InitializeTestFile(string filename, bool deleteExistingFile = true)
        {
            var extractPath = Path.Combine(testDataDirectory, filename);

            if (deleteExistingFile && File.Exists(extractPath))
            {
                File.Delete(extractPath);
            }

            return extractPath;
        }
    }
}