using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sift4Net
{
   public static class SiftAlgorithm
   {
      /// <summary>
      /// Searches keypoints in the image by using the SIFT-Algorithm
      /// </summary>
      /// <param name="grayscaleImage">Two-dimensional representation of the grayscale-image.</param>
      /// <returns></returns>
      public static List<Keypoint> ComputeSIFTKeypoints(double[,] grayscaleImage)
      {
         // Parameters for SIFT
         int octaves = 4;
         int scales = 5;
         double sigma = 1.6;
         double[,] initialBlurredImage = GaussianBlur(grayscaleImage, sigma);

         List<Keypoint> keypoints = new List<Keypoint>();

         for (int o = 0; o < octaves; o++)
         {
            double[,] currentImage = initialBlurredImage;
            List<double[,]> blurredImages = new List<double[,]>();
            List<double[,]> DoGImages = new List<double[,]>();

            // Create blurred images and DoG images for current octave
            for (int s = 0; s < scales + 1; s++)
            {
               double[,] blurredImage = GaussianBlur(currentImage, sigma * Math.Pow(2, s / (double)scales));
               blurredImages.Add(blurredImage);

               if (s > 0)
               {
                  double[,] DoGImage = ComputeDoG(blurredImages[s - 1], blurredImage);
                  DoGImages.Add(DoGImage);
               }

               currentImage = blurredImage;
            }

            // Find extrema (keypoints)
            keypoints.AddRange(FindExtrema(DoGImages));

            initialBlurredImage = Downsample(blurredImages[scales]);
         }

         // Refine keypoints
         keypoints = RefineKeypoints(keypoints, grayscaleImage);

         // Assign orientation and compute descriptors
         foreach (var keypoint in keypoints)
         {
            AssignOrientation(keypoint, grayscaleImage);
            keypoint.Descriptor = ComputeDescriptor(keypoint, grayscaleImage);
         }

         return keypoints;
      }

      static double[,] GaussianBlur(double[,] image, double sigma)
      {
         int size = (int)(6 * sigma + 1);
         if (size % 2 == 0) size++;
         double[,] kernel = new double[size, size];
         double sum = 0;
         int half = size / 2;
         double sigma2 = sigma * sigma;

         for (int y = -half; y <= half; y++)
         {
            for (int x = -half; x <= half; x++)
            {
               double value = Math.Exp(-(x * x + y * y) / (2 * sigma2)) / (2 * Math.PI * sigma2);
               kernel[x + half, y + half] = value;
               sum += value;
            }
         }

         for (int y = 0; y < size; y++)
         {
            for (int x = 0; x < size; x++)
            {
               kernel[x, y] /= sum;
            }
         }

         return Convolve(image, kernel);
      }

      static double[,] Convolve(double[,] image, double[,] kernel)
      {
         int width = image.GetLength(0);
         int height = image.GetLength(1);
         int kSize = kernel.GetLength(0);
         int kHalf = kSize / 2;
         double[,] result = new double[width, height];

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               double sum = 0;
               for (int ky = -kHalf; ky <= kHalf; ky++)
               {
                  for (int kx = -kHalf; kx <= kHalf; kx++)
                  {
                     int ix = x + kx;
                     int iy = y + ky;
                     if (ix >= 0 && ix < width && iy >= 0 && iy < height)
                     {
                        sum += image[ix, iy] * kernel[kx + kHalf, ky + kHalf];
                     }
                  }
               }
               result[x, y] = sum;
            }
         }

         return result;
      }

      static double[,] ComputeDoG(double[,] image1, double[,] image2)
      {
         int width = image1.GetLength(0);
         int height = image1.GetLength(1);
         double[,] DoG = new double[width, height];

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               DoG[x, y] = image2[x, y] - image1[x, y];
            }
         }

         return DoG;
      }

      static double[,] Downsample(double[,] image)
      {
         int width = image.GetLength(0) / 2;
         int height = image.GetLength(1) / 2;
         double[,] downsampled = new double[width, height];

         for (int y = 0; y < height; y++)
         {
            for (int x = 0; x < width; x++)
            {
               downsampled[x, y] = image[x * 2, y * 2];
            }
         }

         return downsampled;
      }

      static List<Keypoint> FindExtrema(List<double[,]> DoGImages)
      {
         List<Keypoint> keypoints = new List<Keypoint>();

         for (int i = 1; i < DoGImages.Count - 1; i++)
         {
            double[,] prev = DoGImages[i - 1];
            double[,] current = DoGImages[i];
            double[,] next = DoGImages[i + 1];
            int width = current.GetLength(0);
            int height = current.GetLength(1);

            for (int y = 1; y < height - 1; y++)
            {
               for (int x = 1; x < width - 1; x++)
               {
                  if (IsExtrema(x, y, prev, current, next))
                  {
                     keypoints.Add(new Keypoint { X = x, Y = y, Scale = i });
                  }
               }
            }
         }

         return keypoints;
      }

      static bool IsExtrema(int x, int y, double[,] prev, double[,] current, double[,] next)
      {
         double value = current[x, y];
         bool isMax = true;
         bool isMin = true;

         for (int ky = -1; ky <= 1; ky++)
         {
            for (int kx = -1; kx <= 1; kx++)
            {
               if (kx == 0 && ky == 0) continue;
               if (current[x + kx, y + ky] >= value) isMax = false;
               if (current[x + kx, y + ky] <= value) isMin = false;
               if (prev[x + kx, y + ky] >= value) isMax = false;
               if (prev[x + kx, y + ky] <= value) isMin = false;
               if (next[x + kx, y + ky] >= value) isMax = false;
               if (next[x + kx, y + ky] <= value) isMin = false;
            }
         }

         return isMax || isMin;
      }

      static List<Keypoint> RefineKeypoints(List<Keypoint> keypoints, double[,] image)
      {
         // Placeholder for keypoint refinement
         // For simplicity, we'll return the keypoints as-is
         return keypoints;
      }

      static void AssignOrientation(Keypoint keypoint, double[,] image)
      {
         int x = keypoint.X;
         int y = keypoint.Y;
         int radius = 8; // Radius for calculating the orientation histogram
         double[,] gradientMagnitude = new double[radius * 2 + 1, radius * 2 + 1];
         double[,] gradientOrientation = new double[radius * 2 + 1, radius * 2 + 1];

         for (int ky = -radius; ky <= radius; ky++)
         {
            for (int kx = -radius; kx <= radius; kx++)
            {
               int ix = x + kx;
               int iy = y + ky;
               if (ix >= 1 && ix < image.GetLength(0) - 1 && iy >= 1 && iy < image.GetLength(1) - 1)
               {
                  double dx = image[ix + 1, iy] - image[ix - 1, iy];
                  double dy = image[ix, iy + 1] - image[ix, iy - 1];
                  gradientMagnitude[kx + radius, ky + radius] = Math.Sqrt(dx * dx + dy * dy);
                  gradientOrientation[kx + radius, ky + radius] = Math.Atan2(dy, dx) * (180.0 / Math.PI);
               }
            }
         }

         // Compute orientation histogram
         int numBins = 36;
         double[] histogram = new double[numBins];
         for (int i = 0; i < gradientOrientation.GetLength(0); i++)
         {
            for (int j = 0; j < gradientOrientation.GetLength(1); j++)
            {
               double orientation = gradientOrientation[i, j];
               double magnitude = gradientMagnitude[i, j];
               int bin = (int)((orientation + 180.0) / 360.0 * numBins) % numBins;
               histogram[bin] += magnitude;
            }
         }

         // Find the dominant orientation
         int maxBin = histogram.ToList().IndexOf(histogram.Max());
         keypoint.Orientation = maxBin * (360.0 / numBins);
      }

      static double[] ComputeDescriptor(Keypoint keypoint, double[,] image)
      {
         int x = keypoint.X;
         int y = keypoint.Y;
         int radius = 8; // Radius for descriptor window
         int numBins = 8; // Number of bins for orientation histogram
         double[] descriptor = new double[numBins * numBins * 4];

         for (int ky = -radius; ky < radius; ky++)
         {
            for (int kx = -radius; kx < radius; kx++)
            {
               int ix = x + kx;
               int iy = y + ky;
               if (ix >= 1 && ix < image.GetLength(0) - 1 && iy >= 1 && iy < image.GetLength(1) - 1)
               {
                  double dx = image[ix + 1, iy] - image[ix - 1, iy];
                  double dy = image[ix, iy + 1] - image[ix, iy - 1];
                  double magnitude = Math.Sqrt(dx * dx + dy * dy);
                  double orientation = Math.Atan2(dy, dx) * (180.0 / Math.PI) - keypoint.Orientation;
                  if (orientation < 0) orientation += 360;
                  if (orientation >= 360) orientation -= 360;

                  int bin = (int)(orientation / 360.0 * numBins);
                  int subregionX = (kx + radius) / (radius / 2);
                  int subregionY = (ky + radius) / (radius / 2);
                  int dIdx = (subregionY * 4 + subregionX) * numBins + bin;

                  if (dIdx >= 0 && dIdx < descriptor.Length)
                  {
                     descriptor[dIdx] += magnitude;
                  }
               }
            }
         }

         // Normalize the descriptor
         double sum = descriptor.Sum();
         if (sum > 0)
         {
            for (int i = 0; i < descriptor.Length; i++)
            {
               descriptor[i] /= sum;
            }
         }

         return descriptor;
      }
   }
}
