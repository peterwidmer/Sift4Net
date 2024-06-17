using System;
using System.Collections.Generic;
using System.Drawing;

namespace Sift4Net.Helpers
{
   public static class PointDistanceHelper
   {
      public static double CalculateDistance(Point p1, Point p2)
      {
         double dx = p1.X - p2.X;
         double dy = p1.Y - p2.Y;
         return Math.Sqrt(dx * dx + dy * dy);
      }

      public static List<Point> FindFourFurthestPoints(List<Point> points)
      {
         int n = points.Count;
         if (n < 4)
         {
            throw new ArgumentException("There must be at least 4 points.");
         }

         List<Point> bestPoints = null;
         double maxDistanceSum = double.MinValue;

         // Evaluate all combinations of four points
         for (int i = 0; i < n - 3; i++)
         {
            for (int j = i + 1; j < n - 2; j++)
            {
               for (int k = j + 1; k < n - 1; k++)
               {
                  for (int l = k + 1; l < n; l++)
                  {
                     double distanceSum =
                         CalculateDistance(points[i], points[j]) +
                         CalculateDistance(points[i], points[k]) +
                         CalculateDistance(points[i], points[l]) +
                         CalculateDistance(points[j], points[k]) +
                         CalculateDistance(points[j], points[l]) +
                         CalculateDistance(points[k], points[l]);

                     if (distanceSum > maxDistanceSum)
                     {
                        maxDistanceSum = distanceSum;
                        bestPoints = new List<Point> { points[i], points[j], points[k], points[l] };
                     }
                  }
               }
            }
         }

         return bestPoints;
      }
   }
}
