namespace LogShark.Shared.LogReading.Containers
{
    public class ReadLogLineResult
    {
        public int LineNumber { get; }
        public object LineContent { get; }

        public bool HasContent => LineContent != null; 

        public ReadLogLineResult(int lineNumber, object lineContent)
        {
            LineNumber = lineNumber;
            LineContent = lineContent;
        }
    }
}