using System.Drawing;

public class Thinning
{
   public static Bitmap ZhangSuenThinning(Bitmap image)
   {
      int width = image.Width;
      int height = image.Height;

      // Convert image to binary (black and white)
      bool[,] binaryImage = new bool[width, height];
      for (int x = 0; x < width; x++)
      {
         for (int y = 0; y < height; y++)
         {
            binaryImage[x, y] = image.GetPixel(x, y).R < 200; // Black = true
         }
      }

      bool[,] marker;
      bool pixelsChanged;

      do
      {
         pixelsChanged = false;
         marker = new bool[width, height];

         // Step 1
         for (int x = 1; x < width - 1; x++)
         {
            for (int y = 1; y < height - 1; y++)
            {
               if (binaryImage[x, y] && ShouldRemovePixel(binaryImage, x, y, true))
               {
                  marker[x, y] = true;
                  pixelsChanged = true;
               }
            }
         }

         // Remove marked pixels
         for (int x = 1; x < width - 1; x++)
         {
            for (int y = 1; y < height - 1; y++)
            {
               if (marker[x, y])
                  binaryImage[x, y] = false;
            }
         }

         marker = new bool[width, height];

         // Step 2
         for (int x = 1; x < width - 1; x++)
         {
            for (int y = 1; y < height - 1; y++)
            {
               if (binaryImage[x, y] && ShouldRemovePixel(binaryImage, x, y, false))
               {
                  marker[x, y] = true;
                  pixelsChanged = true;
               }
            }
         }

         // Remove marked pixels
         for (int x = 1; x < width - 1; x++)
         {
            for (int y = 1; y < height - 1; y++)
            {
               if (marker[x, y])
                  binaryImage[x, y] = false;
            }
         }

      } while (pixelsChanged);

      // Create a new bitmap with the thinned image
      Bitmap thinnedImage = new Bitmap(width, height);
      for (int x = 0; x < width; x++)
      {
         for (int y = 0; y < height; y++)
         {
            thinnedImage.SetPixel(x, y, binaryImage[x, y] ? Color.Black : Color.White);
         }
      }

      return thinnedImage;
   }

   private static bool ShouldRemovePixel(bool[,] image, int x, int y, bool step1)
   {
      int neighborCount = CountNeighbors(image, x, y);
      int transitionCount = CountTransitions(image, x, y);

      bool p2 = image[x, y - 1];
      bool p4 = image[x - 1, y];
      bool p6 = image[x, y + 1];
      bool p8 = image[x + 1, y];

      if (neighborCount >= 2 && neighborCount <= 6 &&
          transitionCount == 1 &&
          (!step1 || (!p2 || !p4 || !p8)) &&
          (step1 || (!p2 || !p6 || !p8)))
      {
         return true;
      }

      return false;
   }

   private static int CountNeighbors(bool[,] image, int x, int y)
   {
      int count = 0;
      for (int dx = -1; dx <= 1; dx++)
      {
         for (int dy = -1; dy <= 1; dy++)
         {
            if (dx == 0 && dy == 0) continue;
            if (image[x + dx, y + dy]) count++;
         }
      }
      return count;
   }

   private static int CountTransitions(bool[,] image, int x, int y)
   {
      int count = 0;
      bool[] neighbors = {
            image[x, y - 1], // P2
            image[x + 1, y - 1], // P3
            image[x + 1, y], // P4
            image[x + 1, y + 1], // P5
            image[x, y + 1], // P6
            image[x - 1, y + 1], // P7
            image[x - 1, y], // P8
            image[x - 1, y - 1] // P9
        };

      for (int i = 0; i < neighbors.Length; i++)
      {
         if (neighbors[i] && !neighbors[(i + 1) % neighbors.Length])
            count++;
      }

      return count;
   }
}
