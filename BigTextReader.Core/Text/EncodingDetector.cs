using System.Text;

namespace BigTextReader.Core.Text
{
    public static class EncodingDetector
    {
        public readonly record struct BomInfo(int Length, bool Supported);
        public static BomInfo Detect(ReadOnlySpan<byte> head) => head switch
        {
            [0x00, 0x00, 0xFE, 0xFF, ..] => new(4, false),
            [0xFF, 0xFE, 0x00, 0x00, ..] => new(4, false),
            [0xEF, 0xBB, 0xBF, ..] => new(3, true),
            [0xFE, 0xFF, ..] => new(2, false),
            [0xFF, 0xFE, ..] => new(2, false),
            _ => new(0, true),
        };
    }
}
