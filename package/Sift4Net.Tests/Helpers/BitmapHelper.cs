using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Sift4Net.Tests.Helpers
{
   public static class BitmapHelper
   {
      public static double[,] ConvertToGrayscale(Bitmap img)
      {
         int width = img.Width;
         int height = img.Height;
         double[,] grayscale = new double[width, height];

         BitmapData data = img.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
         int stride = data.Stride;
         byte[] pixelBuffer = new byte[stride * height];
         Marshal.Copy(data.Scan0, pixelBuffer, 0, pixelBuffer.Length);
         img.UnlockBits(data);

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               if(x == 50 && y == 50)
               {

               }
               int pos = y * stride + x * 3;
               byte b = pixelBuffer[pos];
               byte g = pixelBuffer[pos + 1];
               byte r = pixelBuffer[pos + 2];
               grayscale[x, y] = (0.299 * r + 0.587 * g + 0.114 * b); // / 255.0;
            }
         }

         return grayscale;
      }

      public static Bitmap ConvertImageArrayToImage(double[,] image, int boostValue = 1)
      {
         int width = image.GetLength(0);  // read from file
         int height = image.GetLength(1); // read from file
         var bitmap = new Bitmap(width, height);

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               var val = image[x, y]; // * 255;
               val = val * boostValue;
               if(val < 0)
               {
                  val = val * -1;
               }

               if (val > 255)
               {
                  val = 255;
               }
               bitmap.SetPixel(x, y, Color.FromArgb(255, (int)val, (int)val, (int)val));
            }
         }

         return bitmap;
      }

      public static Bitmap Combine(Bitmap image1, Bitmap image2)
      {
         var bitmap = new Bitmap(image1.Width + image2.Width, Math.Max(image1.Height, image2.Height));
         using Graphics g = Graphics.FromImage(bitmap);

         g.DrawImage(image1, 0, 0);
         g.DrawImage(image2, image1.Width, 0);

         return bitmap;
      }


      public static void DisplayKeypoints(Bitmap img, int xShift, IEnumerable<Feature> keypoints)
      {
         using Graphics g = Graphics.FromImage(img);

         foreach (var keypoint in keypoints)
         {
            var x = keypoint.X + xShift;

            g.DrawEllipse(Pens.Red, (float)x - 2, (float)keypoint.Y - 2, 4, 4);
            //double rad = keypoint.Orientation * Math.PI / 180.0;
            //g.DrawLine(Pens.Blue, x, keypoint.Y, x + (float)(10 * Math.Cos(rad)), keypoint.Y + (float)(10 * Math.Sin(rad)));
         }
      }

      public static void BpmFast()
      {
         Bitmap bmp = new Bitmap("SomeImage");

         // Lock the bitmap's bits.  
         Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
         BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

         // Get the address of the first line.
         IntPtr ptr = bmpData.Scan0;

         // Declare an array to hold the bytes of the bitmap.
         int bytes = bmpData.Stride * bmp.Height;
         byte[] rgbValues = new byte[bytes];
         byte[] r = new byte[bytes / 3];
         byte[] g = new byte[bytes / 3];
         byte[] b = new byte[bytes / 3];

         // Copy the RGB values into the array.
         Marshal.Copy(ptr, rgbValues, 0, bytes);

         int count = 0;
         int stride = bmpData.Stride;

         for (int column = 0; column < bmpData.Height; column++)
         {
            for (int row = 0; row < bmpData.Width; row++)
            {
               b[count] = (byte)(rgbValues[(column * stride) + (row * 3)]);
               g[count] = (byte)(rgbValues[(column * stride) + (row * 3) + 1]);
               r[count++] = (byte)(rgbValues[(column * stride) + (row * 3) + 2]);
            }
         }
      }
   }
}
