using HomographySharp;
using System;
using System.Drawing;

namespace Sift4Net.Tests
{
   public static class HomographyViewMapper
   {
      public static Bitmap Map(HomographyMatrix<float> homo, int imageWidth, int imageHeight, Bitmap targetBitmap, InterpolationType interpolationType)
      {
         if (interpolationType == InterpolationType.Bicubic)
         {
            return MapBicubic(homo, imageWidth, imageHeight, targetBitmap);
         }
         else
         {
            return MapBilinear(homo, imageWidth, imageHeight, targetBitmap);
         }
      }

      private static Bitmap MapBilinear(HomographyMatrix<float> homo, int imageWidth, int imageHeight, Bitmap targetBitmap)
      {
         var outImage = new Bitmap(imageWidth, imageHeight);
         for (int x = 0; x < imageWidth; x++)
         {
            for (int y = 0; y < imageHeight; y++)
            {
               var res = homo.Translate(x, y);

               // Ensure coordinates are within bounds
               if (res.X >= 0 && res.X < targetBitmap.Width - 1 &&
                   res.Y >= 0 && res.Y < targetBitmap.Height - 1)
               {
                  // Get integer coordinates
                  int x1 = (int)res.X;
                  int y1 = (int)res.Y;

                  // Get fractional part
                  double dx = res.X - x1;
                  double dy = res.Y - y1;

                  // Get colors of the four neighboring pixels
                  Color c11 = targetBitmap.GetPixel(x1, y1);
                  Color c12 = targetBitmap.GetPixel(x1, y1 + 1);
                  Color c21 = targetBitmap.GetPixel(x1 + 1, y1);
                  Color c22 = targetBitmap.GetPixel(x1 + 1, y1 + 1);

                  // Bilinear interpolation
                  int red = (int)(
                      c11.R * (1 - dx) * (1 - dy) +
                      c21.R * dx * (1 - dy) +
                      c12.R * (1 - dx) * dy +
                      c22.R * dx * dy
                  );
                  int green = (int)(
                      c11.G * (1 - dx) * (1 - dy) +
                      c21.G * dx * (1 - dy) +
                      c12.G * (1 - dx) * dy +
                      c22.G * dx * dy
                  );
                  int blue = (int)(
                      c11.B * (1 - dx) * (1 - dy) +
                      c21.B * dx * (1 - dy) +
                      c12.B * (1 - dx) * dy +
                      c22.B * dx * dy
                  );

                  // Set the interpolated color in the output image
                  outImage.SetPixel(x, y, Color.FromArgb(red, green, blue));
               }
            }
         }

         return outImage;
      }

      private static Bitmap MapBicubic(HomographyMatrix<float> homo, int imageWidth, int imageHeight, Bitmap targetBitmap)
      {
         var outImage = new Bitmap(imageWidth, imageHeight);
         for (int x = 0; x < imageWidth; x++)
         {
            for (int y = 0; y < imageHeight; y++)
            {
               var res = homo.Translate(x, y);

               if (res.X >= 1 && res.X < targetBitmap.Width - 2 &&
                   res.Y >= 1 && res.Y < targetBitmap.Height - 2)
               {
                  // Get integer and fractional parts
                  int x1 = (int)res.X;
                  int y1 = (int)res.Y;
                  double dx = res.X - x1;
                  double dy = res.Y - y1;

                  // Get colors in a 4x4 grid of neighboring pixels
                  Color[,] neighbors = new Color[4, 4];
                  for (int i = -1; i <= 2; i++)
                  {
                     for (int j = -1; j <= 2; j++)
                     {
                        neighbors[i + 1, j + 1] = targetBitmap.GetPixel(x1 + i, y1 + j);
                     }
                  }

                  // Interpolate each color channel separately
                  int red = (int)BicubicInterpolate(neighbors, dx, dy, c => c.R);
                  int green = (int)BicubicInterpolate(neighbors, dx, dy, c => c.G);
                  int blue = (int)BicubicInterpolate(neighbors, dx, dy, c => c.B);

                  outImage.SetPixel(x, y, Color.FromArgb(red, green, blue));
               }
            }
         }

         return outImage;
      }

      // Bicubic interpolation function for a specific channel
      static double BicubicInterpolate(Color[,] neighbors, double dx, double dy, Func<Color, int> channelSelector)
      {
         double[] col = new double[4];
         for (int i = 0; i < 4; i++)
         {
            col[i] = CubicInterpolate(
                channelSelector(neighbors[i, 0]),
                channelSelector(neighbors[i, 1]),
                channelSelector(neighbors[i, 2]),
                channelSelector(neighbors[i, 3]),
                dy
            );
         }
         return Clamp(CubicInterpolate(col[0], col[1], col[2], col[3], dx));
      }

      // Cubic interpolation helper
      static double CubicInterpolate(double v0, double v1, double v2, double v3, double fraction)
      {
         double a = -0.5 * v0 + 1.5 * v1 - 1.5 * v2 + 0.5 * v3;
         double b = v0 - 2.5 * v1 + 2 * v2 - 0.5 * v3;
         double c = -0.5 * v0 + 0.5 * v2;
         double d = v1;

         return a * Math.Pow(fraction, 3) + b * Math.Pow(fraction, 2) + c * fraction + d;
      }

      // Clamp color values to valid byte range
      static double Clamp(double value)
      {
         return Math.Max(0, Math.Min(255, value));
      }
   }
}
