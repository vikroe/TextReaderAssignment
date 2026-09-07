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
                int cut = Globals.MaxRenderedLineLength;
                while (cut > 0 && (line[cut] & 0xC0) == 0x80) cut--;
                line = line[..cut];
            }

            return Encoding.UTF8.GetString(line);
        }
    }
}
