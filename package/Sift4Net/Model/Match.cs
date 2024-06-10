namespace Sift4Net.Model
{
   public class Match
   {
      public Keypoint Keypoint1 { get; set; }
      public Keypoint Keypoint2 { get; set; }
      public double Distance { get; set; }
   }
}
