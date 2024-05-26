using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.MedicalImaging
{

    public class MedicalViewerBase : UserControl
    {
        public MedicalViewerBase()
        {
            this.Loaded += MedicalViewerBase_Loaded;
        }

        private void MedicalViewerBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Cells == null) return;
            foreach (var cell in this.Cells)
            {
                cell.MouseDown += (ss, ee) => this.OnCellMouseDown(new MedicalViewerCellMouseButtonEventArgs(ss as MedicalViewerCell, ee));
                cell.MouseUp += (ss, ee) => this.OnCellMouseUp(new MedicalViewerCellMouseButtonEventArgs(ss as MedicalViewerCell, ee));
                cell.MouseMove += (ss, ee) => this.OnCellMouseMove(new MedicalViewerCellMouseEventArgs(ss as MedicalViewerCell, ee));
            }
        }

        public IReadOnlyList<MedicalViewerCell> Cells { get; protected set; }

        /// <summary>
        /// 请在子类的构建方法中调研此函数
        /// </summary>
        /// <param name="cells"></param>
        protected virtual void RegisteCell(IEnumerable<MedicalViewerCell> cells)
        {
            Cells = new List<MedicalViewerCell>(cells);
        }

        public virtual IDicomVolume DicomVolume { get; protected set; }

        public virtual IMaskVolume MaskVolume { get; protected set; }

        public virtual void SetVolumes(IDicomVolume dicomVolume, IMaskVolume maskVolume)
        {
            this.DicomVolume = dicomVolume;
            if (Cells != null)
            {
                foreach (MedicalViewerCell cell in Cells) cell.InitCell();
            }
            this.MaskVolume = maskVolume;
        }

        public virtual void SetD3Cell(MedicalViewerD3Cell d3Cell) { throw new NotImplementedException(); }

        public virtual void RefreshCells()
        {
            if (Cells != null)
            {
                foreach (MedicalViewerCell cell in Cells) cell.Refresh();
            }
        }

        public virtual Point3D AnchorPosition { get; set; }

        public virtual NotifyPoint3<int> AnchorSlices { get; } = new NotifyPoint3<int>();

        public virtual void SetActionType(MedicalViewerMouseButtons mouseButton, MedicalViewerActionType actionType)
        {
            if (Cells != null)
            {
                foreach (MedicalViewerCell cell in Cells) cell.SetActionType(mouseButton, actionType);
            }
        }

        public event MedicalViewerCellMouseButtonEventHandler CellMouseUp;
        public event MedicalViewerCellMouseButtonEventHandler CellMouseDown;
        public event MedicalViewerCellMouseEventHandler CellMouseMove;

        public virtual void ClearEventHandler(MedicalViewerEventType eventType)
        {
            if (eventType == MedicalViewerEventType.None) return;
            switch (eventType)
            {
                case MedicalViewerEventType.CellMouseUp: this.CellMouseUp = null; break;
                case MedicalViewerEventType.CellMouseDown: this.CellMouseDown = null; break;
                case MedicalViewerEventType.CellMouseMove: this.CellMouseMove = null; break;
            }
        }

        protected virtual void OnCellMouseDown(MedicalViewerCellMouseButtonEventArgs e)
        {
            this.CellMouseDown?.Invoke(this, e);
        }

        protected virtual void OnCellMouseUp(MedicalViewerCellMouseButtonEventArgs e)
        {
            this.CellMouseUp?.Invoke(this, e);
        }

        protected virtual void OnCellMouseMove(MedicalViewerCellMouseEventArgs e)
        {
            this.CellMouseMove?.Invoke(this, e);
        }

    }

    public class MedicalViewerCellMouseButtonEventArgs : MouseButtonEventArgs
    {
        public MedicalViewerCellMouseButtonEventArgs(MedicalViewerCell cell, MouseButtonEventArgs args)
            : base(args.MouseDevice, args.Timestamp, args.ChangedButton)
        {
            this._cell = cell;
        }

        protected MedicalViewerCell _cell;
        public MedicalViewerCell Cell => _cell;

    }

    public class MedicalViewerCellMouseEventArgs : MouseEventArgs
    {
        public MedicalViewerCellMouseEventArgs(MedicalViewerCell cell, MouseEventArgs args)
            : base(args.MouseDevice, args.Timestamp)
        {
            this._cell = cell;
        }

        protected MedicalViewerCell _cell;
        public MedicalViewerCell Cell => _cell;

    }

    public delegate void MedicalViewerCellMouseButtonEventHandler(object sender, MedicalViewerCellMouseButtonEventArgs e);

    public delegate void MedicalViewerCellMouseEventHandler(object sender, MedicalViewerCellMouseEventArgs e);

    public enum MedicalViewerEventType
    {
        None,
        CellMouseDown,
        CellMouseUp,
        CellMouseMove,
    }
}
