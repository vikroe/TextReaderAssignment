using System.Text;

namespace BigTextReader.Core.Text
{
    internal static class LineDecoder
    {
        internal static string DecodeByteLine(ReadOnlySpan<byte> line)
        {
            if (line.Length > 0 && line[^1] == (byte)'\r')
                line = line[..^1];
        
            if (line.Length > Globals.MaxRenderedLineLength)
            {
                string message = "...[line truncated at 256KB]";
                int cut = Globals.MaxRenderedLineLength - message.Length;
                while (cut > 0 && (line[cut] & 0xC0) == 0x80) cut--;
                return Encoding.UTF8.GetString(line[..cut]) + message;
            }

            return Encoding.UTF8.GetString(line);
        }
    }
}
