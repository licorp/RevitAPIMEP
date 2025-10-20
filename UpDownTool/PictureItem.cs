using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Quoc_MEP.UpDownTool
{
    public class PictureItem
    {
        public BitmapImage Image { get; set; }
        public string Option { get; set; }

        public PictureItem(BitmapImage bitmapImage, string option)
        {
            Image = bitmapImage;
            Option = option;
        }

    }
}
