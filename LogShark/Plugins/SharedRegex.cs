using System.Text.RegularExpressions;

namespace LogShark.Plugins
{
    public static class SharedRegex
    {
        // 2024.2 added optional "pid" to match 2024.2, and made ts_offset more exclusionary
        public static readonly Regex JavaLogLineRegex = new Regex(@"^
            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
            (?<ts_offset>[-|+]\d+)\s
            ?(?<pid>\d+)?\s
            \((?<site>[^,]*?), (?<user>[^,]*?), (?<sess>[^,]*?), (?<req>[^,\)]*?) (,(?<local_req_id>[^\)]*?))?\)\s
            (?<thread>[^\s]*?)\s
             (?<service>[^:]*?)?:\s
            (?<sev>[A-Z]+)(\s+)
            (?<class>[^\s]*?)\s-\s
            (?<message>(.|\n)*)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
    }
}