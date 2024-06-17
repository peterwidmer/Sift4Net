
using HomographySharp;
using Sift4Net.Helpers;
using Sift4Net.Model;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Sift4Net.Tests
{
   public enum HomographyType
   {
      Forward,
      Backward
   }

   public static class HomographyCalculator
   {
      public static List<Match> GetFurthestDistanceQuadrilateral(List<Match> matches)
      {
         var points = matches.Select(m => new Point((int)m.Keypoint1.X, (int)m.Keypoint1.Y)).ToList();
         var res = PointDistanceHelper.FindFourFurthestPoints(points);
         var pointMatches = new List<Match>();
         foreach (var point in res)
         {
            var match = matches.First(m => (int)m.Keypoint1.X == point.X && (int)m.Keypoint1.Y == point.Y);
            pointMatches.Add(match);
         }

         return GetRectangle(pointMatches);
      }

      public static List<Match> GetRectangle(List<Match> matches)
      {
         var topMatches = (from match in matches orderby match.Keypoint1.Y ascending select match).Take(2).ToArray();
         var topLeft = topMatches[0].Keypoint1.X < topMatches[1].Keypoint1.X ? topMatches[0] : topMatches[1];
         var topRight = topMatches[0].Keypoint1.X > topMatches[1].Keypoint1.X ? topMatches[0] : topMatches[1];

         var bottomMatches = (from match in matches orderby match.Keypoint1.Y descending select match).Take(2).ToArray();
         var bottomLeft = bottomMatches[0].Keypoint1.X < bottomMatches[1].Keypoint1.X ? bottomMatches[0] : bottomMatches[1];
         var bottomRight = bottomMatches[0].Keypoint1.X > bottomMatches[1].Keypoint1.X ? bottomMatches[0] : bottomMatches[1];

         return new List<Match>
         {
            topLeft,
            topRight,
            bottomRight,
            bottomLeft
         };
      }

      public static HomographyMatrix<float> CalculateHomoGraphyMatrix(List<Match> matches, HomographyType homographyType)
      {
         DisplayPoints(matches, 500);

         var srcList = new List<Vector2>(4);
         var dstList = new List<Vector2>(4);

         foreach (var match in matches)
         {
            srcList.Add(new Vector2((float)match.Keypoint2.X, (float)match.Keypoint2.Y));
            dstList.Add(new Vector2((float)match.Keypoint1.X, (float)match.Keypoint1.Y));
         }

         if (homographyType == HomographyType.Forward)
         {
            return Homography.Find(dstList, srcList);
         }
         else
         {
            return Homography.Find(srcList, dstList);
         }
      }
      static void DisplayPoints(List<Match> matches, int shiftX)
      {
         using var img = new Bitmap(2000, 2000);
         using var g = Graphics.FromImage(img);

         int iCounter = 0;
         foreach (var match in matches)
         {
            g.DrawString($"1_{iCounter}", new Font("Tahoma", 12), Brushes.LightGray, new Point((int)match.Keypoint1.X, (int)match.Keypoint1.Y));
            g.DrawString($"2_{iCounter}", new Font("Tahoma", 12), Brushes.LightGray, new Point((int)(match.Keypoint2.X + shiftX), (int)match.Keypoint2.Y));
            iCounter++;
         }

         img.Save("C:\\Temp\\keypoints.jpg");
      }
   }
}
