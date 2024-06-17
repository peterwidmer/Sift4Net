using System.Collections.Generic;

namespace Sift4Net.Model
{
   public class Octave
   {
      public List<float[,]> Images { get; set; } = [];

      public float[,] this[int i]
      {
         get { return Images[i]; }
         set
         {
            if(i > Images.Count -1)
            {
               Images.Add(value);
            }
            else
            {
               Images[i] = value;
            }
            
         }
      }
   }
}
