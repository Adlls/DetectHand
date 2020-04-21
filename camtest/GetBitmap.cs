using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camtest
{

    public class GetBitmap
    {
        private Bitmap bitmap;
      public GetBitmap(Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

     

        public void ConvertBitmap()
        {
            var tempBitmap = this.bitmap;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var b = tempBitmap.GetPixel(x, y);
                    //Console.WriteLine("this is pixel >>" + tempImg.GetPixel(x,y));
                    //Console.WriteLine(count);
                    //var test = tempImg.GetPixel(0, 0).R + 10;
                    //Color pixelColor = tempImg.GetPixel(x, y);
                    // var Y = 0.299*tempImg.GetPixel(x, y).R + 
                    // 0.587*tempImg.GetPixel(x, y).G + 0.114*tempImg.GetPixel(x,y).B;

                    //var Cr = tempImg.GetPixel(x, y).R - Y;

                    //var Cb = tempImg.GetPixel(x, y).B - Y;
                }
            }
        }
    }
}
