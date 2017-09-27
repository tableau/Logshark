using CsvHelper.Configuration;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.Parsers
{
    public sealed class PostgresParser : AbstractCsvParser, IParser
    {
        private static readonly string collectionName = ParserConstants.PgSqlCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "file", "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        /// <summary>
        /// Ordinal mapping to the contents of a Postgres CSV record.
        /// </summary>
        private class PostgresCsvMapping
        {
            [JsonProperty(PropertyName = "ts")]
            public DateTime Timestamp { get; set; }

            [JsonProperty(PropertyName = "user_name")]
            public string Username { get; set; }

            [JsonProperty(PropertyName = "database_name")]
            public string DatabaseName { get; set; }

            [JsonProperty(PropertyName = "pid")]
            public int Pid { get; set; }

            [JsonProperty(PropertyName = "client")]
            public string Client { get; set; }

            [JsonProperty(PropertyName = "session_id")]
            public string SessionId { get; set; }

            [JsonProperty(PropertyName = "per_session_line_number")]
            public string PerSessionLineNumber { get; set; }

            [JsonProperty(PropertyName = "command_tag")]
            public string CommandTag { get; set; }

            [JsonProperty(PropertyName = "session_start_time")]
            public string SessionStartTime { get; set; }

            [JsonProperty(PropertyName = "virtual_transaction_id")]
            public string VirtualTransactionId { get; set; }

            [JsonProperty(PropertyName = "regular_transaction_id")]
            public string RegularTransactionId { get; set; }

            [JsonProperty(PropertyName = "sev")]
            public string Sev { get; set; }

            [JsonProperty(PropertyName = "sqlstate_code")]
            public string SqlstateCode { get; set; }

            [JsonProperty(PropertyName = "message")]
            public string Message { get; set; }

            [JsonProperty(PropertyName = "message_detail")]
            public string MessageDetail { get; set; }

            [JsonProperty(PropertyName = "hint")]
            public string Hint { get; set; }

            [JsonProperty(PropertyName = "internal_query_led_to_error")]
            public string InternalQueryLedToError { get; set; }

            [JsonProperty(PropertyName = "internal_query_error_position")]
            public string InternalQueryErrorPosition { get; set; }

            [JsonProperty(PropertyName = "error_context")]
            public string ErrorContext { get; set; }

            [JsonProperty(PropertyName = "user_query_led_to_error")]
            public string UserQueryLedToError { get; set; }

            [JsonProperty(PropertyName = "user_query_error_position")]
            public string UserQueryErrorPosition { get; set; }

            [JsonProperty(PropertyName = "error_location_in_postgres_source")]
            public string ErrorLocationInPostgresSource { get; set; }

            [JsonProperty(PropertyName = "application_name")]
            public string ApplicationName { get; set; }
        }

        public override CollectionSchema CollectionSchema
        {
            get { return collectionSchema; }
        }

        protected override CsvConfiguration GetCsvConfiguration()
        {
            return new CsvConfiguration { HasHeaderRecord = false };
        }

        protected override JObject ParseRecord()
        {
            object record = csvReader.GetRecord(typeof(PostgresCsvMapping));

            return JObject.FromObject(record);
        }

        public PostgresParser()
        {
        }

        public PostgresParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}