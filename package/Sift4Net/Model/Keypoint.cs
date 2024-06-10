namespace Sift4Net.Model
{
   public class Keypoint
   {
      public int X { get; set; }
      public int Y { get; set; }
      public int Scale { get; set; }
      public double Orientation { get; set; }
      public double[] Descriptor { get; set; }
   }
}
