using System.Text;

namespace GscImporter.Infrastructure.Exports;

internal static class SimpleCsvReader
{
    public static IReadOnlyList<IReadOnlyList<string>> Read(TextReader reader)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var insideQuotes = false;

        int character;
        while ((character = reader.Read()) != -1)
        {
            if (insideQuotes)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); field.Append('"'); }
                    else insideQuotes = false;
                }
                else field.Append((char)character);
                continue;
            }

            switch (character)
            {
                case '"': insideQuotes = true; break;
                case ',': row.Add(field.ToString()); field.Clear(); break;
                case '\r':
                    if (reader.Peek() == '\n') reader.Read();
                    CompleteRow();
                    break;
                case '\n': CompleteRow(); break;
                default: field.Append((char)character); break;
            }
        }

        if (insideQuotes) throw new InvalidDataException("The CSV file contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) CompleteRow();
        return rows;

        void CompleteRow()
        {
            row.Add(field.ToString());
            field.Clear();
            if (row.Any(value => value.Length > 0)) rows.Add(row.ToArray());
            row = [];
        }
    }
}
