using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Compressors
{
    public interface ICompressor
    {
        string Name { get; }

        byte[] Compress(byte[] input);

        byte[] Decompress(byte[] input);
    }

    public static class CompressorTool
    {
        public const int BUFFER_SIZE = 0x1000;

        internal static void CopyStreamToStream(this Stream srcStream, Stream destStream)
        {
            CopyStreamToStream(srcStream, destStream, BUFFER_SIZE);
        }

        internal static void CopyStreamToStream(this Stream srcStream, Stream destStream, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            int readCount;

            while ((readCount = srcStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                destStream.Write(buffer, 0, readCount);
            }
        }

    }
}
