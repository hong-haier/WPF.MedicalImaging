using FellowOakDicom.Imaging.LUT;
using FellowOakDicom.Imaging.Mathematics;
using FellowOakDicom.Imaging.Reconstruction;
using FellowOakDicom.Imaging.Render;
using FellowOakDicom.Imaging;
using FellowOakDicom.Tools;
using FellowOakDicom;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using WPF.MedicalImaging.Compressors;
using System.Runtime.Caching;
using WPF.MedicalImaging.Extensions;

namespace WPF.MedicalImaging
{
    public class DicomVolume : IDicomVolume
    {
        private readonly List<ImageData> _slices;
        private double[] _sortOrders;

        private Vector3D _slicesNormal;
        private double _maxSliceSpace;
        private double _minSliceSpace;
        public IntervalD SliceSpaces => new IntervalD(_minSliceSpace, _maxSliceSpace);

        public Point3D BoundingMin { get; private set; }
        public Point3D BoundingMax { get; private set; }

        public double PixelSpacingInSource => _slices?.FirstOrDefault()?.Geometry.PixelSpacingBetweenColumns ?? 0;

        private double? _sliceThicknessInCoronal;
        public double SliceThicknessInCoronal => _sliceThicknessInCoronal.HasValue ? _sliceThicknessInCoronal.Value : PixelSpacingBetweenRowsInSource;

        private double? _sliceThicknessInSagittal;
        public double SliceThicknessInSagittal => _sliceThicknessInSagittal.HasValue ? _sliceThicknessInSagittal.Value : PixelSpacingBetweenColumnsInSource;

        private readonly Lazy<DicomDataset> _commonData;
        public DicomDataset CommonData => _commonData.Value;

        public DicomVolume(IEnumerable<ImageData> slices)
        {
            slices = new List<ImageData>(slices
                    .Where(s => s != null) // only use valid slices
                    .Where(s => s.FrameOfReferenceUID != null) // remove all slices without geometry data (presentation states, luts, ..)
                    );

            // validate data
            ValidateInput(slices.Select(s => s.FrameOfReferenceUID).Distinct().Count() == 1, "The images are mixed up from different stacks");

            _slices = slices.GroupBy(s => s.Orientation).OrderBy(g => g.Count()).Last().ToList();
            ValidateInput(slices.Count() > 5, "There are too few images for reconstruction");

            // TODO: check for each having the same normal vector
            _commonData = new Lazy<DicomDataset>(GetCommonData);
            _pixelDataRange = new Lazy<DicomRange<double>>(GetPixelDataRange);
            BuildVolumeData();
        }

        public DicomVolume(IEnumerable<ImageData> slices, double sliceThicknessInCoronal, double sliceThicknessInSagittal)
            : this(slices)
        {
            this._sliceThicknessInCoronal = sliceThicknessInCoronal;
            this._sliceThicknessInSagittal = sliceThicknessInSagittal;
        }


        private void ValidateInput(Func<bool> validation, string message = "")
        {
            if (!validation())
            {
                throw new DicomDataException(message);
            }
        }

        private void ValidateInput(bool validated, string message = "") => ValidateInput(() => validated, message);

        private void BuildVolumeData()
        {
            // sort the slices
            _slices.Sort((a, b) => a.SortingValue.CompareTo(b.SortingValue));
            // calcualate values
            _slicesNormal = _slices.First().Geometry.DirectionNormal;
            var sliceDistances = _slices.Diff((a, b) => b.SortingValue - a.SortingValue);
            _minSliceSpace = sliceDistances.Min();
            _maxSliceSpace = sliceDistances.Max();
            var boundings = _slices.Select(s => s.Geometry.GetBoundingBox());
            BoundingMin = boundings.Select(b => b.min).GetBoundingBox().min;
            BoundingMax = boundings.Select(b => b.max).GetBoundingBox().max;
            _sortOrders = _slices.Select(s => s.SortingValue).ToArray();
            this.SliceThicknessInSource = (_slices.LastOrDefault().Geometry.PointTopLeft.Z - _slices.FirstOrDefault().Geometry.PointTopLeft.Z) / (this.Frames - 1);
        }

        private DicomDataset GetCommonData()
        {
            var valueComparer = new DicomValueComparer();
            var commonData = new DicomDataset().NotValidated();
            commonData.Add(
                _slices
                .Select(s => s.Dataset.Where(t => t.Tag != DicomTag.PixelData || (t.Tag.Group >= 0x6000 && t.Tag.Group < 0x6100)))
                .Aggregate((x, y) => x.Intersect(y, valueComparer))
                );
            return commonData;
        }

        private DicomRange<double> GetPixelDataRange()
        {
            BlockingCollection<DicomRange<double>> pixelDataRanges = new BlockingCollection<DicomRange<double>>();
            _slices.Select(s => s.Pixels.GetMinMax()).ToList();
            Parallel.For(0, _slices.Count, p =>
            {
                pixelDataRanges.Add(_slices[p].Pixels.GetMinMax());
            });
            var range = new DicomRange<double>(pixelDataRanges.Select(r => r.Minimum).Min(), pixelDataRanges.Select(r => r.Maximum).Max());
            return range;
        }

        private ILUT _lut = null;

        public ILUT Lut
        {
            get
            {
                if (_lut == null)
                {
                    var option = GrayscaleRenderOptions.FromDataset(_slices.First().Dataset);
                    var pipelie = new GenericGrayscalePipeline(option);
                    _lut = pipelie.LUT;
                    _lut.Recalculate();
                }
                return _lut;
            }
        }

        private int SortingIndex(double value, int guess)
        {
            var len = _sortOrders.Length;
            while (_sortOrders[guess] >= value && guess > 0)
            {
                guess--;
            }

            while (guess < len)
            {
                if (_sortOrders[guess] >= value)
                {
                    return guess;
                }
                guess++;
            }
            return -1;
        }

        /// <summary>
        /// ['1', '0', '0', '0', '0', '-1'] you are dealing with Coronal plane view
        /// ['0', '1', '0', '0', '0', '-1'] you are dealing with Sagittal plane view
        /// ['1', '0', '0', '0', '1', '0'] you are dealing with Axial plane view
        /// </summary>
        /// <param name="topleft"></param>
        /// <param name="rowDir"></param>
        /// <param name="colDir"></param>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="spacing"></param>
        /// <returns></returns>
        public short[] GetCut(Point3D topleft, Vector3D rowDir, Vector3D colDir, int rows, int cols, double spacing)
        {
            var output = new short[rows * cols];

            var deltaX = spacing * rowDir;
            var deltaY = spacing * colDir;
            var orderedDeltaX = _slicesNormal.DotProduct(deltaX);
            var orderedDeltaY = _slicesNormal.DotProduct(deltaY);

            var orderedRowStart = _slicesNormal.DotProduct(topleft);

            var lastIndex = 0;

            Parallel.For(0, cols, x =>
            {
                var pointInPatSpace = topleft + x * deltaX;
                var ordered = orderedRowStart + x * orderedDeltaX;
                for (int y = 0; y < rows; y++)
                {
                    // get index of the two planes
                    var index = SortingIndex(ordered, lastIndex);
                    if (index > 0)
                    {
                        lastIndex = index;
                        var nextSlice = _slices[index];
                        var prevSlice = _slices[index - 1];

                        var nextImgSpace = nextSlice.Geometry.TransformPatientPointToImage(pointInPatSpace);
                        var prevImgSpace = prevSlice.Geometry.TransformPatientPointToImage(pointInPatSpace);

                        var nextPixel = Interpolate(nextSlice.Pixels, nextImgSpace);
                        var prevPixel = Interpolate(prevSlice.Pixels, prevImgSpace);

                        if (nextPixel.HasValue && prevPixel.HasValue)
                        {
                            var pixel = (prevPixel.Value * (nextSlice.SortingValue - ordered) + nextPixel.Value * (ordered - prevSlice.SortingValue)) / (nextSlice.SortingValue - prevSlice.SortingValue);
                            // convert from 12bit to 8 bit
                            output[x + y * cols] = (short)pixel;
                        }
                    }
                    pointInPatSpace += deltaY;
                    ordered += orderedDeltaY;
                }
            });

            return output;
        }

        private double? Interpolate(IPixelData pixels, Point2D imgSpace)
        {
            if ((imgSpace.X >= 0.0) && (imgSpace.X < pixels.Width - 1) && (imgSpace.Y >= 0.0) && (imgSpace.Y < pixels.Height - 1))
            {
                var posX = (int)Math.Floor(imgSpace.X);
                double alphaX = imgSpace.X - posX;
                var posY = (int)Math.Floor(imgSpace.Y);
                double alphaY = imgSpace.Y - posY;

                return (1 - alphaX) * ((1 - alphaY) * pixels.GetPixel(posX, posY)
                    + alphaY * pixels.GetPixel(posX, posY + 1))
                    + alphaX * ((1 - alphaY) * pixels.GetPixel(posX + 1, posY)
                    + alphaY * pixels.GetPixel(posX + 1, posY + 1));
            }

            return null;
        }

        #region DicomVolume接口

        private double? windowWidth;
        public double WindowWidth
        {
            get => windowWidth.HasValue ? windowWidth.Value : _slices.Count == 0 ? double.NaN : _slices.First().Dataset.GetValue<double>(DicomTag.WindowWidth, 0);
            private set { windowWidth = value; }
        }

        private double? windowCenter;
        public double WindowCenter
        {
            get => windowCenter.HasValue ? windowCenter.Value : _slices.Count == 0 ? double.NaN : _slices.First().Dataset.GetValue<double>(DicomTag.WindowCenter, 0);
            private set { windowCenter = value; }
        }

        public int Columns => this._slices == null ? 0 : this._slices.FirstOrDefault().Dataset.GetValue<int>(DicomTag.Columns, 0);

        public int Rows => this._slices == null ? 0 : this._slices.FirstOrDefault().Dataset.GetValue<int>(DicomTag.Rows, 0);

        public int Frames => this._slices == null ? 0 : this._slices.Count;

        public double PixelSpacingBetweenColumnsInSource => _slices?.FirstOrDefault()?.Geometry.PixelSpacingBetweenColumns ?? 0;

        public double PixelSpacingBetweenRowsInSource => _slices?.FirstOrDefault()?.Geometry.PixelSpacingBetweenRows ?? 0;

        public double SliceThicknessInSource { get; private set; } //=> _slices?.FirstOrDefault()?.Dataset.GetValue<double>(DicomTag.SliceThickness, 0) ?? 0;

        public double RescaleIntercept => _slices?.FirstOrDefault().Dataset.GetValue<double>(DicomTag.RescaleIntercept, 0) ?? 0;
        public double RescaleSlope => _slices?.FirstOrDefault().Dataset.GetValue<double>(DicomTag.RescaleSlope, 0) ?? 0;

        private readonly Lazy<DicomRange<double>> _pixelDataRange;
        public DicomRange<double> PixelDataRange => _pixelDataRange.Value;

        public void SetWidthCenter(double width, double center)
        {
            var dataset = _slices.First().Dataset.Clone();
            dataset.AddOrUpdate(DicomTag.WindowWidth, width);
            dataset.AddOrUpdate(DicomTag.WindowCenter, center);
            var option = GrayscaleRenderOptions.FromDataset(dataset);
            var pipelie = new GenericGrayscalePipeline(option);
            _lut = pipelie.LUT;
            _lut.Recalculate();
            //if (_cache != null)
            //{
            //    _cache.Dispose();
            //    _cache = null;
            //}
            this.windowWidth = width;
            this.windowCenter = center;
        }

        public int GetNumberOfSlices(StackType stackType)
        {
            var volumeVector = this.BoundingMax - this.BoundingMin;
            var numberOfSlices = 0.0;
            switch (stackType)
            {
                case StackType.Axial: numberOfSlices = this._slices.Count; break;
                case StackType.Coronal: numberOfSlices = volumeVector.Y / SliceThicknessInCoronal; break;
                case StackType.Sagittal: numberOfSlices = volumeVector.X / SliceThicknessInSagittal; break;
            }
            return (int)numberOfSlices;
        }

        public Size GetSizeOfSlice(StackType stackType)
        {
            var volumeVector = this.BoundingMax - this.BoundingMin;
            double spacing = PixelSpacingInSource;
            int rows = 0;
            int cols = 0;
            if (stackType == StackType.Axial)
            {
                rows = (int)Math.Round(volumeVector.Y / spacing);
                cols = (int)Math.Round(volumeVector.X / spacing);
            }
            else if (stackType == StackType.Coronal)
            {
                rows = (int)Math.Round(volumeVector.Z / spacing);
                cols = (int)Math.Round(volumeVector.X / spacing);
            }
            else if (stackType == StackType.Sagittal)
            {
                rows = (int)Math.Round(volumeVector.Z / spacing);
                cols = (int)Math.Round(volumeVector.Y / spacing);
            }
            return new Size(cols, rows);
        }

        public bool EnableCompression = true;
        private ICompressor compressor = new GZipCompressor();
        private MemoryCache _cache;

        public byte[] RenderAsGray8Array(StackType stackType, int page)
        {
            var volumeVector = this.BoundingMax - this.BoundingMin;
            double spacing = PixelSpacingInSource;
            Point3D topLeft = new Point3D(this.BoundingMin.X, this.BoundingMin.Y, this.BoundingMax.Z);
            Vector3D sliceVector = new Vector3D(0, 0, 0);
            Vector3D rowDir = new Vector3D(0, 0, 0);
            Vector3D colDir = new Vector3D(0, 0, 0);
            int rows = 0;
            int cols = 0;
            if (stackType == StackType.Axial) /// Axial 视图无需缓存
            {
                sliceVector = new Vector3D(0, -SliceThicknessInSource, 0);
                rowDir = new Vector3D(1, 0, 0);
                colDir = new Vector3D(0, 1, 0);
                rows = (int)Math.Round(volumeVector.Y / spacing);
                cols = (int)Math.Round(volumeVector.X / spacing);
                byte[] imageData = new byte[rows * cols];
                page = GetAxialSliceIndex(page);
                Parallel.For(0, rows, y =>
                {
                    for (int x = 0; x < cols; x++)
                    {
                        var pixel = this._slices[page].Pixels.GetPixel(x, y);
                        var gray = (byte)(((int)Lut[pixel]) & 0xFF);
                        imageData[x + y * cols] = gray;
                    }
                });
                return imageData;
            }

            if (_cache == null) _cache = new MemoryCache("DicomImages");
            string cacheKey = $"{stackType}_{page.ToString().PadLeft(3, '0')}";

            if (stackType == StackType.Coronal)
            {
                sliceVector = new Vector3D(0, SliceThicknessInCoronal, 0);
                rowDir = new Vector3D(1, 0, 0);
                colDir = new Vector3D(0, 0, -1);
                rows = (int)Math.Round(volumeVector.Z / spacing);
                cols = (int)Math.Round(volumeVector.X / spacing);
            }
            else if (stackType == StackType.Sagittal)
            {
                sliceVector = new Vector3D(SliceThicknessInSagittal, 0, 0);
                rowDir = new Vector3D(0, 1, 0);
                colDir = new Vector3D(0, 0, -1);
                rows = (int)Math.Round(volumeVector.Z / spacing);
                cols = (int)Math.Round(volumeVector.Y / spacing);
            }

            short[] datas = null;
            if (!_cache.Contains(cacheKey))
            {
                datas = GetCut(topLeft + page * sliceVector, rowDir, colDir, rows, cols, spacing);
                if (this.EnableCompression)
                {
                    byte[] bytes = compressor.Compress(datas.SelectMany(BitConverter.GetBytes).ToArray());
                    _cache.Set(cacheKey, bytes, null);
                }
                else
                {
                    _cache.Set(cacheKey, datas, null);
                }
            }
            else
            {
                if (this.EnableCompression)
                {
                    datas = new short[rows * cols];
                    byte[] bytes = compressor.Decompress((byte[])_cache.Get(cacheKey, null));
                    Marshal.Copy(bytes, 0, Marshal.UnsafeAddrOfPinnedArrayElement(datas, 0), bytes.Length);
                }
                else
                {
                    datas = (short[])_cache.Get(cacheKey, null);
                }
            }

            byte[] pixels = new byte[rows * cols];
            Parallel.For(0, rows, y =>
            {
                int x0 = y * cols;
                for (int x = 0; x < cols; x++)
                {
                    int i = x + x0;
                    var pixel = datas[i];
                    var gray = (byte)(((int)Lut[pixel]) & 0xFF);
                    pixels[i] = gray;
                }
            });
            return pixels;
        }

        public short[] GetDicomPixelData(int page)
        {
            page = GetAxialSliceIndex(page);
            if (this._slices[page].PixelData.BytesAllocated > 2) throw new NotSupportedException("不支持的影像格式");
            int width = this._slices[page].PixelData.Width;
            int height = this._slices[page].PixelData.Height;
            short[] pixels = new short[width * height];
            if (this._slices[page].PixelData.BytesAllocated == 2) // 16位数据
            {
                byte[] data = this._slices[page].PixelData.GetFrame(0).Data;
                Marshal.Copy(data.GetIntPtr(), pixels, 0, width * height);
            }
            else // 8位数据
            {
                byte[] data = this._slices[page].PixelData.GetFrame(0).Data;
                for (int i = 0; i < width * height; i++)
                {
                    pixels[i] = data[i];
                }
            }
            return pixels;
        }

        public short[] GetDicomPixelData()
        {
            if (this._slices[0].PixelData.BytesAllocated > 2) throw new NotSupportedException("不支持的影像格式");
            short[] pixels = new short[Frames * Rows * Columns];
            int pageSize = Rows * Columns;
            for (int z = 0; z < Frames; z++)
            {
                int page = GetAxialSliceIndex(z);
                if (this._slices[page].PixelData.BytesAllocated == 2) // 16位数据
                {
                    byte[] data = this._slices[page].PixelData.GetFrame(0).Data;
                    Marshal.Copy(data.GetIntPtr(), pixels, z * pageSize, pageSize);
                }
                else // 8位数据
                {
                    int z0 = z * pageSize;
                    byte[] data = this._slices[page].PixelData.GetFrame(0).Data;
                    for (int i = 0; i < pageSize; i++)
                    {
                        pixels[z0 + i] = data[z0 + i];
                    }
                }
            }
            return pixels;
        }

        public double GetPixel(int x, int y, int z)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return double.NaN;
            int page = GetAxialSliceIndex(z);
            double pixel = this._slices[page].Pixels.GetPixel(x, y);
            pixel = pixel * RescaleSlope + RescaleIntercept;
            return pixel;
        }

        /// <summary>
        /// 根据层信息，获取对应视图中的坐标信息
        /// </summary>
        /// <param name="stackType"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        public System.Windows.Media.Media3D.Point3D ConvertSliceToImagePixel(StackType stackType, NotifyPoint3<int> slice)
        {
            var anchor = new System.Windows.Media.Media3D.Point3D(
                slice.X * this.SliceThicknessInSagittal,
                slice.Y * this.SliceThicknessInCoronal,
                slice.Z * this.SliceThicknessInSource);
            //(this.Frames - 1 - slice.Z) * this.SliceThicknessInSource);
            var pixelPoint = new System.Windows.Media.Media3D.Point3D();
            switch (stackType)
            {
                case StackType.Axial:
                    pixelPoint.Offset(
                    anchor.X / PixelSpacingBetweenColumnsInSource,
                    anchor.Y / PixelSpacingBetweenRowsInSource,
                    anchor.Z / SliceThicknessInSource); break;// X,Y
                case StackType.Coronal:
                    pixelPoint.Offset(
                    anchor.X / this.PixelSpacingInSource,
                    anchor.Z / this.PixelSpacingInSource,
                    anchor.Y / this.SliceThicknessInCoronal); break; // X,Z
                case StackType.Sagittal:
                    pixelPoint.Offset(
                    anchor.Y / this.PixelSpacingInSource,
                    anchor.Z / this.PixelSpacingInSource,
                    anchor.X / this.SliceThicknessInSagittal); break; // Y,Z
            }
            return pixelPoint;
        }


        public Histogram GetHistogram(int page)
        {
            page = this.GetAxialSliceIndex(page);
            var data = this.GetDicomPixelData(page);
            var range = data.Range().Select(v => v * this.RescaleSlope + this.RescaleIntercept).ToArray();
            var histogram = new Histogram((int)range[0], (int)range[1]);
            if (this.RescaleSlope == 1)
            {
                int intercept = (int)this.RescaleIntercept;
                for (int i = 0; i < data.Length; i++) histogram.Add(data[i] + intercept);
            }
            else
            {
                double slope = this.RescaleSlope;
                int intercept = (int)this.RescaleIntercept;
                for (int i = 0; i < data.Length; i++) histogram.Add((int)(data[i] * slope + intercept));
            }
            return histogram;

            // return this._slices[page].Pixels.GetHistogram(0);
        }

        #endregion

        #region Dispose接口

        public void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
            }
            this._slices.Clear();
        }

        #endregion

        private int GetAxialSliceIndex(int page)
        {
            return Frames - 1 - page;
        }

        private static MD5 md5 = new MD5CryptoServiceProvider();
        public static string GetMD5(byte[] bytes)
        {
            byte[] result = md5.ComputeHash(bytes);
            return string.Join("", result.Select(b => b.ToString("X2")));
        }
    }
}
