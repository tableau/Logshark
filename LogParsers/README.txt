************************************
***** Parser Development Guide *****
************************************

-- General Class Design --

The ParserFactory delegates to ParserBuilders which instantiate Parsers.

	* ParserFactory - This is the main entry point for an outside class to retrieve a parser for a given file.  It contains a mapping of directories within the log structure that it uses to delegate the actual parser instantiation to the correct ParserBuilder.
	* ParserBuilder - An internal class used to map the types of files encountered in a single logfile directory to their associated parsers.  Also maintains blacklist information used by the ParserFactory to determine whether a log file is supported or not.
	* Parser - The main worker horse that contains all of the logic for parsing JSON log documents out of a specific log file type.  Most of the heavy lifting is currently done by the various Abstract classes, so the actual implementation classes are small and
			   tend to just contain Regex mappings.

-- How To Add New Parsers --

	* Create Parser class stub

	Start by creating a new Parser class that derives from BaseParser (or one of the Abstract Parser classes, if they suit your needs).  Your stub should also explicitly implement the IParser interface.

	* Create supporting unit test(s)

	A TDD approach to parser development speeds things up.  Start by developing a simple unit test to read a sample log line and parse it into a log document using the parser class stub you just created.

	* Implement parser logic

	Back in your new parser class, you'll need to implement the ParseLogDocument method as well as the CollectionSchema & FinishedParsing flags.  ParseLogDocument should consume a line from the reader and flag FinishedParsing wherever appropriate.

	* Test!!

	Refine parser implementation until it passes your tests.  You should also test it on actual logfiles, which can be pretty dirty.
	
	* Add parser to ParserBuilder or create new ParserBuilder

	When your parser is ready to be run against full log sets, you'll need to link it up in a ParserBuilder.  The ParserBuilder contains a regex file name entry between a log filename and the parser type.  Find the ParserBuilder for the correct 
	directory and add an entry for your new parser.  If no ParserBuilder exists for the directory containing the logfile, you'll need to create one using the existing ParserBuilders as a template and then register it in the ParserFactory directoryMapping.

	* Celebrate!

-- Best Practices --

	* Anchor Regex to the start of the line using "^" wherever possible.
	* Minimize amount of Regex lookaround/backtracking.
	* Keep Regex as simple as possible, and take advantage of the IgnorePatternWhiteSpace flag to keep long Regex strings humanly readable in code.  Well, at least sort-of humanly readable.
	* Derive from the BaseParser if possible, it contains some handy functionality for managing metadata.
