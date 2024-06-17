using System;
namespace Sift4Net
{
   public struct DetectionData : ICloneable
   {
      public int Row;
      public int Column;
      public int Octave;
      public int Intervall;
      public double SubInterval;
      public double ScaleOctave;

      #region ICloneable Members

      public object Clone()
      {
         return new DetectionData
         {
            Column = Column,
            Intervall = Intervall,
            Octave = Octave,
            Row = Row,
            ScaleOctave = ScaleOctave,
            SubInterval = SubInterval
         };
      }

      #endregion
   }
}
