using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Extensions
{
    public static class ColorExtensions
    {
        static Random random = new Random();

        public static System.Windows.Media.Color RandomColor()
        {
            return System.Windows.Media.Color.FromRgb(
                (byte)random.Next(0, 255), 
                (byte)random.Next(0, 255), 
                (byte)random.Next(0, 255));
        }
    }
}
