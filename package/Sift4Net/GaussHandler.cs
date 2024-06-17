using Sift4Net.Helpers;
using Sift4Net.Model;
using System;

namespace Sift4Net
{
   public static class GaussHandler
   {
      public static Images BuildGaussImages(float[,] baseimage, int octaves, int levels, double sigma)
      {
         var images = new Images(octaves);
         var sigmas = PreComputeGaussianSigmas(levels, sigma);

         for (int octave = 0; octave < octaves; octave++)
         {
            for (int level = 0; level < levels + 3; level++)
            {
               if (octave == 0 && level == 0)
               {
                  images[octave][level] = ImageHelper.CloneImage(baseimage);
               }
               else if (level == 0) /* base of new octvave is halved image from end of previous octave */
               {
                  images[octave][level] = Downsample(images[octave - 1][levels]);
               }
               else /* blur the current octave's last image to create the next one */
               {
                  images[octave][level] = ImageHelper.SmoothGaussian(images[octave][level - 1], 0, 0, sigmas[level], sigmas[level]);
               }
            }
         }

         return images;
      }

      private static double[] PreComputeGaussianSigmas(int levels, double sigma)
      {
         var sig = new double[levels + 3];

         sig[0] = sigma;
         var k = Math.Pow(2.0, 1.0 / levels);
         for (int i = 1; i < levels + 3; i++)
         {
            var sig_prev = Math.Pow(k, i - 1) * sigma;
            var sig_total = sig_prev * k;
            sig[i] = Math.Sqrt(sig_total * sig_total - sig_prev * sig_prev);
         }

         return sig;
      }

      private static float[,] Downsample(float[,] img)
      {
         var resized = ImageHelper.ResizeImageBilinearInterpolation(img, (decimal)0.5);
         return resized;
      }
   }
}
