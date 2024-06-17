using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Sift4Net.Helpers
{
   public static class ImageHelper
   {
      public static float[,] CloneImage(float[,] imageToClone)
      {
         int height = imageToClone.GetLength(0);
         int width = imageToClone.GetLength(1);

         float[,] copy = new float[height, width];
         Array.Copy(imageToClone, copy, imageToClone.Length);

         return copy;
      }

      public static float[,] ConvertImageToGrayscale(Bitmap img)
      {
         int width = img.Width;
         int height = img.Height;
         float[,] grayscale = new float[height, width];

         BitmapData data = img.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
         int stride = data.Stride;
         byte[] pixelBuffer = new byte[stride * height];
         Marshal.Copy(data.Scan0, pixelBuffer, 0, pixelBuffer.Length);
         img.UnlockBits(data);

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               int pos = y * stride + x * 3;
               byte b = pixelBuffer[pos];
               byte g = pixelBuffer[pos + 1];
               byte r = pixelBuffer[pos + 2];
               grayscale[y, x] = (float)((0.299 * r + 0.587 * g + 0.114 * b) / 255.0);
            }
         }

         return grayscale;
      }

      public static float[,] SubtractImage(float[,] image1, float[,] image2)
      {
         int height = image1.GetLength(0);
         int width = image1.GetLength(1);

         var result = new float[height, width];

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               result[y,x] = image1[y, x] - image2[y, x];
            }
         }

         return result;
      }

      public static float[,] ResizeImageBilinearInterpolation(float[,] image, decimal resizeFactor)
      {
         int originalHeight = image.GetLength(0);
         int originalWidth = image.GetLength(1);
         int newHeight = (int)(originalHeight * resizeFactor);
         int newWidth = (int)(originalWidth * resizeFactor);

         float[,] resizedImage = new float[newHeight, newWidth];

         for (int y = 0; y < newHeight; y++)
         {
            for (int x = 0; x < newWidth; x++)
            {
               // Map coordinates from the resized image to the original image
               double gx = (double)x / (newWidth - 1) * (originalWidth - 1);
               double gy = (double)y / (newHeight - 1) * (originalHeight - 1);

               int gxi = (int)gx;
               int gyi = (int)gy;

               // Get the four surrounding pixels
               double c00 = image[gyi, gxi];
               double c10 = (gxi + 1 < originalWidth) ? image[gyi, gxi + 1] : c00;
               double c01 = (gyi + 1 < originalHeight) ? image[gyi + 1, gxi] : c00;
               double c11 = (gxi + 1 < originalWidth && gyi + 1 < originalHeight) ? image[gyi + 1, gxi + 1] : c00;

               // Calculate the weights
               double dx = gx - gxi;
               double dy = gy - gyi;

               // Perform the bilinear interpolation
               double top = c00 * (1 - dx) + c10 * dx;
               double bottom = c01 * (1 - dx) + c11 * dx;
               double value = top * (1 - dy) + bottom * dy;

               resizedImage[y, x] = (float)value;
            }
         }

         return resizedImage;
      }

      public static float[,] ResizeImageBicubic(float[,] image, decimal resizeFactor)
      {
         int originalHeight = image.GetLength(0);
         int originalWidth = image.GetLength(1);
         int newHeight = (int)(originalHeight * resizeFactor);
         int newWidth = (int)(originalWidth * resizeFactor);

         float[,] resizedImage = new float[newHeight, newWidth];

         for (int y = 0; y < newHeight; y++)
         {
            for (int x = 0; x < newWidth; x++)
            {
               double gx = (double)x / (newWidth - 1) * (originalWidth - 1);
               double gy = (double)y / (newHeight - 1) * (originalHeight - 1);

               int gxi = (int)Math.Floor(gx);
               int gyi = (int)Math.Floor(gy);

               double result = 0.0;
               for (int m = -1; m <= 2; m++)
               {
                  for (int n = -1; n <= 2; n++)
                  {
                     result += Weight(gx - (gxi + m)) * Weight(gy - (gyi + n)) * GetPixelValue(image, gxi + m, gyi + n, originalWidth, originalHeight);
                  }
               }

               resizedImage[y, x] = (float)result;
            }
         }

         return resizedImage;
      }

      private static double GetPixelValue(float[,] image, int x, int y, int width, int height)
      {
         // Handle boundary cases by clamping
         x = Math.Max(0, Math.Min(x, width - 1));
         y = Math.Max(0, Math.Min(y, height - 1));
         return image[y, x];
      }

      private static double Weight(double t)
      {
         t = Math.Abs(t);
         if (t <= 1)
         {
            return 1 - 2 * t * t + t * t * t;
         }
         else if (t < 2)
         {
            return 4 - 8 * t + 5 * t * t - t * t * t;
         }
         else
         {
            return 0;
         }
      }

      public static float[,] SmoothGaussian(float[,] image, int kernelWidth, int kernelHeight, double sigma1, double sigma2)
      {
         //var img = ConvertArrayToImage(image);
         //var smoothed = img.SmoothGaussian(kernelWidth, kernelHeight, sigma1, sigma2);
         //var conv = ImageHelper.ConvertImageToArray(smoothed);
         //return conv;

         int width = image.GetLength(0);
         int height = image.GetLength(1);

         // Calculate kernel dimensions if they are set to 0
         if (kernelWidth == 0)
         {
            kernelWidth = 2 * (int)Math.Round(sigma1 * 3) + 1;
         }
         if (kernelHeight == 0)
         {
            kernelHeight = 2 * (int)Math.Round(sigma2 * 3) + 1;
         }

         double[] kernelX = CreateGaussianKernel1D(kernelWidth, sigma1);
         double[] kernelY = CreateGaussianKernel1D(kernelHeight, sigma2);

         float[,] temp = new float[width, height];
         float[,] result = new float[width, height];

         // Apply horizontal Gaussian blur
         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               double sum = 0.0;
               double weightSum = 0.0;

               for (int k = -kernelWidth / 2; k <= kernelWidth / 2; k++)
               {
                  int xi = Clamp(x + k, 0, width - 1);
                  double weight = kernelX[k + kernelWidth / 2];
                  sum += image[xi, y] * weight;
                  weightSum += weight;
               }

               temp[x, y] = (float)(sum / weightSum);
            }
         }

         // Apply vertical Gaussian blur
         for (int x = 0; x < width; x++)
         {
            for (int y = 0; y < height; y++)
            {
               double sum = 0.0;
               double weightSum = 0.0;

               for (int k = -kernelHeight / 2; k <= kernelHeight / 2; k++)
               {
                  int yj = Clamp(y + k, 0, height - 1);
                  double weight = kernelY[k + kernelHeight / 2];
                  sum += temp[x, yj] * weight;
                  weightSum += weight;
               }

               result[x, y] = (float)(sum / weightSum);
            }
         }

         return result;
      }

      private static double[] CreateGaussianKernel1D(int size, double sigma)
      {
         double[] kernel = new double[size];
         int halfSize = size / 2;

         double sum = 0.0;
         for (int i = -halfSize; i <= halfSize; i++)
         {
            double value = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + halfSize] = value;
            sum += value;
         }

         // Normalize the kernel
         for (int i = 0; i < size; i++)
         {
            kernel[i] /= sum;
         }

         return kernel;
      }

      private static int Clamp(int value, int min, int max)
      {
         return Math.Max(min, Math.Min(max, value));
      }
   }
}
