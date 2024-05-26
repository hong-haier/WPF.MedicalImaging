using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Compressors
{

    public class NoneCompressor : ICompressor
    {
        public string Name => "None";

        public byte[] Compress(byte[] input)
        {
            return input;
        }

        public byte[] Decompress(byte[] input)
        {
            return input;
        }
    }
}
