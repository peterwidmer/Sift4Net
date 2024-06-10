using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sift4Net
{
   public static class KeyPointMatcher
   {
      public static List<Match> KnnMatch(List<Keypoint> keypoints1, List<Keypoint> keypoints2, int k = 2, double ratioThreshold = 0.75)
      {
         List<Match> matches = new List<Match>();

         foreach (var kp1 in keypoints1)
         {
            List<DistanceIndexPair> distances = new List<DistanceIndexPair>();

            for (int i = 0; i < keypoints2.Count; i++)
            {
               var kp2 = keypoints2[i];
               double distance = ComputeEuclideanDistance(kp1.Descriptor, kp2.Descriptor);
               distances.Add(new DistanceIndexPair { Distance = distance, Index = i });
            }

            distances.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            if (distances.Count >= k && distances[0].Distance < ratioThreshold * distances[1].Distance)
            {
               matches.Add(new Match { Keypoint1 = kp1, Keypoint2 = keypoints2[distances[0].Index], Distance = distances[0].Distance });
            }
         }

         return matches;
      }

      public static List<Match> GetBestMatches(List<Match> matches, int numBestMatches)
      {
         return matches.OrderBy(m => m.Distance).Take(numBestMatches).ToList();
      }

      private static double ComputeEuclideanDistance(double[] descriptor1, double[] descriptor2)
      {
         double sum = 0;
         for (int i = 0; i < descriptor1.Length; i++)
         {
            double diff = descriptor1[i] - descriptor2[i];
            sum += diff * diff;
         }
         return Math.Sqrt(sum);
      }
   }
}
