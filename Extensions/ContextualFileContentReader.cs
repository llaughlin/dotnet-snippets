using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Extensions
{
    public class ContextualFileContentReader
    {
        private readonly IFileSystemAccessor _FileSystemAccessor;
        private readonly ILogger _Log;

        private readonly ILoggerFactory _LoggerFactory;

        public ContextualFileContentReader(ILoggerFactory loggerFactory, IFileSystemAccessor fileSystemAccessor)
        {
            _LoggerFactory = loggerFactory;
            _FileSystemAccessor = fileSystemAccessor;
            _Log = loggerFactory.CreateLogger<FileContentObserver>();
        }

        public IEnumerable<LineWithContext> ReadFile(FileInfo file, int precedingLineCount, int followingLineCount)
        {
            return ReadLinesWithContext(ReadAllLogLinesFromFile(file), precedingLineCount, followingLineCount);
        }


        public IEnumerable<LineWithContext> ReadFile(FileInfo file, Regex lineRegex, int precedingLineCount,
            int followingLineCount)
        {
            return ReadLinesWithContext(ReadAllLogLinesFromFile(file), lineRegex, precedingLineCount,
                followingLineCount);
        }

        internal DateTimeOffset? TryParseDateTime(LineWithContext line)
        {
            var lineContext = new List<string>();
            lineContext.Add(line.Line.Content);
            lineContext.AddRange(line.PrecedingLines.Select(l => l.Content));
            lineContext.AddRange(line.FollowingLines.Select(l => l.Content));
            foreach (var l in lineContext)
            {
                var split = l.Split(' ');
                if (split.Length >= 2 &&
                    DateTimeOffset.TryParse($"{split[0]} {split[1]}".Replace(',', '.'), out var time)) return time;
            }

            //_Log.Verbose("Couldn't parse datetime from {Line}", lineContent);
            return null;
        }

        private IEnumerable<LineWithContext> ReadLinesWithContext(IEnumerable<LogLine> source, Regex lineRegex,
            int precedingLineCount, int followingLineCount)
        {
            _Log.LogTrace(
                "Creating context lines. Source Count: {Count} Regex: {Regex} PreviousLineCount: {PreviousLineCount} FollowingLineCount: {FollowingLineCount}",
                source.Count(), lineRegex, precedingLineCount, followingLineCount);

            foreach (var lines in source.SlidingWindow(precedingLineCount + 1 + followingLineCount))
            {
                if (lines.Count >= precedingLineCount)
                {
                    var errorLine = lines[precedingLineCount];
                    if (errorLine?.Content != null)
                    {
                        //_Log.LogTrace("Checking line {LineNumber}:{Line} against regex {Regex}", errorLine.LineNumber,errorLine.Content, lineRegex.ToString());
                        if (lineRegex?.IsMatch(errorLine.Content) ?? true)
                            yield return new LineWithContext(lines, precedingLineCount);
                    }
                }
            }
        }

        private IEnumerable<LogLine> ReadAllLogLinesFromFile(FileInfo file)
        {
            return _FileSystemAccessor.ReadAllFileLines(file.FullName).Select((l, i) => new LogLine(i + 1, l));
        }

        private IEnumerable<LineWithContext> ReadLinesWithContext(IEnumerable<LogLine> source, int precedingLineCount,
            int followingLineCount)
        {
            return ReadLinesWithContext(source, null, precedingLineCount, followingLineCount);
        }
    }


    public class LineWithContext
    {
        public LineWithContext(IList<LogLine> lines, int errorIndex)
        {
            PrecedingLines = lines.Take(errorIndex).ToList();
            Line = lines[errorIndex];
            FollowingLines = lines.Skip(errorIndex + 1).ToList();
        }

        public List<LogLine> FollowingLines { get; }

        public LogLine Line { get; }

        public List<LogLine> PrecedingLines { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var previousLine in PrecedingLines)
                sb.AppendLine($"{previousLine.LineNumber} - {previousLine.Content}");

            sb.AppendLine($"===== {Line.LineNumber} - {Line.Content}");

            foreach (var followingLine in FollowingLines)
                sb.AppendLine($"{followingLine.LineNumber} - {followingLine.Content}");

            return sb.ToString();
        }
    }


    public class LogLine
    {
        public LogLine(int lineNumber, string line)
        {
            LineNumber = lineNumber;
            Content = line;
        }

        public int LineNumber { get; }
        public string Content { get; }

        public override string ToString()
        {
            return $"{LineNumber} - {Content}";
        }
    }
}