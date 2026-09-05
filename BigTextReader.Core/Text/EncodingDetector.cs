using System.Text;

namespace BigTextReader.Core.Text
{
    public static class EncodingDetector
    {
        public readonly record struct BomInfo(Encoding Encoding, int Length, bool Supported);
        public static BomInfo Detect(ReadOnlySpan<byte> head) => head switch
        {
            [0x00, 0x00, 0xFE, 0xFF, ..] => new(new UTF32Encoding(true, true), 4, false),
            [0xFF, 0xFE, 0x00, 0x00, ..] => new(new UTF32Encoding(false, true), 4, false),
            [0xEF, 0xBB, 0xBF, ..] => new(new UTF8Encoding(true), 3, true),
            [0xFE, 0xFF, ..] => new(Encoding.BigEndianUnicode, 2, false),
            [0xFF, 0xFE, ..] => new(Encoding.Unicode, 2, false),
            _ => new(new UTF8Encoding(false), 0, true),
        };
    }
}
