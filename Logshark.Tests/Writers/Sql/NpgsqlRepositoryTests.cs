using LogShark.Writers.Sql;
using LogShark.Writers.Sql.Connections;
using LogShark.Writers.Sql.Connections.Npgsql;
using Moq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace LogShark.Tests.Writers.Sql
{
    public class NpgsqlRepositoryTests : InvariantCultureTestsBase
    {
        private const string _expectedSchema = "public";
        private const string _dummyColumnName = "ego_superego_id";
        private const string _testDatabaseName = "TestDatabase";
        private const int _insertBatchSize = 100;

        [Fact]
        public async Task VerifyStatementsForCreateDatabase()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            await repo.CreateDatabaseIfNotExist();

            VerifyInvocationOfExecuteScalarWithoutDatabase<bool>(
                mockContext, 
                $@"SELECT EXISTS
                (
                    SELECT datname
                    FROM pg_catalog.pg_database
                    WHERE datname = '{_testDatabaseName}'
                );");

            VerifyInvocationOfExecuteNonQueryWithoutDatabase(
                mockContext, 
                $@"CREATE DATABASE ""{_testDatabaseName}"" ENCODING 'UTF8'");
        }

        [Fact]
        public void VerifyExceptionForCreateColumnsIfTypeNotRegistered()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);

            ((Func<Task>)(async () => await repo.CreateColumnsForTypeIfNotExist<TestModel>()))
                .Should()
                .Throw<Exception>()
                .WithMessage($@"TypeProjection for type '{typeof(TestModel).FullName}' has not been generated. Call 'GenerateTypeProjection' first.");
        }

        [Fact]
        public async Task VerifyStatementsForCreateSchema()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            await repo.CreateSchemaIfNotExist<TestModel>();

            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"CREATE SCHEMA IF NOT EXISTS ""{_expectedSchema}"";");
        }

        [Fact]
        public async Task VerifyStatementsForCreateTable()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            await repo.CreateTableIfNotExist<TestModel>();

            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"CREATE TABLE IF NOT EXISTS ""{_expectedSchema}"".""{nameof(TestModel)}""();");
        }

        [Fact]
        public async Task VerifyStatementsForCreateColumns()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            await repo.CreateColumnsForTypeIfNotExist<TestModel>();

            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""Birthday"" timestamp;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""MoleCount"" bigint;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""Name"" text;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""Points"" integer;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""Registered"" boolean;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""SubpixelPosition"" numeric;");
            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""Weight"" double precision;");
        }

        [Fact]
        public async Task VerifyStatementsForCreateColumnWithPrimaryKey()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            await repo.CreateColumnWithPrimaryKeyIfNotExist<TestModel>(_dummyColumnName);

            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""{_dummyColumnName}"" SERIAL PRIMARY KEY;");
        }

        [Fact]
        public async Task VerifyStatementsForCreateColumnWithForeignKey()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            repo.RegisterType<TestForForeignKeyModel>(null);
            const string dummyTargetColumnName = "dummy_id";
            await repo.CreateColumnWithForeignKeyIfNotExist<TestModel, TestForForeignKeyModel>(_dummyColumnName, dummyTargetColumnName);

            VerifyInvocationOfExecuteNonQuery(
                mockContext,
                $@"ALTER TABLE ""{_expectedSchema}"".""{nameof(TestModel)}""
                ADD COLUMN ""{_dummyColumnName}"" INTEGER
                REFERENCES ""{_expectedSchema}"".""{nameof(TestForForeignKeyModel)}""(""{dummyTargetColumnName}"");");
        }

        [Fact]
        public async Task VerifyStatementsForInsertRow()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);

            await repo.InsertRow(
                new TestModel()
                {
                    Birthday = new DateTime(2020, 1, 1),
                    MoleCount = 10,
                    Name = "Bob",
                    Points = 20,
                    Registered = true,
                    SubpixelPosition = 30.5m,
                    Weight = 40
                },
                new Dictionary<string, object>() { { "DummyColumn", "DummyValue" } });
            await repo.Flush();

            VerifyInvocationOfExecuteNonQueryWithParamsTrimmed(mockContext,
                $@"INSERT INTO ""{_expectedSchema}"".""{nameof(TestModel)}"" 
                (""Birthday"", ""MoleCount"", ""Name"", ""Points"", ""Registered"", ""SubpixelPosition"", ""Weight"", ""DummyColumn"") 
                VALUES (@Birthday, @MoleCount, @Name, @Points, @Registered, @SubpixelPosition, @Weight, @DummyColumn)",
                new Dictionary<string, object>()
                {
                    { "Birthday", new DateTime(2020, 1, 1) },
                    { "MoleCount", 10 },
                    { "Name", "Bob" },
                    { "Points", 20 },
                    { "Registered", true },
                    { "SubpixelPosition", 30.5m },
                    { "Weight", 40 },
                    { "DummyColumn", "DummyValue" }
                });
        }

        [Fact]
        public async Task VerifyStatementsForInsertRowWithReturnValue()
        {
            var mockContext = GetMockContext();
            var repo = new NpgsqlRepository(mockContext.Object, _insertBatchSize);
            repo.RegisterType<TestModel>(null);
            const string fakeColumnName = "FakeColumnNameToReturn";

            var dummyReturnValue = await repo.InsertRowWithReturnValue< TestModel, int>(
                new TestModel()
                {
                    Birthday = new DateTime(2020, 1, 1),
                    MoleCount = 10,
                    Name = "Bob",
                    Points = 20,
                    Registered = true,
                    SubpixelPosition = 30.5m,
                    Weight = 40
                },
                fakeColumnName,
                new Dictionary<string, object>() { { "DummyColumn", "DummyValue" } });
            await repo.Flush();

            VerifyInvocationOfExecuteScalarWithParams<int>(mockContext,
                $@"INSERT INTO ""{_expectedSchema}"".""{nameof(TestModel)}"" 
                (""Birthday"", ""MoleCount"", ""Name"", ""Points"", ""Registered"", ""SubpixelPosition"", ""Weight"", ""DummyColumn"") 
                VALUES (@Birthday, @MoleCount, @Name, @Points, @Registered, @SubpixelPosition, @Weight, @DummyColumn)
                RETURNING ""{fakeColumnName}"";",
                new Dictionary<string, object>()
                {
                    { "Birthday", new DateTime(2020, 1, 1) },
                    { "MoleCount", 10 },
                    { "Name", "Bob" },
                    { "Points", 20 },
                    { "Registered", true },
                    { "SubpixelPosition", 30.5m },
                    { "Weight", 40 },
                    { "DummyColumn", "DummyValue" }
                });
        }

        public class TestModel
        {
            public DateTime Birthday { get; set; }
            public Int64 MoleCount { get; set; }
            public string Name { get; set; }
            public int Points { get; set; }
            public bool Registered { get; set; }
            public decimal SubpixelPosition { get; set; }
            public double Weight { get; set; }
        }

        public class TestForForeignKeyModel
        {

        }

        private Mock<INpgsqlDataContext> GetMockContext()
        {
            var mockContext = new Mock<INpgsqlDataContext>();
            mockContext.SetupGet(x => x.DatabaseName).Returns(_testDatabaseName);
            return mockContext;
        }

        private void VerifyInvocationOfExecuteScalarWithoutDatabase<T>(Mock<INpgsqlDataContext> mock, string statement)
        {
            mock.Verify(m => m.ExecuteScalarToServiceDatabase<T>(
                It.Is<string>(callParam => CompressWhiteSpace(callParam) == CompressWhiteSpace(statement)),
                null, 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>()));
        }

        private void VerifyInvocationOfExecuteNonQueryWithoutDatabase(Mock<INpgsqlDataContext> mock, string statement)
        {
            mock.Verify(m => m.ExecuteNonQueryToServiceDatabase(
                It.Is<string>(callParam => CompressWhiteSpace(callParam) == CompressWhiteSpace(statement)),
                null, 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>()));
        }

        private void VerifyInvocationOfExecuteNonQuery(Mock<INpgsqlDataContext> mock, string statement)
        {
            mock.Verify(m => m.ExecuteNonQuery(
                It.Is<string>(callParam => CompressWhiteSpace(callParam) == CompressWhiteSpace(statement)), 
                null, 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>()));
        }

        private void VerifyInvocationOfExecuteNonQueryWithParamsTrimmed(Mock<INpgsqlDataContext> mock, string statement, Dictionary<string, object> expectedParams)
        {
            mock.Verify(m => m.ExecuteNonQuery(
                It.Is<string>(callParam => TrimRandomizedParameterSuffix(CompressWhiteSpace(callParam)) == CompressWhiteSpace(statement)),
                It.Is<Dictionary<string,object>>(callParam => 
                    callParam.Count == expectedParams.Count &&
                    expectedParams.All(expectedParam => expectedParam.Value.ToString() == callParam[callParam.Keys.First(k => k.StartsWith(expectedParam.Key))].ToString())),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()));
        }

        private void VerifyInvocationOfExecuteScalarWithParams<T>(Mock<INpgsqlDataContext> mock, string statement, Dictionary<string, object> expectedParams)
        {
            mock.Verify(m => m.ExecuteScalar<T>(
                It.Is<string>(callParam => CompressWhiteSpace(callParam) == CompressWhiteSpace(statement)),
                It.Is<Dictionary<string, object>>(callParam =>
                     callParam.Count == expectedParams.Count &&
                     expectedParams.All(expectedParam => expectedParam.Value.ToString() == callParam[callParam.Keys.First(k => k.StartsWith(expectedParam.Key))].ToString())),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()));
        }

        private string CompressWhiteSpace(string input)
        {
            return Regex.Replace(input, @"\s+", " ").Trim();
        }

        private string TrimRandomizedParameterSuffix(string input)
        {
            return Regex.Replace(input, @"(@[a-zA-Z0-9]+)(_[a-zA-Z0-9]+)*(_[a-zA-Z0-9]+)", "$1$2");
        }
    }
}
