using Sift4Net.Helpers;
using Sift4Net.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift4Net
{
   public class SiftMatcher
   {
      private const double LoweRatio = 0.3;

      public List<Match> MatchFeatures(List<Feature> list1, List<Feature> list2)
      {
         var matches = new List<(Feature, Feature)>();
         var forwardMatches = new List<(Feature, Feature)>();
         var backwardMatches = new List<(Feature, Feature)>();

         // Build KD-Tree for list2
         var kdTree2 = BuildKdTree(list2);

         // Forward matching using parallel processing
         Parallel.ForEach(list1, feature1 =>
         {
            var nearestNeighbors = FindTwoNearestNeighbors(feature1, kdTree2);
            if (nearestNeighbors.Item2 != null && (nearestNeighbors.Item1.distance / nearestNeighbors.Item2.distance) < LoweRatio)
            {
               lock (forwardMatches)
               {
                  forwardMatches.Add((feature1, nearestNeighbors.Item1.feature));
               }
            }
         });

         // Build KD-Tree for list1
         var kdTree1 = BuildKdTree(list1);

         // Backward matching using parallel processing
         Parallel.ForEach(list2, feature2 =>
         {
            var nearestNeighbors = FindTwoNearestNeighbors(feature2, kdTree1);
            if (nearestNeighbors.Item2 != null && (nearestNeighbors.Item1.distance / nearestNeighbors.Item2.distance) < LoweRatio)
            {
               lock (backwardMatches)
               {
                  backwardMatches.Add((feature2, nearestNeighbors.Item1.feature));
               }
            }
         });

         // Cross-check
         foreach (var match in forwardMatches)
         {
            if (backwardMatches.Exists(bm => bm.Item1 == match.Item2 && bm.Item2 == match.Item1))
            {
               matches.Add(match);
            }
         }

         // Use RANSAC to refine matches
         var refinedMatches = RansacNew.Ransac(matches);

         return refinedMatches
                     .Select(m => new Match()
                     {
                        Keypoint1 = m.Item1,
                        Keypoint2 = m.Item2,
                     })
                     .ToList();
      }

      private KdTree BuildKdTree(List<Feature> features)
      {
         var kdTree = new KdTree(128);
         foreach (var feature in features)
         {
            kdTree.Add(feature.Description, feature);
         }
         return kdTree;
      }

      private (FeatureDistance, FeatureDistance) FindTwoNearestNeighbors(Feature feature, KdTree kdTree)
      {
         var nearestNeighbors = kdTree.NearestNeighbors(feature.Description, 2);
         var result = nearestNeighbors.Select(nn => new FeatureDistance
         {
            feature = nn.Item1,
            distance = nn.Item2
         }).ToList();

         return (result[0], result.Count > 1 ? result[1] : null);
      }

      private sealed class FeatureDistance
      {
         public Feature feature;
         public double distance;
      }
   }
}
