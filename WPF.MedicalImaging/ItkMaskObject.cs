using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using itk.simple;
using WPF.MedicalImaging.Extensions;

namespace WPF.MedicalImaging
{
    public class ItkMaskObject : MaskObject
    {
        /// <summary>
        /// 使用ITK解析mhd文件，仅支持生成2D的mask，不支持3D渲染（如若要支持3D渲染，可使用vtk实现MaskObject对象
        /// </summary>
        /// <param name="mhdFile"></param>
        /// <exception cref="NotSupportedException"></exception>
        public ItkMaskObject(string mhdFile)
        {
            ImageFileReader reader = new ImageFileReader();
            reader.SetFileName(mhdFile);
            reader.ReadImageInformation();
            var image = reader.Execute();
            if (reader.GetDimension() != 3) throw new NotSupportedException("仅支持三维空间模型");

            ulong[] size = reader.GetSize().ToArray();
            this.Dimensions[0] = (int)size[0];
            this.Dimensions[1] = (int)size[1];
            this.Dimensions[2] = (int)size[2];
            reader.GetSpacing().CopyTo(this.Spacing);
            reader.GetDirection().CopyTo(this.Direction);
            double[] origin = reader.GetOrigin().ToArray();
            this.Offset[0] = (int)Math.Round(origin[0] / Spacing[0]);
            this.Offset[1] = (int)Math.Round(origin[1] / Spacing[1]);
            this.Offset[2] = (int)Math.Round(origin[2] / Spacing[2]);
            this.DataArray = image.ToCSharpArray().ToByteArray();
            image.Dispose();
            reader.Dispose();

            var color = ColorExtensions.RandomColor();
            this.Color = System.Windows.Media.Color.FromArgb(64, color.R, color.G, color.B);
        }

        public ItkMaskObject(int[] dims, double[] spacing)
        {
            this.Dimensions = dims;
            this.Spacing = spacing;
            int dataLength = Dimensions.Product();
            this.DataArray = new byte[dataLength];
        }

    }
}
