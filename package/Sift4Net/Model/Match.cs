namespace Sift4Net.Model
{
   public class Match
   {
      public Feature Keypoint1 { get; set; }
      public Feature Keypoint2 { get; set; }
      public double Distance { get; set; }
   }
}
