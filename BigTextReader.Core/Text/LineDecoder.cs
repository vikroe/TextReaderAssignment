using BigTextReader.Core.Search;
using System.Text;

namespace BigTextReader.Core.Text
{
    internal static class LineDecoder
    {
        private const string Message = "...[line truncated at 256KB]";

        internal static string DecodeByteLine(ReadOnlySpan<byte> line)
        {
            if (line.Length > 0 && line[^1] == (byte)'\r')
                line = line[..^1];
        
            if (line.Length > Globals.MaxRenderedLineLength)
            {
                int cut = Globals.MaxRenderedLineLength - Message.Length;
                while (cut > 0 && (line[cut] & 0xC0) == 0x80) cut--;
                return Encoding.UTF8.GetString(line[..cut]) + Message;
            }

            return Encoding.UTF8.GetString(line);
        }

        internal static int DecodeLineUpTo(
            ReadOnlySpan<byte> line,
            long target,
            int startOffset = 0,
            int startChars = 0)
        {
            if (target > Globals.MaxRenderedLineLength) return SearchHit.UnknownColumn;

            int parsed = startOffset, noChars = startChars;
            while (parsed < target)
            {
                if ((line[parsed] & 0xC0) != 0x80) noChars++;
                parsed++;
            }

            return noChars;
        }
    }
}
