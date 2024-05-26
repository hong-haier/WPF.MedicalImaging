using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Compressors
{

    public class GZipCompressor : ICompressor
    {
        public string Name => "GZip";

        public byte[] Compress(byte[] input)
        {
            if (input == null || input.Length == 0) return new byte[0];

            byte[] output;
            using (var outStream = new MemoryStream(input.Length))
            {
                using (var gzip = new GZipStream(outStream, CompressionMode.Compress))
                {
                    gzip.Write(input, 0, input.Length);
                }
                output = outStream.ToArray();
            }
            return output;
        }

        public byte[] Decompress(byte[] input)
        {
            if (input == null || input.Length == 0) return new byte[0];

            byte[] output;
            var outStream = new MemoryStream(input.Length * 2);
            try
            {
                using (var inStream = new MemoryStream(input))
                using (var gzip = new GZipStream(inStream, CompressionMode.Decompress))
                {
                    gzip.CopyStreamToStream(outStream);
                }
                output = outStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("GZIP解压过程中发生了意外。", ex);
            }
            finally
            {
                outStream.Close();
            }
            return output;
        }

    }
}
