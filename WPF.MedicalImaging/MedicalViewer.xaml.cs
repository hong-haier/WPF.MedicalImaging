using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF.MedicalImaging
{
    /// <summary>
    /// MedicalViewer.xaml 的交互逻辑
    /// </summary>
    public partial class MedicalViewer : MedicalViewerBase
    {
        public MedicalViewer()
        {
            InitializeComponent();
            base.RegisteCell(new MedicalViewerCell[]
            {
                this.axialCell, this.coronalCell, this.sagittalCell
            });
            this.AnchorSlices.Changed += AnchorSlices_Changed;
        }

        private void AnchorSlices_Changed(object sender, NotifyPoint3DChangedEventArgs e)
        {
            if (e.XChanged)
            {
                this.axialCell.DrawingImage();
                this.coronalCell.DrawingImage();
            }
            if (e.YChanged)
            {
                this.axialCell.DrawingImage();
                this.sagittalCell.DrawingImage();
            }
            if (e.ZChanged)
            {
                this.coronalCell.DrawingImage();
                this.sagittalCell.DrawingImage();
            }
        }

        protected MedicalViewerD3Cell d3Cell;
        public override void SetD3Cell(MedicalViewerD3Cell d3Cell)
        {
            this.d3Cell = d3Cell;
            d3CellPanel.Children.Add(this.d3Cell);
        }

        private void CellPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            var panel = sender as Panel;
            int childCount = VisualTreeHelper.GetChildrenCount(panel);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(panel, i);
                if (child is MedicalViewerCell cell)
                {
                    cell.Focus();
                    break;
                }
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
        }
    }
}
