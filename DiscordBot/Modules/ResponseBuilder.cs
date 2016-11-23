using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Modules
{
    public class ResponseBuilder
    {
        public string Title;
        private readonly List<string> lines = new List<string>();

        public string Build()
        {
            var sb = new StringBuilder();

            const char invisCharacter = (char)8290; // Bypass trimming away first newline by using an invisible character not counted as whitespace.

            sb.AppendLine($"{invisCharacter}\n{Discord.Format.Bold(Title)}");

            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        public void AddLine(string line)
        {
            lines.Add(line);
        }

        public void AddTable(string col1Header, string col2Header, IEnumerable<Tuple<string, string>> data)
        {
            var sb = new StringBuilder();
            var lines = data.ToList();

            sb.Append("```");
            int longestLineLength = lines.Max(tuple => tuple.Item1.Length);

            if (col1Header.Length > longestLineLength)
                longestLineLength = col1Header.Length;

            sb.AppendLine(col1Header.PadLeft(longestLineLength) + " " + col2Header);

            foreach (var line in lines)
            {
                string col1 = line.Item1;
                string col2 = line.Item2;
                string row = col1.PadLeft(longestLineLength) + " " + col2;

                sb.AppendLine(row);
            }

            sb.Append("```");
            this.lines.Add(sb.ToString());
        }
    }
}