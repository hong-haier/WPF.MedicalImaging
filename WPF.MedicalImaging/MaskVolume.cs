using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WPF.MedicalImaging
{
    public class MaskVolume : IMaskVolume
    {
        public bool ReverseXAxisOnRenderImage { get; set; } = true;

        public IDicomVolume DicomVolume { get; private set; }

        public ObservableCollection<MaskObject> Masks { get; set; }

        public MaskVolume(IDicomVolume dicomVolume)
        {
            this.DicomVolume = dicomVolume;
            this.Masks = new ObservableCollection<MaskObject>();
        }

        public List<MaskGraphs> GetAxialMaskGraphs(int page)
        {
            List<MaskGraphs> graphs = new List<MaskGraphs>();
            foreach (var mask in this.Masks)
            {
                graphs.AddRange(mask.GetAxialGraphs(page));
            }
            return graphs;
        }

        const double DPI = 96;
        public List<WriteableBitmap> GetAxialMaskBitmap(int page)
        {
            if (this.Masks.Count == 0) return null;
            int width = this.DicomVolume.Columns;
            int height = this.DicomVolume.Rows;

            List<WriteableBitmap> images = new List<WriteableBitmap>();
            foreach (var mask in this.Masks)
            {
                WriteableBitmap bitmap = new WriteableBitmap(width, height, DPI, DPI,
                    System.Windows.Media.PixelFormats.Bgra32, null);
                byte b = mask.Color.B;
                byte g = mask.Color.G;
                byte r = mask.Color.R;
                unsafe
                {
                    var bytes = (byte*)bitmap.BackBuffer.ToPointer();
                    bitmap.Lock();
                    //for (int row = 0; row < height; row++)
                    Parallel.For(0, height, row =>
                    {
                        var pixel = mask.GetAxialPixelValues(page, row, width);
                        if (pixel != null)
                        {
                            if (ReverseXAxisOnRenderImage) pixel = pixel.Reverse().ToArray(); // X轴翻转
                            for (int x = 0; x < width; x++)
                            {
                                int i0 = (row * width + x) * 4;
                                if (pixel[x] == 1)
                                {
                                    bytes[i0] = b;
                                    bytes[i0 + 1] = g;
                                    bytes[i0 + 2] = r;
                                    bytes[i0 + 3] = 64;
                                }
                            }
                        }
                    });
                    bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                    bitmap.Unlock();
                }
                images.Add(bitmap);
            }

            return images;
        }

        RenderTargetBitmap AxialMaskImage;
        /// <summary>
        /// 使用RenderTargetBitmap，性能相对较差，GPU利用率低
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public BitmapSource GetAxialMaskBitmap2(int page)
        {
            if (this.Masks.Count == 0) return null;
            int width = this.DicomVolume.Columns;
            int height = this.DicomVolume.Rows;
            if (AxialMaskImage == null)
            {
                AxialMaskImage = new RenderTargetBitmap(width, height, DPI, DPI,
                    System.Windows.Media.PixelFormats.Pbgra32);
            }
            DrawingVisual drawingVisual = new DrawingVisual();
            {
                DrawingContext dc = drawingVisual.RenderOpen();
                foreach (var mask in this.Masks)
                {
                    int[] offset = mask.Offset;
                    int[] dim = mask.Dimensions;
                    int w0 = dim[0];
                    int h0 = dim[1];
                    if (page < offset[2] && page >= offset[2] + dim[2]) continue;
                    WriteableBitmap bitmap = new WriteableBitmap(w0, h0, DPI, DPI, PixelFormats.Pbgra32, null);
                    unsafe
                    {
                        var bytes = (byte*)bitmap.BackBuffer.ToPointer();
                        bitmap.Lock();
                        Parallel.For(0, h0, row =>
                        //for (int row = 0; row < h0; row++)
                        {
                            var pixel = mask.GetAxialPixelValues(page, row + offset[1]);
                            if (pixel != null)
                            {
                                pixel = pixel.Reverse().ToArray(); // X轴翻转1
                                for (int x = 0; x < w0; x++)
                                {
                                    int i0 = (row * w0 + x) * 4;
                                    if (pixel[x] == 1)
                                    {
                                        bytes[i0] = mask.Color.B;
                                        bytes[i0 + 1] = mask.Color.G;
                                        bytes[i0 + 2] = mask.Color.R;
                                        bytes[i0 + 3] = 80;
                                    }
                                }
                            }
                        });
                        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                        bitmap.Unlock();
                    }
                    dc.DrawImage(bitmap, new Rect(width - offset[0] - w0, offset[1], w0, h0)); // X轴翻转2
                }
                dc.Close();
            }
            AxialMaskImage.Clear();
            AxialMaskImage.Render(drawingVisual);
            return AxialMaskImage;
        }
    }
}
