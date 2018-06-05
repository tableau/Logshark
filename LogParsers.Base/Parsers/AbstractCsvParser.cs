using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json.Linq;
using System.IO;

namespace LogParsers.Base.Parsers
{
    public abstract class AbstractCsvParser : BaseParser
    {
        protected CsvReader csvReader;

        public override bool IsMultiLineLogType { get { return false; } }

        protected override bool UseLineNumbers { get { return true; } }

        protected AbstractCsvParser()
        {
        }

        protected AbstractCsvParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        public override JObject ParseLogDocument(TextReader textReader)
        {
            // Initialize only once.
            if (csvReader == null)
            {
                CsvConfiguration csvConfiguration = GetCsvConfiguration();
                csvReader = new CsvReader(textReader, csvConfiguration);
            }

            // Advance reader and check if we're done.
            if (!csvReader.Read())
            {
                FinishedParsing = true;
                return null;
            }
            if (UseLineNumbers)
            {
                LineCounter.CurrentValue = csvReader.Row;
            }

            JObject record = ParseRecord();
            return InsertMetadata(record);
        }

        protected abstract CsvConfiguration GetCsvConfiguration();

        protected abstract JObject ParseRecord();
    }
}
