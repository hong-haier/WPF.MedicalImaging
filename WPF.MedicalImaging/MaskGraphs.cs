using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WPF.MedicalImaging
{
    /// <summary>
    /// mask图形，主要表示3D模型在2D上的轮廓
    /// </summary>
    public class MaskGraphs
    {
        public MaskGraphs(string name, Color color, List<Point[]> paths)
        {
            this.Color = color;
            this.Paths = paths;
            this.ObjectName = name;
        }


        public string ObjectName { get; set; }

        public Color Color { get; set; }

        /// <summary>
        /// 封闭轮廓
        /// </summary>
        public List<Point[]> Paths { get; set; }
    }
}
