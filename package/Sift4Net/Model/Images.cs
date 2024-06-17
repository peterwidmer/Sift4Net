using System.Collections.Generic;

namespace Sift4Net.Model
{
   public class Images
   {
      public List<Octave> Octaves { get; set; } = [];

      public Images(int octaves)
      {
         for(int i = 0; i < octaves; i++)
         {
            Octaves.Add(new Octave());
         }
      }

      public Octave this[int i]
      {
         get { return Octaves[i]; }
         set { Octaves[i] = value; }
      }
   }
}
