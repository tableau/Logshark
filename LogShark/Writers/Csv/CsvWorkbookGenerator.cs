using System.Collections.Generic;
using System.Linq;
using LogShark.Containers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers.Csv
{
    public class CsvWorkbookGenerator : IWorkbookGenerator
    {
        private readonly ILogger _logger;

        public bool GeneratesWorkbooks => false;

        public CsvWorkbookGenerator(ILogger logger)
        {
            _logger = logger;
        }

        public WorkbookGeneratorResults CompleteWorkbooksWithResults(WritersStatistics writersStatistics)
        {
            // TODO - do something here if we decide to use CSV files for workbooks
            return new WorkbookGeneratorResults(null, null);
        }
    }
}