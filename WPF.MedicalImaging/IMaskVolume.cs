using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WPF.MedicalImaging
{
    public interface IMaskVolume
    {
        ObservableCollection<MaskObject> Masks { get; }

        bool ReverseXAxisOnRenderImage { get; set; }

        List<WriteableBitmap> GetAxialMaskBitmap(int page);

        BitmapSource GetAxialMaskBitmap2(int page);
    }
}
