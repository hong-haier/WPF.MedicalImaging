using FellowOakDicom.Imaging.Reconstruction;
using FellowOakDicom;
using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.MedicalImaging
{
    public interface IDicomVolume : IDisposable
    {
        int Columns { get; }

        int Rows { get; }

        int Frames { get; }

        double PixelSpacingBetweenColumnsInSource { get; }

        double PixelSpacingBetweenRowsInSource { get; }

        double SliceThicknessInSource { get; }

        double WindowWidth { get; }

        double WindowCenter { get; }

        double RescaleIntercept { get; }

        double RescaleSlope { get; }

        DicomRange<double> PixelDataRange { get; }

        int GetNumberOfSlices(StackType stackType);

        Size GetSizeOfSlice(StackType stackType);

        void SetWidthCenter(double width, double center);

        byte[] RenderAsGray8Array(StackType stackType, int page);

        Point3D ConvertSliceToImagePixel(StackType stackType, NotifyPoint3<int> slice);

        double GetPixel(int x, int y, int z);

        short[] GetDicomPixelData(int page);

        short[] GetDicomPixelData();

        FellowOakDicom.Imaging.Mathematics.Histogram GetHistogram(int page);
    }
}
