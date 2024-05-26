using FellowOakDicom;
using FellowOakDicom.Imaging.Reconstruction;
using FellowOakDicom.Media;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
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
using WPF.MedicalImaging;

namespace Examples
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Common.Logging.ILog log = Common.Logging.LogManager.GetLogger(typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();
            log.Info("打开主窗口");
            //this.d3Viewer = new Vtk3DViewer();
            //this.medicalViewer.SetD3Cell(d3Viewer);
        }

        //private Vtk3DViewer d3Viewer;
        private List<DicomFile> dicomFiles;
        private IDicomVolume dicomVolume;
        private IMaskVolume maskVolume;

        private void LoadSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                dicomFiles = new List<DicomFile>();
                DicomFileScanner fileScanner = new DicomFileScanner();
                fileScanner.FileFound += FileScanner_FileFound;
                fileScanner.Progress += FileScanner_Progress;
                fileScanner.Complete += FileScanner_Complete;
                fileScanner.Start(dialog.FileName);
            }
        }

        private void FileScanner_Complete(DicomFileScanner scanner)
        {
            if (this.dicomVolume != null)
            {
                this.dicomVolume.Dispose();
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.dicomVolume = new DicomVolume(dicomFiles.Select(file => new ImageData(file.Dataset)));
                this.maskVolume = new MaskVolume(this.dicomVolume);
                this.medicalViewer.SetVolumes(this.dicomVolume, this.maskVolume);
                //this.d3Viewer.SetMaskVolume(maskVolume);
                log.Debug($"WW:{dicomVolume.WindowWidth},WC:{dicomVolume.WindowCenter}");
            });
        }

        private void FileScanner_Progress(DicomFileScanner scanner, string directory, int count)
        {
            log.Debug($"{directory}, {count}");
        }

        private void FileScanner_FileFound(DicomFileScanner scanner, DicomFile file, string fileName)
        {
            dicomFiles.Add(file);
        }


        private void LoadMhdButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Multiselect = true;
            if (!dialog.IsFolderPicker)
            {
                dialog.Filters.Add(new CommonFileDialogFilter($"MHD文件", "*.mhd"));
                dialog.Filters.Add(new CommonFileDialogFilter("所有文件", "*.*"));
            }
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    var maskObj = new ItkMaskObject(fileName);
                    this.maskVolume.Masks.Add(maskObj);
                    log.Debug($"{fileName} loaded.");
                }
                this.medicalViewer.RefreshCells();
            }
        }


    }
}
