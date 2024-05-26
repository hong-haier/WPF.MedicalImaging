using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WPF.MedicalImaging
{

    /// <summary>
    /// 类 名 称：ImageViewer
    /// 创 建 人：AI-Hongqiang
    /// 创建时间：2023/11/27 14:49:37
    /// 描    述：
    /// </summary>
    public class ImageViewer : FrameworkElement
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(ImageViewer),
                new PropertyMetadata(null, BackgroundPropertyChangedCallback));

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(BitmapSource), typeof(ImageViewer),
                new PropertyMetadata(null));



        public static void BackgroundPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageViewer viewer)
            {
                viewer.DrawingBackground();
            }
        }

        protected DrawingVisual imageVisual;
        protected DrawingVisual backgroundVisual;
        protected DrawingVisual annotationVisual;

        public ImageViewer()
        {
            _children = new VisualCollection(this);
            ClipToBounds = true;
            InitImageLayer();
        }

        protected virtual void InitImageLayer()
        {
            Transforms.Children.Add(_scale);
            Transforms.Children.Add(_translate);
            backgroundVisual = new DrawingVisual();
            _children.Add(backgroundVisual);
            imageVisual = new DrawingVisual();
            _children.Add(imageVisual);
            annotationVisual = new DrawingVisual();
            _children.Add(annotationVisual);
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public virtual BitmapSource ImageSource
        {
            get { return (BitmapSource)GetValue(ImageSourceProperty); }
            set
            {
                SetValue(ImageSourceProperty, value);
                this.Refresh();
            }
        }


        public virtual void Refresh()
        {
            if (isFit) this.UpdateTransformsToFit();
            this.Drawing();
        }

        protected virtual void Drawing()
        {
            DrawingBackground();
            DrawingImage();
            DrawingAnnotation();
        }

        protected virtual void DrawingBackground()
        {
            var drawingContext = backgroundVisual.RenderOpen();
            drawingContext.DrawRectangle(Background ?? Brushes.Black, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            drawingContext.Close();
        }

        public virtual void DrawingImage()
        {
            var drawingContext = imageVisual.RenderOpen();
            if (ImageSource != null)
            {
                drawingContext.PushTransform(Transforms);
                drawingContext.DrawImage(ImageSource, new Rect(0, 0, ImageSource.Width, ImageSource.Height));
                drawingContext.Pop();
            }
            drawingContext.Close();
        }

        protected virtual void DrawingAnnotation()
        {
            var drawingContext = annotationVisual.RenderOpen();
            drawingContext.DrawEllipse(Brushes.Red, null, new Point(RenderSize.Width / 2, RenderSize.Height / 2), 6, 6);
            drawingContext.Close();
        }

        private Point previousMousePosition = new Point();
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Handled) return;
            var mousePosition = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.isFit = false;
                var offset = mousePosition - previousMousePosition;
                _translate.X += offset.X;
                _translate.Y += offset.Y;
                previousMousePosition = mousePosition;
                this.DrawingImage();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Handled) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                previousMousePosition = e.GetPosition(this);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Handled) return;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Handled) return;
            if (_scale.ScaleX <= minScale && e.Delta > 0) return;
            if (_scale.ScaleX >= maxScale && e.Delta < 0) return;
            this.isFit = false;
            Point mousePosition = e.GetPosition(this);
            //double step = _scale.ScaleX <= 1 ? 0.05 : _scale.ScaleX <= 5 ? 0.1 : 0.2;
            var x = _scale.ScaleX / 10;
            double step = x / Math.Sqrt(1 + x * x) / 2;
            double scale = _scale.ScaleX - e.Delta / 120 * step;
            this.Scale(scale, mousePosition);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            bool update = isFit ? UpdateTransformsToFit() : UpdataTransformsToNormal(sizeInfo.PreviousSize, sizeInfo.NewSize);
            this.Drawing();
        }

        #region 缩放和平移

        protected bool isFit = true;
        protected double minScale = 0.1;
        protected double maxScale = 20;
        protected ScaleTransform _scale = new ScaleTransform();
        protected TranslateTransform _translate = new TranslateTransform();
        protected TransformGroup Transforms = new TransformGroup();

        public virtual void Scale(double scale, Point center)
        {
            scale = Math.Min(maxScale, Math.Max(minScale, scale));
            if (scale == _scale.ScaleX) return;

            Point imageCenter = Transforms.Inverse.Transform(center);
            _scale.ScaleX = scale;
            _scale.ScaleY = scale;
            Point viewCenter = Transforms.Transform(imageCenter); //ConvertPointToViewPosition(imageCenter);
            var offset = viewCenter - center;
            _translate.X -= offset.X;
            _translate.Y -= offset.Y;
            this.DrawingImage();
        }

        protected virtual bool UpdateTransformsToFit()
        {
            if (ImageSource != null)
            {
                double imageW = ImageSource.PixelWidth;
                double imageH = ImageSource.PixelHeight;
                double renderW = RenderSize.Width;
                double renderH = RenderSize.Height;
                double dw = renderW / imageW;
                double dh = renderH / imageH;
                double d = Math.Min(dw, dh);
                _scale.ScaleX = d;
                _scale.ScaleY = d;
                _translate.X = (renderW - imageW * d) / 2;
                _translate.Y = (renderH - imageH * d) / 2;
            }
            else
            {
                _scale.ScaleX = 1;
                _scale.ScaleY = 1;
                _translate.X = 0;
                _translate.Y = 0;
            }
            return true;
        }

        protected virtual bool UpdataTransformsToNormal(Size preRenderSize, Size newRenderSize)
        {
            double dw = newRenderSize.Width / preRenderSize.Width;
            double dh = newRenderSize.Height / preRenderSize.Height;
            double d = dw < 1 && dh < 1 ? Math.Max(dw, dh) : dw > 1 && dh > 1 ? Math.Min(dw, dh) : 1;
            double newScale = _scale.ScaleX * d;
            newScale = Math.Min(maxScale, Math.Max(minScale, newScale));
            Point renderCenter0 = new Point(preRenderSize.Width / 2, preRenderSize.Height / 2); // 原来的视图中心
            Point imageCenter0 = ConvertPointToImagePosition(renderCenter0); // 原始视图中心的像素坐标P0
            _scale.ScaleX = newScale;
            _scale.ScaleY = newScale;
            Point renderCenter1 = ConvertPointToViewPosition(imageCenter0);// P0点在新视图的位置
            _translate.X += newRenderSize.Width / 2 - renderCenter1.X;
            _translate.Y += newRenderSize.Height / 2 - renderCenter1.Y;
            return true;
        }

        #endregion

        #region 坐标转换

        /// <summary>
        /// 将View坐标转换为Image坐标
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public virtual Point ConvertPointToImagePosition(Point p)
        {
            return Transforms.Inverse.Transform(p);
            //return new Point((p.X - _translate.X) / _scale.ScaleX, (p.Y - _translate.Y) /  _scale.ScaleY);
        }
        public virtual Point ConvertPointToImagePosition(double x, double y)
        {
            return Transforms.Inverse.Transform(new Point(x, y));
            //return new Point((x - _translate.X) / _scale.ScaleX, (y - _translate.Y) / _scale.ScaleY);
        }

        /// <summary>
        /// 将Image坐标转换为View坐标
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public virtual Point ConvertPointToViewPosition(Point p)
        {
            return Transforms.Transform(p);
            //return new Point(p.X * _scale.ScaleX + _translate.X, p.Y * _scale.ScaleY + _translate.Y);
        }
        public virtual Point ConvertPointToViewPosition(double x, double y)
        {
            return Transforms.Transform(new Point(x, y));
            //return new Point(x * _scale.ScaleX + _translate.X, y * _scale.ScaleY + _translate.Y);
        }

        public virtual IEnumerable<Point> ConvertPointToViewPosition(IEnumerable<Point> ps)
        {
            foreach (Point p in ps)
            {
                yield return ConvertPointToViewPosition(p);
            }
        }

        #endregion

        #region 重写FrameworkElement的渲染图层

        protected VisualCollection _children;

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
                throw new ArgumentOutOfRangeException();
            return _children[index];
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// C# WPF 保存WriteableBitmap图像
        /// </summary>
        /// <param name="bmp"></param>
        /// 
        protected void SaveBitmap(WriteableBitmap bmp)
        {
            if (bmp == null)
            {
                return;
            }
            try
            {
                RenderTargetBitmap rtbitmap = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, bmp.DpiX, bmp.DpiY, PixelFormats.Default);
                DrawingVisual drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawImage(bmp, new Rect(0, 0, bmp.Width, bmp.Height));
                }
                rtbitmap.Render(drawingVisual);
                JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(rtbitmap));
                string strDir = @"D:\XXX\";
                string strpath = strDir + DateTime.Now.ToString("yyyyMMddfff") + ".jpg";
                if (!Directory.Exists(strDir))
                {
                    Directory.CreateDirectory(strDir);
                }
                if (!File.Exists(strpath))
                {
                    Stream fileStream = File.OpenWrite(strpath);
                    bitmapEncoder.Save(fileStream);
                    fileStream.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        #endregion
    }
}
