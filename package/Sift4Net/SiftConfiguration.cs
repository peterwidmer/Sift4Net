namespace Sift4Net
{
   public class SiftConfiguration
   {
      public double InitialSigma { get; set; } = .5;
      public int ImageBorder { get; set; } = 3;
      public int MaximumInterpretationSteps { get; set; } = 5;

      public int OrientationHistoryBins { get; set; } = 36;
      public double OrientationRadius { get; set; }
      public double OrientationSigmaFactor { get; set; } = 1.5;
      public int OrientationSmoothPasses { get; set; } = 2;
      public double OrientationPeakRatio { get; set; } = .8;

      public double DescriptionScaleFactor { get; set; } = 3.0;
      public double DescriptionMagnitudeThreshold { get; set; } = .2;
      public double DescriptionFactor { get; set; } = 512.0;
      public int DescriptionWidth { get; set; } = 4;
      public int DescriptionHistoryBins { get; set; } = 8;

      public int Levels { get; set; } = 3;
      public double Sigma { get; set; } = 1.7;
      public double ContrastThreshold { get; set; } = 0.03;
      public int CurveThreshold { get; set; } = 10;
      
      public SiftConfiguration()
      {
         OrientationRadius = 5 * OrientationSigmaFactor;
      }

   }
}
