using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Shared.LogReading.Readers
{
    public class SimpleLinePerLineReader : ILogReader
    {
        private readonly Stream _stream;


        public SimpleLinePerLineReader(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            int maxLineLength = 100000;
            int lineNumber = 0;
            bool incompleteline = false;
            StringBuilder line = new StringBuilder(maxLineLength);
            using (var reader = new StreamReader(_stream))
            {
                int i;
                while ((i = reader.Read()) > 0)
                {
                    char c = (char)i;
                    if (c == '\n')
                    {
                        if (line != null)
                        {
                            ++lineNumber;
                            if (incompleteline == true)
                            {
                                incompleteline = false; //reset the incomplete line as it reached the end of an incomplete line
                            }
                            else
                            {
                                yield return new ReadLogLineResult(lineNumber, line.ToString().Trim('\r'));
                            }
                        }
                        line.Length = 0;
                        continue;
                    }
                    

                    line.Append((char)c);
                    if (line.Length > maxLineLength)
                    {
                        
                        if (incompleteline == false)
                        {
                            ++lineNumber;
                            yield return new ReadLogLineResult(lineNumber, String.Concat(line.ToString().Trim('\r'), "truncated>\""));
                        }
                        //clearing buffer and continuing
                        reader.DiscardBufferedData();
                        line.Length = 0;
                        incompleteline = true;

                    }


                }
                if (reader.EndOfStream)
                {//last line has no end character and still can have some content
                    ++lineNumber;
                    if (line.Length > 1) { //not picking blank characters, has to be more than 1 character
                    yield return new ReadLogLineResult(lineNumber, line.ToString().Trim('\r'));
                }
                }
            }
        }
    }
}