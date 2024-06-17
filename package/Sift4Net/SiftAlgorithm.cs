using Sift4Net.Helpers;
using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Sift4Net
{
   public class Sift(SiftConfiguration config = null)
   {
      private readonly SiftConfiguration _config = config ?? new SiftConfiguration();

      public List<Feature> FindFeatures(Bitmap bmp)
      {
         var initialImageArray = CreateInitialImage(bmp, _config.Sigma, _config);

         var octaves = (int)(Math.Log(Math.Min(initialImageArray.GetLength(1), initialImageArray.GetLength(0))) / Math.Log(2) - 2);

         var gaussImages = GaussHandler.BuildGaussImages(initialImageArray, octaves, _config.Levels, _config.Sigma);

         var differenceOfGaussImages = BuildDifferenceOfGauss(gaussImages, octaves, _config.Levels);

         var features = ScaleSpaceExtrema(differenceOfGaussImages, octaves, _config.ImageBorder, _config.ContrastThreshold, _config.CurveThreshold, _config);

         CalculateFeatureScales(features, _config.Sigma, _config.Levels);

         AdjustForDoubleImageSize(features);

         CalculateFeatureOrientation(features, gaussImages, _config);

         ComputeDescriptors(features, gaussImages, _config);

         features.Sort(new Comparison<Feature>(delegate (Feature a, Feature b) { if (a.Scale < b.Scale) return 1; return a.Scale > b.Scale ? -1 : 0; }));

         return features;
      }

      private void ComputeDescriptors(List<Feature> features, Images images, SiftConfiguration config)
      {
         for (var i = 0; i < features.Count; i++)
         {
            var feature = features[i];
            var featureData = feature.FeatureData;

            var image = images[featureData.Octave][featureData.Intervall];
            var history = CalculateHistory(image, featureData.Row, featureData.Column, feature.Orientation, featureData.ScaleOctave, config);
            feature.Description = CalculateDescription(history, config);
         }
      }

      private double[] CalculateDescription(float[,,] history, SiftConfiguration config)
      {
         int k = 0;
         var description = new double[config.DescriptionWidth * config.DescriptionWidth * config.DescriptionHistoryBins];

         for (var r = 0; r < config.DescriptionWidth; r++)
         {
            for (var c = 0; c < config.DescriptionWidth; c++)
            {
               for (var o = 0; o < config.DescriptionHistoryBins; o++)
               {
                  description[k++] = history[r, c, o];
               }
            }
         }

         NormalizeDescription(ref description);

         for (var i = 0; i < k; i++)
         {
            if (description[i] > config.DescriptionMagnitudeThreshold)
            {
               description[i] = config.DescriptionMagnitudeThreshold;
            }
         }

         NormalizeDescription(ref description);

         // convert descriptor to int valued descriptor
         for (var i = 0; i < k; i++)
         {
            var intValue = (int)(config.DescriptionFactor * description[i]);
            description[i] = Math.Min(255, intValue);
         }

         return description;
      }

      private void NormalizeDescription(ref double[] description)
      {
         double len_sq = 0.0;
         for (var i = 0; i < description.Length; i++)
         {
            var cur = description[i];
            len_sq += cur * cur;
         }

         var len_inv = 1.0 / Math.Sqrt(len_sq);

         for (var i = 0; i < description.Length; i++)
         {
            description[i] *= len_inv;
         }
      }

      private float[,,] CalculateHistory(float[,] image, int row, int column, double orientation, double scaleOctave, SiftConfiguration config)
      {
         double gradientMagnitude, gradientOrientation, w, rbin, cbin;

         var history = new float[config.DescriptionWidth, config.DescriptionWidth, config.DescriptionHistoryBins];

         var PI2 = 2.0 * Math.PI;
         var cos_t = Math.Cos(orientation);
         var sin_t = Math.Sin(orientation);
         var bins_per_rad = config.DescriptionHistoryBins / PI2;
         var exp_denom = config.DescriptionWidth * config.DescriptionWidth * 0.5;
         var hist_width = config.DescriptionScaleFactor * scaleOctave;
         var radius = (int)(hist_width * Math.Sqrt(2) * (config.DescriptionWidth + 1.0) * 0.5 + 0.5);

         for (var i = -radius; i <= radius; i++)
         {
            for (var j = -radius; j <= radius; j++)
            {
               // Calculate sample's histogram array coords rotated relative to orientation.
               // Subtract 0.5 so samples that fall e.g. in the center of row 1 (i.e. r_rot = 1.5) have full weight placed in row 1 after interpolation.
               var c_rot = (j * cos_t - i * sin_t) / hist_width;
               var r_rot = (j * sin_t + i * cos_t) / hist_width;
               rbin = r_rot + config.DescriptionWidth / 2 - 0.5;
               cbin = c_rot + config.DescriptionWidth / 2 - 0.5;

               if (rbin > -1.0 && rbin < config.DescriptionWidth && cbin > -1.0 && cbin < config.DescriptionWidth)
               {
                  if (CalculateGradientMagnitudeOrientation(image, row + i, column + j, out gradientMagnitude, out gradientOrientation) != 0)
                  {
                     gradientOrientation -= orientation;
                     while (gradientOrientation < 0.0)
                     {
                        gradientOrientation += PI2;
                     }
                     while (gradientOrientation >= PI2)
                     {
                        gradientOrientation -= PI2;
                     }

                     var obin = gradientOrientation * bins_per_rad;
                     w = Math.Exp(-(c_rot * c_rot + r_rot * r_rot) / exp_denom);
                     DefineHistoryEntry(ref history, rbin, cbin, obin, gradientMagnitude * w, config.DescriptionWidth, config.DescriptionHistoryBins);
                  }
               }
            }
         }

         return history;
      }

      private void DefineHistoryEntry(ref float[,,] history, double rowBin, double columnBin, double orientationBin, double magnitude, int d, int numberOfBins)
      {
         var r0 = (int)Math.Floor(rowBin);
         var c0 = (int)Math.Floor(columnBin);
         var o0 = (int)Math.Floor(orientationBin);
         var d_r = (float)rowBin - r0;
         var d_c = (float)columnBin - c0;
         var d_o = (float)orientationBin - o0;

         
         // The entry is distributed into up to 8 bins.
         // Each entry into a bin is multiplied by a weight of 1 - d for each dimension,
         // where d is the distance from the center value of the bin measured in bin units.
         for (var r = 0; r <= 1; r++)
         {
            var rb = r0 + r;
            if (rb >= 0 && rb < d)
            {
               var v_r = (float)magnitude * (r == 0 ? 1.0F - d_r : d_r);

               for (var c = 0; c <= 1; c++)
               {
                  var cb = c0 + c;
                  if (cb >= 0 && cb < d)
                  {
                     var v_c = v_r * (c == 0 ? 1.0F - d_c : d_c);

                     for (var o = 0; o <= 1; o++)
                     {
                        var ob = (o0 + o) % numberOfBins;
                        var v_o = v_c * (o == 0 ? 1.0F - d_o : d_o);
                        history[rb, cb, ob] += v_o;
                     }
                  }
               }
            }
         }
      }

      private void CalculateFeatureOrientation(List<Feature> features, Images gaussImages, SiftConfiguration config)
      {
         for (var i = 0; i < features.Count; i++)
         {
            var feature = features[0];
            features.RemoveAt(0);
            var featureData = feature.FeatureData;

            var currentImage = gaussImages[featureData.Octave][featureData.Intervall];
            var radius = (int)Math.Round(config.OrientationRadius * featureData.ScaleOctave);
            var sigma = config.OrientationSigmaFactor * featureData.ScaleOctave;
            var history = CalculateOrientationHistory(currentImage, featureData.Row, featureData.Column, config.OrientationHistoryBins, radius, sigma);

            for (var j = 0; j < config.OrientationSmoothPasses; j++)
            {
               SmoothOrientationHistory(ref history, config.OrientationHistoryBins);
            }

            var orientationMaximum = GetMaximumOrientation(ref history, config.OrientationHistoryBins);

            AddGoodOrientationFeatures(ref features, history, config.OrientationHistoryBins, orientationMaximum * config.OrientationPeakRatio, feature);
         }
      }

      private void AddGoodOrientationFeatures(ref List<Feature> features, double[] hist, int n, double magnitudeThreshold, Feature feature)
      {
         double bin, PI2 = Math.PI * 2.0;
         int l, r, i;

         for (i = 0; i < n; i++)
         {
            l = (i == 0) ? n - 1 : i - 1;
            r = (i + 1) % n;

            if (hist[i] > hist[l] && hist[i] > hist[r] && hist[i] >= magnitudeThreshold)
            {
               bin = i + GetHistoryPeak(hist[l], hist[i], hist[r]);
               bin = (bin < 0) ? n + bin : (bin >= n) ? bin - n : bin;
               var new_feat = (Feature)feature.Clone();
               new_feat.Orientation = ((PI2 * bin) / n) - Math.PI;
               features.Add(new_feat);
            }
         }
      }

      private double GetHistoryPeak(double l, double c, double r)
      {
         return 0.5 * (l - r) / (l - 2.0 * c + r);
      }

      private double GetMaximumOrientation(ref double[] history, int n)
      {
         var maximumOrientation = history[0];
         for (int i = 1; i < n; i++)
         {
            if (history[i] > maximumOrientation)
            {
               maximumOrientation = history[i];
            }
         }
         return maximumOrientation;
      }

      private void SmoothOrientationHistory(ref double[] history, int n)
      {
         var h0 = history[0];
         var prev = history[n - 1];

         for (var i = 0; i < n; i++)
         {
            var tmp = history[i];
            history[i] = 0.25 * prev + 0.5 * history[i] + 0.25 * ((i + 1 == n) ? h0 : history[i + 1]);
            prev = tmp;
         }
      }

      private double[] CalculateOrientationHistory(float[,] image, int row, int column, int n, int radius, double sigma)
      {
         double mag, ori;
         var pi2 = Math.PI * 2.0;
         var hist = new double[n];
         var exp_denom = 2.0 * sigma * sigma;

         for (var i = -radius; i <= radius; i++)
         {
            for (var j = -radius; j <= radius; j++)
            {
               if (CalculateGradientMagnitudeOrientation(image, row + i, column + j, out mag, out ori) == 1)
               {
                  var w = Math.Exp(-(i * i + j * j) / exp_denom);
                  var bin = (int)Math.Round(n * (ori + Math.PI) / pi2);
                  bin = (bin < n) ? bin : 0;
                  hist[bin] += w * mag;
               }
            }
         }

         return hist;
      }

      private int CalculateGradientMagnitudeOrientation(float[,] image, int r, int c, out double mag, out double ori)
      {
         if (r > 0 && r < image.GetLength(0) - 1 && c > 0 && c < image.GetLength(1) - 1)
         {
            var dx = image[r, c + 1] - image[r, c - 1];
            var dy = image[r - 1, c] - image[r + 1, c];
            mag = Math.Sqrt(dx * dx + dy * dy);
            ori = Math.Atan2(dy, dx);
            return 1;
         }
         else
         {
            mag = 0;
            ori = 0;
            return 0;
         }
      }

      private void AdjustForDoubleImageSize(List<Feature> features)
      {         
         foreach (var feature in features)
         {
            var x = feature.X / 2;
            var y = feature.Y / 2;
            feature.X = x;
            feature.Y = y;
            feature.Scale /= 2;
            feature.ImagePoint = new PointF((float)x, (float)y);
         }
      }

      private void CalculateFeatureScales(List<Feature> features, double sigma, int intvls)
      {
         foreach (var feature in features)
         {
            var intervall = feature.FeatureData.Intervall + feature.FeatureData.SubInterval;
            feature.Scale = sigma * Math.Pow(2.0, feature.FeatureData.Octave + intervall / intvls);
            feature.FeatureData.ScaleOctave = sigma * Math.Pow(2.0, intervall / intvls);
         }
      }

      private float[,] CreateInitialImage(Bitmap bmp, double sigma, SiftConfiguration config)
      {
         var sigmaDifference = (float)Math.Sqrt(sigma * sigma - config.InitialSigma * config.InitialSigma * 4);

         var grayscaleImage = ImageHelper.ConvertImageToGrayscale(bmp);
         var resized = ImageHelper.ResizeImageBicubic(grayscaleImage, 2);
         var smoothed = ImageHelper.SmoothGaussian(resized, 0, 0, sigmaDifference, sigmaDifference);

         return smoothed;
      }

      private Images BuildDifferenceOfGauss(Images images, int octaves, int intvls)
      {
         var differenceOfGauss = new Images(octaves);

         for (int octave = 0; octave < octaves; octave++)
         {
            for (int intervall = 0; intervall < intvls + 2; intervall++)
            {
               var gaussImage = images[octave][intervall];
               var gaussImageNext = images[octave][intervall + 1];

               var differenceImage = ImageHelper.SubtractImage(gaussImageNext, gaussImage);
               differenceOfGauss[octave][intervall] = differenceImage;
            }
         }

         return differenceOfGauss;
      }

      private List<Feature> ScaleSpaceExtrema(Images dogImages, int octaves, int imageBorder, double contrastThreshold, int curveThreshold, SiftConfiguration config)
      {
         var features = new List<Feature>();
         double preliminaryContrastThreshold = 0.5 * contrastThreshold / config.Levels;

         for (var octave = 0; octave < octaves; octave++)
         {
            var octaveHeight = dogImages[octave][0].GetLength(0) - imageBorder;
            var octaveWidth = dogImages[octave][0].GetLength(1) - imageBorder;

            for (var level = 1; level <= config.Levels; level++)
            {
               var currentImagesDifference = dogImages[octave][level];

               for (var row = imageBorder; row < octaveHeight; row++)
               {
                  for (var column = imageBorder; column < octaveWidth; column++)
                  {
                     /* perform preliminary check on contrast */
                     var contrast = Math.Abs(currentImagesDifference[row, column]);
                     var isOverContrastThreshold = contrast > preliminaryContrastThreshold;

                     if (isOverContrastThreshold && IsExtremum(dogImages, octave, level, row, column) == 1)
                     {
                        var feature = InterpretateExtremum(dogImages, octave, level, row, column, config);
                        if (feature != null)
                        {
                           var featureData = feature.FeatureData;
                           if (IsEdge(currentImagesDifference, featureData.Row, featureData.Column, curveThreshold) == 0)
                           {
                              features.Insert(0, feature);
                           }
                        }
                     }
                  }
               }
            }
         }

         return features;
      }

      private int IsExtremum(Images differenceOfGassImages, int octave, int level, int row, int column)
      {
         float value = differenceOfGassImages[octave][level][row, column];

         /* check for maximum */
         if (value > 0)
         {
            for (var i = -1; i <= 1; i++)
            {
               for (var j = -1; j <= 1; j++)
               {
                  for (var k = -1; k <= 1; k++)
                  {
                     if (value < differenceOfGassImages[octave][level + i][row + j, column + k])
                     {
                        return 0;
                     }
                  }
               }
            }
         }

         /* check for minimum */
         else
         {
            for (var i = -1; i <= 1; i++)
            {
               for (var j = -1; j <= 1; j++)
               {
                  for (var k = -1; k <= 1; k++)
                  {
                     if (value > differenceOfGassImages[octave][level + i][row + j, column + k])
                     {
                        return 0;
                     }
                  }
               }
            }
         }

         return 1;
      }

      private Feature InterpretateExtremum(Images dogImages, int octave, int level, int r, int c, SiftConfiguration config)
      {
         double subIntervall = 0, xr = 0, xc = 0;
         int i = 0;

         while (i < config.MaximumInterpretationSteps)
         {
            InterpretationStep(dogImages, octave, level, r, c, out subIntervall, out xr, out xc);
            if (Math.Abs(subIntervall) < 0.5 && Math.Abs(xr) < 0.5 && Math.Abs(xc) < 0.5)
            {
               break;
            }

            c += (int)Math.Round(xc);
            r += (int)Math.Round(xr);
            level += (int)Math.Round(subIntervall);

            if (level < 1 || level > config.Levels ||
                c < config.ImageBorder ||
                r < config.ImageBorder ||
                c >= dogImages[octave][0].GetLength(1) - config.ImageBorder ||
                r >= dogImages[octave][0].GetLength(0) - config.ImageBorder)
            {
               return null;
            }

            i++;
         }

         /* ensure convergence of interpolation */
         if (i >= config.MaximumInterpretationSteps) { return null; }

         var x = (c + xc) * Math.Pow(2.0, octave);
         var y = (r + xr) * Math.Pow(2.0, octave);

         var feature = new Feature
         {
            X = x,
            Y = y,
            ImagePoint = new PointF((float)x, (float)y),
            FeatureData = new DetectionData()
            {
               Row = r,
               Column = c,
               Octave = octave,
               Intervall = level,
               SubInterval = subIntervall,
            }
         };

         return feature;
      }

      private void InterpretationStep(Images dogImages, int octave, int intvl, int r, int c, out double xi, out double xr, out double xc)
      {
         var previousDoGImage = dogImages[octave][intvl - 1];
         var currentDoGImage = dogImages[octave][intvl];
         var nextDoGImage = dogImages[octave][intvl + 1];

         var hessian3d = Hessian3d(previousDoGImage, currentDoGImage, nextDoGImage, r, c);

         var hessianInverted = MatrixHelper.Invert(hessian3d);

         var derive3d = Derive3d(currentDoGImage, r, c);

         var res = MatrixHelper.Multiply(hessianInverted, derive3d);
         xi = res[0,0];
         xr = res[1,0];
         xc = res[2,0];
      }

      private double[,] Derive3d(float[,] differenceOfGauss, int r, int c)
      {
         var derive3d = new double[3,1];

         derive3d[0, 0] = (differenceOfGauss[r, c + 1] - differenceOfGauss[r, c - 1]) / 2.0; // DX
         derive3d[1, 0] = (differenceOfGauss[r + 1, c] - differenceOfGauss[r - 1, c]) / 2.0; // DY
         derive3d[2, 0] = (differenceOfGauss[r, c] - differenceOfGauss[r, c]) / 2.0; // DS

         return derive3d;
      }

      private double[,] Hessian3d(float[,] previousDoG,  float[,] currentDoG, float[,] nextDoG, int r, int c)
      {
         var v = currentDoG[r, c];
         var dxx = currentDoG[r, c + 1] + currentDoG[r, c - 1] - 2 * v;
         var dyy = currentDoG[r + 1, c] + currentDoG[r - 1, c] - 2 * v;
         var dss = nextDoG[r, c] + previousDoG[r, c] - 2 * v;

         var dxy = (currentDoG[r + 1, c + 1] - currentDoG[r + 1, c - 1] - currentDoG[r - 1, c + 1] + currentDoG[r - 1, c - 1]) / 4.0;
         var dxs = (nextDoG[r, c + 1] - nextDoG[r, c - 1] - previousDoG[r, c + 1] + previousDoG[r, c - 1]) / 4.0;
         var dys = (nextDoG[r + 1, c] - nextDoG[r - 1, c] - previousDoG[r + 1, c] + previousDoG[r - 1, c]) / 4.0;

         var hessianMatrix = new double[3, 3];
         hessianMatrix[0, 0] = dxx;
         hessianMatrix[0, 1] = dxy;
         hessianMatrix[0, 2] = dxs;
         hessianMatrix[1, 0] = dxy;
         hessianMatrix[1, 1] = dyy;
         hessianMatrix[1, 2] = dys;
         hessianMatrix[2, 0] = dxs;
         hessianMatrix[2, 1] = dys;
         hessianMatrix[2, 2] = dss;

         return hessianMatrix;
      }

      private int IsEdge(float[,] dogImage, int r, int c, int curv_thr)
      {
         if (c == 0 || r == 0) { return 1; }

         /* principal curvatures are computed using the trace and det of Hessian */
         var d = dogImage[r, c];
         var dxx = dogImage[r, c + 1] + dogImage[r, c - 1] - 2 * d;
         var dyy = dogImage[r + 1, c] + dogImage[r - 1, c] - 2 * d;
         var dxy = (dogImage[r + 1, c + 1] - dogImage[r + 1, c - 1] - dogImage[r - 1, c + 1] + dogImage[r - 1, c - 1]) / 4.0;
         var tr = dxx + dyy;
         var det = dxx * dyy - dxy * dxy;

         /* negative determinant -> curvatures have different signs; reject Feature */
         if (det <= 0) { return 1; }

         if (tr * tr / det < (curv_thr + 1.0) * (curv_thr + 1.0) / curv_thr)
         {
            return 0;
         }
         return 1;
      }

   }
}
