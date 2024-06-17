using Sift4Net.Model;
using System;
using System.Collections.Generic;

namespace Sift4Net.Helpers
{
   public static class Ransac
   {
      public static List<Match> GetGoodMatches(List<Match> matches, int iterations, double threshold)
      {
         List<Match> bestInliers = new List<Match>();
         Random rand = new Random();

         for (int iter = 0; iter < iterations; iter++)
         {
            // Randomly sample 4 matches
            List<Match> sampleMatches = new List<Match>();
            HashSet<int> selectedIndices = new HashSet<int>();
            while (sampleMatches.Count < 4)
            {
               int idx = rand.Next(matches.Count);
               if (!selectedIndices.Contains(idx))
               {
                  sampleMatches.Add(matches[idx]);
                  selectedIndices.Add(idx);
               }
            }

            // Compute homography from the sampled matches
            double[,] H = ComputeHomography(sampleMatches);

            // Count inliers
            List<Match> inliers = new List<Match>();
            foreach (var match in matches)
            {
               if (IsInlier(match, H, threshold))
               {
                  inliers.Add(match);
               }
            }

            // Update best inliers
            if (inliers.Count > bestInliers.Count)
            {
               bestInliers = inliers;
            }
         }

         return bestInliers;
      }

      private static double[,] ComputeHomography(List<Match> matches)
      {
         int numPoints = matches.Count;
         double[,] A = new double[2 * numPoints, 9];

         for (int i = 0; i < numPoints; i++)
         {
            var kp1 = matches[i].Keypoint1;
            var kp2 = matches[i].Keypoint2;

            double x = kp1.X;
            double y = kp1.Y;
            double xPrime = kp2.X;
            double yPrime = kp2.Y;

            A[2 * i, 0] = -x;
            A[2 * i, 1] = -y;
            A[2 * i, 2] = -1;
            A[2 * i, 3] = 0;
            A[2 * i, 4] = 0;
            A[2 * i, 5] = 0;
            A[2 * i, 6] = x * xPrime;
            A[2 * i, 7] = y * xPrime;
            A[2 * i, 8] = xPrime;

            A[2 * i + 1, 0] = 0;
            A[2 * i + 1, 1] = 0;
            A[2 * i + 1, 2] = 0;
            A[2 * i + 1, 3] = -x;
            A[2 * i + 1, 4] = -y;
            A[2 * i + 1, 5] = -1;
            A[2 * i + 1, 6] = x * yPrime;
            A[2 * i + 1, 7] = y * yPrime;
            A[2 * i + 1, 8] = yPrime;
         }

         // Use SVD to solve the system
         var svd = new SingularValueDecomposition(A);
         double[] h = svd.V.GetColumn(8);

         double[,] H = new double[3, 3];
         for (int i = 0; i < 3; i++)
         {
            for (int j = 0; j < 3; j++)
            {
               H[i, j] = h[i * 3 + j] / h[8];
            }
         }

         return H;
      }

      private static bool IsInlier(Match match, double[,] H, double threshold)
      {
         var kp1 = match.Keypoint1;
         var kp2 = match.Keypoint2;

         double[] p1 = { kp1.X, kp1.Y, 1.0 };
         double[] p2 = MatrixDot(H, p1);

         double x2 = p2[0] / p2[2];
         double y2 = p2[1] / p2[2];

         double distance = Math.Sqrt(Math.Pow(x2 - kp2.X, 2) + Math.Pow(y2 - kp2.Y, 2));
         return distance < threshold;
      }

      private static double[] MatrixDot(double[,] matrix, double[] vector)
      {
         if (matrix.GetLength(1) != vector.Length)
            throw new ArgumentException("Matrix and vector dimensions do not match.");

         double[] result = new double[matrix.GetLength(0)];

         for (int i = 0; i < matrix.GetLength(0); i++)
         {
            result[i] = 0;
            for (int j = 0; j < vector.Length; j++)
            {
               result[i] += matrix[i, j] * vector[j];
            }
         }

         return result;
      }

   }
}
