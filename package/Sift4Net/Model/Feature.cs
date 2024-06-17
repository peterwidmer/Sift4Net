using System;
using System.Drawing;

namespace Sift4Net.Model
{
   public class Feature : ICloneable
   {
      public double X { get; set; }
      public double Y { get; set; }
      public double AffineRegionA { get; set; }
      public double AffineRegionB { get; set; }
      public double AffineRegionC { get; set; }
      public double Scale { get; set; }
      public double Orientation { get; set; }
      public double[] Description { get; set; } = new double[128];
      public PointF ImagePoint { get; set; }
      public DetectionData FeatureData;

      #region ICloneable Members

      public object Clone()
      {
         return new Feature
         {
            AffineRegionA = AffineRegionA,
            AffineRegionB = AffineRegionB,
            AffineRegionC = AffineRegionC,
            Description = (double[])Description.Clone(),
            FeatureData = (DetectionData)FeatureData.Clone(),
            ImagePoint = ImagePoint,
            Orientation = Orientation,
            Scale = Scale,
            X = X,
            Y = Y
         };
      }

      #endregion
   }
}
