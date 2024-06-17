using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sift4Net;
using Sift4Net.Tests;
using Sift4Net.Tests.Helpers;
using System.Drawing;
using System.Linq;

namespace NuGet.Workflow.Tests
{
   [TestClass]
   public class DemoCode
   {
      const string SEARCH_IMAGE_PATH = "C:\\ToDeleteAnyTime\\swiss-cross.jpg";
      const string TARGET_IMAGE_PATH = "C:\\ToDeleteAnyTime\\swiss-id-card_skewed2.jpg";

      [TestMethod]
      public void Test2()
      {
         var searchBitmap = new Bitmap(SEARCH_IMAGE_PATH);
         var targetBitmap = new Bitmap(TARGET_IMAGE_PATH);

         // Compute Features with the help of the SIFT Algorithm
         var sift = new Sift();
         var featuresInSearchImage = sift.FindFeatures(searchBitmap).Take(1000).ToList();
         var featuresInTargetImage = sift.FindFeatures(targetBitmap).Take(1000).ToList();

         // Match the features (see which search-feature belong to which target-features).
         var siftMatcher = new SiftMatcher();
         var matches = siftMatcher.MatchFeatures(featuresInSearchImage, featuresInTargetImage);

         // Whith the matches we can now calculate the homography
         var matchesForHomography = HomographyCalculator.GetFurthestDistanceQuadrilateral(matches);
         var homography = HomographyCalculator.CalculateHomoGraphyMatrix(matchesForHomography, HomographyType.Forward);

         // This allows us to extract a part of an image (or also the other way round, by switching the search and the target).
         var outImage = HomographyViewMapper.Map(homography, searchBitmap.Width, searchBitmap.Height, targetBitmap, InterpolationType.Bicubic);

         outImage.Save("C:\\ToDeleteAnyTime\\FinalResult.jpg");

         // The below shows both images side by side and which four matches have been selected
         using var combinedImage = BitmapHelper.Combine(searchBitmap, targetBitmap);
         BitmapHelper.DisplayKeypoints(combinedImage, 0, matchesForHomography.Select(m => m.Keypoint1));
         BitmapHelper.DisplayKeypoints(combinedImage, searchBitmap.Width, matchesForHomography.Select(m => m.Keypoint2));

         combinedImage.Save("C:\\ToDeleteAnyTime\\combinedImage.jpg");

      }
   }
}
