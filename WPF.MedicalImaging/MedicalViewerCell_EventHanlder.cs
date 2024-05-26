using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace WPF.MedicalImaging
{
    public partial class MedicalViewerCell
    {

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            // Console.WriteLine(e.Key + " " + (int)e.Key );
            switch (e.Key)
            {
                case Key.PageDown: if (this.PageIndex < this.PageCount) this.PageIndex = this.PageIndex + 1; break;
                case Key.PageUp: if (this.PageIndex > 1) this.PageIndex = this.PageIndex - 1; break;
                case Key.System:
                    if (e.SystemKey == Key.F10)
                    {
                        this.isFit = true; this.UpdateTransformsToFit(); this.Drawing();
                    }
                    break;
                case Key.F10: this.isFit = true; this.UpdateTransformsToFit(); this.Drawing(); break;
                case Key.OemMinus: this.Scale(_scale.ScaleX - 0.1); break;
                case Key.OemPlus: this.Scale(_scale.ScaleX + 0.1); break;
                default: break;
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            this.DrawingImage();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            this.DrawingImage();
        }

        private Dictionary<MedicalViewerMouseButtons, MedicalViewerActionType> cellActions
            = new Dictionary<MedicalViewerMouseButtons, MedicalViewerActionType>();

        public virtual void SetActionType(MedicalViewerMouseButtons mouseButton, MedicalViewerActionType actionType)
        {
            if (cellActions.ContainsKey(mouseButton))
            {
                cellActions[mouseButton] = actionType;
            }
            else
            {
                cellActions.Add(mouseButton, actionType);
            }
            //if (mouseButton == MedicalViewerMouseButtons.Left)
            //{
            //    this.SetCellCursor(actionType);
            //}
            this.Drawing();
        }

        public MedicalViewerActionType GetActionType(MedicalViewerMouseButtons mouseButton)
        {
            if (!cellActions.ContainsKey(mouseButton))
                return MedicalViewerActionType.None;
            return cellActions[mouseButton];
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Handled) return;
            MedicalViewerActionType actionType = currentMouseActionType;
            if (actionType == MedicalViewerActionType.None) return;
            this.ExecuteMouseOperation(actionType, MouseStateType.MouseMove, new MouseSuperEventArgs(e, MouseStateType.MouseMove));
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            MedicalViewerActionType actionType =
                e.ChangedButton == MouseButton.Left ? GetActionType(MedicalViewerMouseButtons.Left) :
                e.ChangedButton == MouseButton.Right ? GetActionType(MedicalViewerMouseButtons.Right) :
                e.ChangedButton == MouseButton.Middle ? GetActionType(MedicalViewerMouseButtons.Middle) :
                MedicalViewerActionType.None;
            if (actionType == MedicalViewerActionType.None) return;
            this.ExecuteMouseOperation(actionType, MouseStateType.MouseDown, new MouseSuperEventArgs(e, MouseStateType.MouseDown));
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            MedicalViewerActionType actionType =
                e.ChangedButton == MouseButton.Left ? GetActionType(MedicalViewerMouseButtons.Left) :
                e.ChangedButton == MouseButton.Right ? GetActionType(MedicalViewerMouseButtons.Right) :
                e.ChangedButton == MouseButton.Middle ? GetActionType(MedicalViewerMouseButtons.Middle) :
                MedicalViewerActionType.None;
            if (actionType == MedicalViewerActionType.None) return;
            this.ExecuteMouseOperation(actionType, MouseStateType.MouseUp, new MouseSuperEventArgs(e, MouseStateType.MouseUp));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            MedicalViewerActionType actionType = GetActionType(MedicalViewerMouseButtons.Wheel);
            if (actionType == MedicalViewerActionType.None) return;
            this.ExecuteMouseOperation(actionType, MouseStateType.MouseWheel, new MouseSuperEventArgs(e, MouseStateType.MouseWheel));
        }

        private bool currentMouseButtonIsDown = false;
        private MedicalViewerActionType currentMouseActionType = MedicalViewerActionType.None;
        private Point previousMouseDownPosition = new Point();
        private Point previousMouseMovePosition = new Point();
        internal virtual void ExecuteMouseOperation(
            MedicalViewerActionType actionType,
            MouseStateType mouseStatus,
            MouseSuperEventArgs e)
        {
            if (mouseStatus == MouseStateType.MouseDown)
            {
                currentMouseButtonIsDown = true;
                currentMouseActionType = actionType;
            }
            if (mouseStatus == MouseStateType.MouseUp)
            {
                currentMouseButtonIsDown = false;
                currentMouseActionType = MedicalViewerActionType.None;
            }
            switch (actionType)
            {
                case MedicalViewerActionType.Offset: MouseOffset(mouseStatus, e); break;
                case MedicalViewerActionType.Stack: MouseStack(mouseStatus, e); break;
                case MedicalViewerActionType.Scale: MouseScale(mouseStatus, e); break;
                case MedicalViewerActionType.WindowLevel: MouseWindowLevel(mouseStatus, e); break;
                case MedicalViewerActionType.PaintEffect: MousePaintEffect(mouseStatus, e); break;
            }
        }

        internal virtual void MouseOffset(MouseStateType mouseStateType, MouseSuperEventArgs e)
        {
            switch (mouseStateType)
            {
                case MouseStateType.MouseDown:
                    previousMouseMovePosition = e.GetPosition(this);
                    break;
                case MouseStateType.MouseMove:
                    if (currentMouseButtonIsDown)
                    {
                        this.isFit = false;
                        var mousePosition = e.GetPosition(this);
                        var offset = mousePosition - previousMouseMovePosition;
                        _translate.X += offset.X;
                        _translate.Y += offset.Y;
                        previousMouseMovePosition = mousePosition;
                        this.DrawingImage();
                        this.DrawingMask();
                    }
                    break;
                case MouseStateType.MouseUp:
                    break;
            }
        }

        private int previousMouseDownPageIndex = 0;
        internal virtual void MouseStack(MouseStateType mouseStateType, MouseSuperEventArgs e)
        {
            switch (mouseStateType)
            {
                case MouseStateType.MouseDown:
                    previousMouseDownPageIndex = this.PageIndex;
                    previousMouseDownPosition = e.GetPosition(this);
                    break;
                case MouseStateType.MouseMove:
                    var mousePosition = e.GetPosition(this);
                    int deltaPage = (int)((mousePosition.Y - previousMouseDownPosition.Y) / 5);
                    if (this.PageIndex <= 1 && deltaPage < 0) return;
                    if (this.PageIndex >= PageCount && deltaPage > 0) return;
                    int page = previousMouseDownPageIndex + deltaPage;
                    if (page < 1) page = 1;
                    if (page > PageCount) page = PageCount;
                    this.PageIndex = page;
                    previousMouseMovePosition = mousePosition;
                    break;
                case MouseStateType.MouseUp:
                    break;
                case MouseStateType.MouseWheel:
                    if (this.PageIndex <= 1 && e.Delta < 0) return;
                    if (this.PageIndex >= PageCount && e.Delta > 0) return;
                    this.PageIndex = this.PageIndex + (e.Delta > 0 ? 1 : -1);
                    break;
            }
        }

        private double previousMouseDownScale = 0;
        internal virtual void MouseScale(MouseStateType mouseStateType, MouseSuperEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);
            double scale = _scale.ScaleX;
            switch (mouseStateType)
            {
                case MouseStateType.MouseDown:
                    previousMouseDownScale = scale;
                    previousMouseDownPosition = mousePosition;
                    break;
                case MouseStateType.MouseMove:
                    double deltaY = mousePosition.Y - previousMouseDownPosition.Y;
                    if (_scale.ScaleX <= minScale && deltaY < 0) return;
                    if (_scale.ScaleX >= maxScale && deltaY > 0) return;
                    scale = previousMouseDownScale + deltaY / 10 * 0.1;
                    this.Scale(scale, previousMouseDownPosition);
                    previousMouseMovePosition = mousePosition;
                    break;
                case MouseStateType.MouseUp:
                    break;
                case MouseStateType.MouseWheel:
                    if (_scale.ScaleX <= minScale && e.Delta > 0) return;
                    if (_scale.ScaleX >= maxScale && e.Delta < 0) return;
                    this.isFit = false;
                    var x = _scale.ScaleX / 10;
                    double step = x / Math.Sqrt(1 + x * x) / 2;
                    scale = _scale.ScaleX - e.Delta / 120 * step;
                    this.Scale(scale, mousePosition);
                    break;
            }
        }


        internal virtual void MouseWindowLevel(MouseStateType mouseStateType, MouseSuperEventArgs e)
        {
            if (ParentViewer == null) return;
            if (ParentViewer.DicomVolume == null) return;
            Point mousePosition = e.GetPosition(this);
            switch (mouseStateType)
            {
                case MouseStateType.MouseDown:
                    previousMouseMovePosition = mousePosition;
                    break;
                case MouseStateType.MouseMove:
                    double deltaX = mousePosition.X - previousMouseMovePosition.X;
                    double deltaY = mousePosition.Y - previousMouseMovePosition.Y;
                    double ww = (int)(this.ParentViewer.DicomVolume.WindowWidth + deltaX / 2);
                    double wc = (int)(this.ParentViewer.DicomVolume.WindowCenter + deltaY / 2);
                    if (ww < 1) ww = 1;
                    this.ParentViewer.DicomVolume.SetWidthCenter(ww, wc);
                    this.UpdateImageSource();
                    previousMouseMovePosition = mousePosition;
                    break;
                case MouseStateType.MouseUp:
                    this.ParentViewer.RefreshCells();
                    break;
            }
        }


        private List<Point> mouseDownPath = new List<Point>();
        internal virtual void MousePaintEffect(MouseStateType mouseStateType, MouseSuperEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);
            switch (mouseStateType)
            {
                case MouseStateType.MouseDown:
                    mouseDownPath.Clear();
                    mouseDownPath.Add(mousePosition);
                    break;
                case MouseStateType.MouseMove:
                    mouseDownPath.Add(mousePosition);
                    this.DrawingAnnotation();
                    break;
                case MouseStateType.MouseUp:
                    break;
            }
        }
    }
}
