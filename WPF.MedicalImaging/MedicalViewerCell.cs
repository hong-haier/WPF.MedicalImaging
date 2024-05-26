using FellowOakDicom.Imaging.Reconstruction;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.MedicalImaging
{
    public partial class MedicalViewerCell : ImageViewer
    {
        public static readonly DependencyProperty StackTypeProperty =
            DependencyProperty.Register("StackType", typeof(StackType), typeof(MedicalViewerCell), new PropertyMetadata(StackType.Axial));

        public static readonly DependencyProperty PageIndexProperty =
            DependencyProperty.Register("PageIndex", typeof(int), typeof(MedicalViewerCell), new PropertyMetadata(0, PageIndexPropertyChangedCallback));

        public static readonly DependencyProperty PageCountProperty =
            DependencyProperty.Register("PageCount", typeof(int), typeof(MedicalViewerCell), new PropertyMetadata(0));

        public static void PageIndexPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue.Equals(e.NewValue)) return;
            if (d is MedicalViewerCell viewer)
            {
                viewer.UpdateImageSource();
            }
        }

        protected DrawingVisual maskVisual;

        public MedicalViewerCell() : base()
        {
            this.Focusable = true;
            //this.SetActionType(MedicalViewerMouseButtons.Left, MedicalViewerActionType.PaintEffect);
            this.SetActionType(MedicalViewerMouseButtons.Left, MedicalViewerActionType.WindowLevel);
            this.SetActionType(MedicalViewerMouseButtons.Right, MedicalViewerActionType.Offset);
            this.SetActionType(MedicalViewerMouseButtons.Wheel, MedicalViewerActionType.Scale);
        }

        protected override void InitImageLayer()
        {
            base.InitImageLayer();
            maskVisual = new DrawingVisual();
            _children.Add(maskVisual);
        }

        public StackType StackType
        {
            get { return (StackType)GetValue(StackTypeProperty); }
            set { SetValue(StackTypeProperty, value); }
        }

        public int PageIndex
        {
            get { return (int)GetValue(PageIndexProperty); }
            set { SetValue(PageIndexProperty, value); }
        }

        public int PageCount
        {
            get { return (int)GetValue(PageCountProperty); }
            set { SetValue(PageCountProperty, value); }
        }

        private MedicalViewerBase parentViewer;
        public MedicalViewerBase ParentViewer
        {
            get
            {
                if (this.parentViewer == null)
                {
                    FrameworkElement temp = this.Parent as FrameworkElement;
                    while (temp != null && !(temp is MedicalViewerBase))
                    {
                        temp = temp.Parent as FrameworkElement;
                    }
                    if (temp != null)
                    {
                        this.parentViewer = (MedicalViewerBase)temp;
                    }
                }
                return this.parentViewer;
            }
        }

        protected WriteableBitmap dicomImage;
        public override BitmapSource ImageSource
        {
            get => dicomImage;
        }

        const double DPI = 96.0;
        public virtual void InitCell()
        {
            if (ParentViewer == null) return;
            if (ParentViewer.DicomVolume == null) return;
            this.PageCount = ParentViewer.DicomVolume.GetNumberOfSlices(this.StackType);
            var imageSize = ParentViewer.DicomVolume.GetSizeOfSlice(this.StackType);
            this.dicomImage = new WriteableBitmap((int)imageSize.Width, (int)imageSize.Height, DPI, DPI, PixelFormats.Gray8, null);
            this.PageIndex = this.PageCount / 2;
            this.Refresh();
        }

        protected virtual void UpdateImageSource()
        {
            if (ParentViewer == null) return;
            if (PageIndex < 1 || PageIndex > PageCount) return;
            switch (this.StackType)
            {
                case StackType.Axial: ParentViewer.AnchorSlices.SetZ(PageIndex - 1); break;
                case StackType.Coronal: ParentViewer.AnchorSlices.SetY(PageIndex - 1); break;
                case StackType.Sagittal: ParentViewer.AnchorSlices.SetX(PageIndex - 1); break;
            }
            var pixelData = ParentViewer.DicomVolume.RenderAsGray8Array(this.StackType, this.PageIndex - 1);
            if (pixelData != null)
            {
                var image = this.dicomImage;
                image.Lock();
                // 当 image.PixelWidth 不是4的倍数时，image.PixelWidth 和 image.BackBufferStride 不相等
                if (image.PixelWidth == image.BackBufferStride)
                {
                    Marshal.Copy(pixelData, 0, image.BackBuffer, pixelData.Length);
                }
                else
                {
                    for (int y = 0; y < image.PixelHeight; y++)
                    {
                        Marshal.Copy(pixelData, y * image.PixelWidth, image.BackBuffer + y * image.BackBufferStride, image.PixelWidth);
                    }
                }
                image.AddDirtyRect(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight));
                image.Unlock();
            }
            DrawingImage();
            DrawingMask();
        }

        public override void Scale(double scale, Point center)
        {
            base.Scale(scale, center);
            this.DrawingMask();
        }

        public void Scale(double scale)
        {
            this.Scale(scale, new Point(this.ActualWidth / 2, this.ActualHeight / 2));
        }

        protected virtual void DrawingMask()
        {
            if (ParentViewer == null) return;
            if (ParentViewer.MaskVolume == null) return;

            var drawingContext = maskVisual.RenderOpen();
            if (this.StackType == StackType.Axial)
            {
                //var graphs = this.ParentViewer.MaskVolume.GetAxialMaskGraphs(this.PageIndex - 1);
                //if (graphs != null && graphs.Count > 0)
                //{
                //    foreach (var graph in graphs)
                //    {
                //        foreach (var path in graph.Paths)
                //        {
                //            var line = ConvertPointToViewPosition(path).ToList();
                //            var geometry = CreatePathGeometry(line, true);
                //            drawingContext.DrawGeometry(
                //                new SolidColorBrush(Color.FromArgb(64, graph.Color.R, graph.Color.G, graph.Color.B)), 
                //                new Pen(new SolidColorBrush(graph.Color), 1), geometry);
                //        }
                //    }
                //    //drawingContext.DrawEllipse(Brushes.Red, null, new Point(RenderSize.Width / 2, RenderSize.Height / 2), 6, 6);
                //}

                var masks = this.ParentViewer.MaskVolume.GetAxialMaskBitmap(this.PageIndex - 1);
                if (masks != null)
                {
                    drawingContext.PushTransform(Transforms);
                    foreach (var bitmap in masks)
                    {
                        drawingContext.DrawImage(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height));
                    }
                    drawingContext.Pop();
                }
                //var mask = this.ParentViewer.MaskVolume.GetAxialMaskBitmap2(this.PageIndex - 1);
                //if (mask != null)
                //{
                //    drawingContext.PushTransform(Transforms);
                //    drawingContext.DrawImage(mask, new Rect(0, 0, mask.Width, mask.Height));
                //    drawingContext.Pop();
                //}
            }
            //var tt = CreatePathGeometry(new List<Point> { new Point(10, 100), new Point(100, 100), new Point(100, 50) });
            //drawingContext.DrawGeometry(Brushes.LightBlue, new Pen(new SolidColorBrush(Colors.Red), 1), tt);
            drawingContext.Close();
        }

        private Pen solidRedPen = new Pen(Brushes.Red, 1);
        private Pen solidGreenPen = new Pen(Brushes.Green, 1);
        private Pen solidYellowPen = new Pen(Brushes.Yellow, 1);
        private Pen dottedOrangePen = new Pen(Brushes.Orange, 1) { DashStyle = new DashStyle(new double[] { 5, 5 }, 0) };

        public override void DrawingImage()
        {
            if (ParentViewer == null) return;
            if (PageIndex < 1 || PageIndex > PageCount) return;
            var drawingContext = imageVisual.RenderOpen();
            if (ImageSource != null)
            {
                drawingContext.PushTransform(Transforms);
                drawingContext.DrawImage(ImageSource, new Rect(0, 0, ImageSource.Width, ImageSource.Height));
                drawingContext.Pop();
            }
            if (this.IsFocused)
            {
                drawingContext.DrawRectangle(null, dottedOrangePen, new Rect(new Point(3, 3), new Point(this.ActualWidth - 3, this.ActualHeight - 3)));
            }
            int textBoxPadding = 5;
            FormattedText text = new FormattedText($"Fr: {PageIndex}/{PageCount}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Microsoft Yahei"),
                12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(text, new Point(textBoxPadding, textBoxPadding));
            text = new FormattedText($"Scale:{this._scale.ScaleX.ToString("0.00")}X",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Microsoft Yahei"),
                12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(text, new Point(textBoxPadding, this.ActualHeight - text.Height - textBoxPadding));
            if (ParentViewer.DicomVolume != null)
            {
                text = new FormattedText($"WW: {this.ParentViewer.DicomVolume.WindowWidth}, WC {this.ParentViewer.DicomVolume.WindowCenter}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Microsoft Yahei"),
                    12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawingContext.DrawText(text, new Point(3, this.ActualHeight - text.Height * 2 - 3));
            }
            Point3D anchorSlice;
            Point anchorPoint;
            switch (this.StackType)
            {
                case StackType.Axial: // X,Y
                    anchorSlice = ParentViewer.DicomVolume.ConvertSliceToImagePixel(this.StackType, ParentViewer.AnchorSlices);
                    anchorPoint = this.ConvertPointToViewPosition(anchorSlice.X, anchorSlice.Y);
                    drawingContext.DrawLine(solidGreenPen, new Point(0, anchorPoint.Y), new Point(this.ActualWidth, anchorPoint.Y)); // 水平
                    drawingContext.DrawLine(solidYellowPen, new Point(anchorPoint.X, 0), new Point(anchorPoint.X, this.ActualHeight)); // 垂直
                    break;
                case StackType.Coronal: // X,Z
                    anchorSlice = ParentViewer.DicomVolume.ConvertSliceToImagePixel(this.StackType, ParentViewer.AnchorSlices);
                    anchorPoint = this.ConvertPointToViewPosition(anchorSlice.X, anchorSlice.Y);
                    drawingContext.DrawLine(solidRedPen, new Point(0, anchorPoint.Y), new Point(this.ActualWidth, anchorPoint.Y)); // 水平
                    drawingContext.DrawLine(solidYellowPen, new Point(anchorPoint.X, 0), new Point(anchorPoint.X, this.ActualHeight)); // 垂直
                    break;
                case StackType.Sagittal: // Y,Z
                    anchorSlice = ParentViewer.DicomVolume.ConvertSliceToImagePixel(this.StackType, ParentViewer.AnchorSlices);
                    anchorPoint = this.ConvertPointToViewPosition(anchorSlice.X, anchorSlice.Y);
                    drawingContext.DrawLine(solidRedPen, new Point(0, anchorPoint.Y), new Point(this.ActualWidth, anchorPoint.Y)); // 水平
                    drawingContext.DrawLine(solidGreenPen, new Point(anchorPoint.X, 0), new Point(anchorPoint.X, this.ActualHeight)); // 垂直
                    break;
            }
            drawingContext.Close();
        }

        public override void Refresh()
        {
            this.UpdateImageSource();
            base.Refresh();
        }

        protected override void DrawingAnnotation()
        {
            var drawingContext = annotationVisual.RenderOpen();
            if (this.mouseDownPath != null && mouseDownPath.Count > 0)
            {
                //drawingContext.DrawGeometry(
                //    new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)), 
                //    new Pen(Brushes.Red, 10), 
                //    CreatePathGeometry(mouseDownPath));
                foreach (var p in mouseDownPath)
                {
                    drawingContext.DrawEllipse(null, new Pen(Brushes.Red, 1), p, 12, 12);
                }
            }
            drawingContext.Close();
        }

        /// <summary>
        /// https://stackoverflow.com/questions/19680947/how-to-convert-a-point-til-pathgeometry
        /// </summary>
        /// <param name="points"></param>
        /// <param name="closed"></param>
        /// <returns></returns>
        private PathGeometry CreatePathGeometry(IEnumerable<Point> points, bool closed = false)
        {
            if (points == null) return null;
            if (points.Count() == 0) return null;

            int i = 0;
            Point start = points.FirstOrDefault();
            List<LineSegment> segments = new List<LineSegment>();
            foreach (var p in points)
            {
                if (i++ > 0) segments.Add(new LineSegment(p, true));
            }
            PathFigure figure = new PathFigure(start, segments, closed); //true if closed
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

    }
}
