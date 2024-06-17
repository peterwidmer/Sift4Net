using Sift4Net.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sift4Net.Helpers
{
   public class KdTree
   {
      private KdNode root;
      private int k;

      public KdTree(int dimensions)
      {
         k = dimensions;
      }

      public void Add(double[] point, Feature value)
      {
         root = AddRecursive(root, point, value, 0);
      }

      private KdNode AddRecursive(KdNode node, double[] point, Feature value, int depth)
      {
         if (node == null)
            return new KdNode(point, value);

         int cd = depth % k;
         if (point[cd] < node.Point[cd])
            node.Left = AddRecursive(node.Left, point, value, depth + 1);
         else
            node.Right = AddRecursive(node.Right, point, value, depth + 1);

         return node;
      }

      public List<(Feature, double)> NearestNeighbors(double[] point, int count)
      {
         var bestNodes = new List<(Feature, double)>();
         NearestNeighborsRecursive(root, point, count, 0, bestNodes);
         return bestNodes.OrderBy(n => n.Item2).Take(count).ToList();
      }

      private void NearestNeighborsRecursive(KdNode node, double[] point, int count, int depth, List<(Feature, double)> bestNodes)
      {
         if (node == null) return;

         double distance = ComputeEuclideanDistance(node.Point, point);
         bestNodes.Add((node.Value, distance));
         bestNodes.Sort((x, y) => x.Item2.CompareTo(y.Item2));
         if (bestNodes.Count > count)
            bestNodes.RemoveAt(bestNodes.Count - 1);

         int cd = depth % k;
         KdNode nextBranch = null;
         KdNode oppositeBranch = null;

         if (point[cd] < node.Point[cd])
         {
            nextBranch = node.Left;
            oppositeBranch = node.Right;
         }
         else
         {
            nextBranch = node.Right;
            oppositeBranch = node.Left;
         }

         NearestNeighborsRecursive(nextBranch, point, count, depth + 1, bestNodes);
         if (Math.Abs(point[cd] - node.Point[cd]) < bestNodes.Last().Item2)
            NearestNeighborsRecursive(oppositeBranch, point, count, depth + 1, bestNodes);
      }

      private double ComputeEuclideanDistance(double[] point1, double[] point2)
      {
         double sum = 0;
         for (int i = 0; i < point1.Length; i++)
            sum += Math.Pow(point1[i] - point2[i], 2);

         return Math.Sqrt(sum);
      }

      private class KdNode
      {
         public double[] Point;
         public Feature Value;
         public KdNode Left;
         public KdNode Right;

         public KdNode(double[] point, Feature value)
         {
            Point = point;
            Value = value;
         }
      }
   }
}
