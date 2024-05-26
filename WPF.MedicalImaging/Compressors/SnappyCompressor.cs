using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Compressors
{

    public class SnappyCompressor : ICompressor
    {
        public string Name => "Snappy";

        public byte[] Compress(byte[] input)
        {
            var target = new Snappy.Sharp.SnappyCompressor();
            var result = new byte[target.MaxCompressedLength(input.Length)];
            var count = target.Compress(input, 0, input.Length, result);
            return result.Take(count).ToArray();
        }

        public byte[] Decompress(byte[] input)
        {
            var target = new Snappy.Sharp.SnappyDecompressor();
            var result = target.Decompress(input, 0, input.Length);
            return result;
        }
    }
}
