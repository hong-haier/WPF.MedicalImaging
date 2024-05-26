using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPF.MedicalImaging
{
    public abstract class MaskObject
    {
        protected virtual byte[] DataArray { get; set; }

        /// <summary>
        /// 像素间距
        /// </summary>
        public virtual double[] Spacing { get; protected set; } = new double[3] { 1, 1, 1 };

        /// <summary>
        /// 三围大小（长、宽、高）
        /// </summary>
        public virtual int[] Dimensions { get; protected set; } = new int[3] { 0, 0, 0 };

        /// <summary>
        /// 像素偏移量
        /// </summary>
        public virtual int[] Offset { get; protected set; } = new int[3] { 0, 0, 0 };

        /// <summary>
        /// 方向
        /// </summary>
        public virtual double[] Direction { get; protected set; } = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

        public virtual void SetOffset(int[] offset) { this.Offset = offset; }

        public virtual void DetDimensions(int[] dimensions) { this.Dimensions = dimensions; }

        public Color Color { get; set; }

        public virtual string ObjectType { get; set; }

        public virtual string ObjectName { get; set; }

        public virtual IList<MaskGraphs> GetAxialGraphs(int page) { return null; }

        public virtual byte GetPixelValue(int x, int y, int z)
        {
            int i = GetPixelIndex(x, y, z);
            return DataArray[i];
        }

        public virtual void SetPixelValue(int x, int y, int z, byte v)
        {
            if (x < Offset[0] || x >= Offset[0] + Dimensions[0]) throw new ArgumentException("需要扩容");
            if (y < Offset[1] || y >= Offset[1] + Dimensions[1]) throw new ArgumentException("需要扩容");
            if (z < Offset[2] || z >= Offset[2] + Dimensions[2]) throw new ArgumentException("需要扩容");
            int i = GetPixelIndex(x, y, z);
            DataArray[i] = v;
        }

        protected virtual int GetPixelIndex(int x, int y, int z)
        {
            return
                (z - Offset[2]) * Dimensions[1] * Dimensions[0] +
                (y - Offset[1]) * Dimensions[0] +
                (x - Offset[0]);
        }

        public virtual byte[] GetAxialPixelValues(int page, int row, int size)
        {
            if (page < Offset[2] || page >= Offset[2] + Dimensions[2]) return null;
            if (row < Offset[1] || row >= Offset[1] + Dimensions[1]) return null;
            byte[] values = new byte[size];
            int x = this.Offset[0];
            int y = row;
            int z = page;
            int i = GetPixelIndex(x, y, z);
            Array.Copy(DataArray, i, values, x, Dimensions[0]);
            return values;
        }

        public virtual byte[] GetAxialPixelValues(int page, int row)
        {
            if (page < Offset[2] || page >= Offset[2] + Dimensions[2]) return null;
            if (row < Offset[1] || row >= Offset[1] + Dimensions[1]) return null;
            int size = Dimensions[0];
            byte[] values = new byte[size];
            int x = this.Offset[0];
            int y = row;
            int z = page;
            int i = GetPixelIndex(x, y, z);
            Array.Copy(DataArray, i, values, 0, size);
            return values;
        }

        public virtual void WriteData(byte[] data, int startPosition)
        {
            if (data == null || data.Length == 0) return;
            Array.Copy(data, 0, this.DataArray, startPosition, data.Length);
        }
    }
}
