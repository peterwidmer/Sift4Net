using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sift4Net.Helpers
{
   public static class RansacNew
   {
      public static List<(Feature, Feature)> Ransac(List<(Feature, Feature)> matches)
      {
         const int maxIterations = 1000;
         const double distanceThreshold = 5;
         const int minInliers = 8;

         var bestInliers = new List<(Feature, Feature)>();
         var random = new Random();

         for (int iteration = 0; iteration < maxIterations; iteration++)
         {
            // Randomly select a subset of matches
            var subset = matches.OrderBy(x => random.Next()).Take(minInliers).ToList();

            // Estimate the affine transformation
            var affineMatrix = EstimateAffineTransform(subset);

            // Find inliers
            var inliers = new List<(Feature, Feature)>();
            foreach (var match in matches)
            {
               var transformedPoint = TransformPoint(match.Item1, affineMatrix);
               var distance = ComputeEuclideanDistance(transformedPoint, match.Item2.ImagePoint);

               if (distance < distanceThreshold)
               {
                  inliers.Add(match);
               }
            }

            // Update the best set of inliers
            if (inliers.Count > bestInliers.Count)
            {
               bestInliers = inliers;
            }
         }

         return bestInliers;
      }

      private static double[,] EstimateAffineTransform(List<(Feature, Feature)> matches)
      {
         int count = matches.Count;

         if (count < 3)
         {
            throw new ArgumentException("At least 3 matches are required to estimate an affine transform.");
         }

         double[,] A = new double[2 * count, 6];
         double[] B = new double[2 * count];

         for (int i = 0; i < count; i++)
         {
            var (f1, f2) = matches[i];

            A[2 * i, 0] = f1.X;
            A[2 * i, 1] = f1.Y;
            A[2 * i, 2] = 1;
            A[2 * i, 3] = 0;
            A[2 * i, 4] = 0;
            A[2 * i, 5] = 0;
            B[2 * i] = f2.X;

            A[2 * i + 1, 0] = 0;
            A[2 * i + 1, 1] = 0;
            A[2 * i + 1, 2] = 0;
            A[2 * i + 1, 3] = f1.X;
            A[2 * i + 1, 4] = f1.Y;
            A[2 * i + 1, 5] = 1;
            B[2 * i + 1] = f2.Y;
         }

         // Solve the system of linear equations A * x = B
         var x = SolveLinearSystem(A, B);

         // Construct the affine transformation matrix
         double[,] matrix = new double[3, 3];
         matrix[0, 0] = x[0];
         matrix[0, 1] = x[1];
         matrix[0, 2] = x[2];
         matrix[1, 0] = x[3];
         matrix[1, 1] = x[4];
         matrix[1, 2] = x[5];
         matrix[2, 0] = 0;
         matrix[2, 1] = 0;
         matrix[2, 2] = 1;

         return matrix;
      }

      private static double[] SolveLinearSystem(double[,] A, double[] B)
      {
         int rows = A.GetLength(0);
         int cols = A.GetLength(1);

         double[,] augmentedMatrix = new double[rows, cols + 1];

         // Form the augmented matrix [A|B]
         for (int i = 0; i < rows; i++)
         {
            for (int j = 0; j < cols; j++)
            {
               augmentedMatrix[i, j] = A[i, j];
            }
            augmentedMatrix[i, cols] = B[i];
         }

         // Perform Gaussian elimination
         for (int i = 0; i < Math.Min(rows, cols); i++)
         {
            // Search for maximum in this column
            double maxEl = Math.Abs(augmentedMatrix[i, i]);
            int maxRow = i;
            for (int k = i + 1; k < rows; k++)
            {
               if (Math.Abs(augmentedMatrix[k, i]) > maxEl)
               {
                  maxEl = Math.Abs(augmentedMatrix[k, i]);
                  maxRow = k;
               }
            }

            // Swap maximum row with current row (column by column)
            for (int k = i; k < cols + 1; k++)
            {
               double tmp = augmentedMatrix[maxRow, k];
               augmentedMatrix[maxRow, k] = augmentedMatrix[i, k];
               augmentedMatrix[i, k] = tmp;
            }

            // Make all rows below this one 0 in current column
            for (int k = i + 1; k < rows; k++)
            {
               double c = -augmentedMatrix[k, i] / augmentedMatrix[i, i];
               for (int j = i; j < cols + 1; j++)
               {
                  if (i == j)
                  {
                     augmentedMatrix[k, j] = 0;
                  }
                  else
                  {
                     augmentedMatrix[k, j] += c * augmentedMatrix[i, j];
                  }
               }
            }
         }

         // Solve equation for an upper triangular matrix
         double[] x = new double[cols];
         for (int i = cols - 1; i >= 0; i--)
         {
            x[i] = augmentedMatrix[i, cols] / augmentedMatrix[i, i];
            for (int k = i - 1; k >= 0; k--)
            {
               augmentedMatrix[k, cols] -= augmentedMatrix[k, i] * x[i];
            }
         }

         return x;
      }

      private static PointF TransformPoint(Feature feature, double[,] matrix)
      {
         double x = feature.X;
         double y = feature.Y;

         double newX = matrix[0, 0] * x + matrix[0, 1] * y + matrix[0, 2];
         double newY = matrix[1, 0] * x + matrix[1, 1] * y + matrix[1, 2];

         return new PointF((float)newX, (float)newY);
      }

      private static double ComputeEuclideanDistance(PointF point1, PointF point2)
      {
         double dx = point1.X - point2.X;
         double dy = point1.Y - point2.Y;
         return Math.Sqrt(dx * dx + dy * dy);
      }
   }
}
