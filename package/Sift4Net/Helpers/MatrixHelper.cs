using System;

namespace Sift4Net.Helpers
{
   public static class MatrixHelper
   {
      public static double[,] Invert(double[,] matrix, double regularization = 1e-10)
      {
         int n = matrix.GetLength(0);
         if (n != matrix.GetLength(1))
         {
            throw new ArgumentException("Matrix must be square to invert.");
         }

         // Create augmented matrix [A | I]
         double[,] augmentedMatrix = new double[n, 2 * n];
         for (int i = 0; i < n; i++)
         {
            for (int j = 0; j < n; j++)
            {
               augmentedMatrix[i, j] = matrix[i, j];
            }
            augmentedMatrix[i, i] += regularization; // Add regularization term to the diagonal
            augmentedMatrix[i, i + n] = 1; // Identity matrix part
         }

         // Perform Gaussian elimination with partial pivoting
         for (int i = 0; i < n; i++)
         {
            // Find the row with the maximum pivot element
            int maxRow = i;
            for (int k = i + 1; k < n; k++)
            {
               if (Math.Abs(augmentedMatrix[k, i]) > Math.Abs(augmentedMatrix[maxRow, i]))
               {
                  maxRow = k;
               }
            }

            // Swap the current row with the maxRow
            if (maxRow != i)
            {
               for (int j = 0; j < 2 * n; j++)
               {
                  double temp = augmentedMatrix[i, j];
                  augmentedMatrix[i, j] = augmentedMatrix[maxRow, j];
                  augmentedMatrix[maxRow, j] = temp;
               }
            }

            // Ensure pivot element is non-zero
            if (Math.Abs(augmentedMatrix[i, i]) < 1e-12) // Use a small tolerance instead of strict zero
            {
               throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
            }

            // Divide current row to make pivot element 1
            double pivot = augmentedMatrix[i, i];
            for (int j = 0; j < 2 * n; j++)
            {
               augmentedMatrix[i, j] /= pivot;
            }

            // Subtract current row from all other rows to make elements below the pivot zero
            for (int k = 0; k < n; k++)
            {
               if (k != i)
               {
                  double factor = augmentedMatrix[k, i];
                  for (int j = 0; j < 2 * n; j++)
                  {
                     augmentedMatrix[k, j] -= factor * augmentedMatrix[i, j];
                  }
               }
            }
         }

         // Extract inverse matrix [I | A^-1] from augmented matrix
         double[,] inverseMatrix = new double[n, n];
         for (int i = 0; i < n; i++)
         {
            for (int j = 0; j < n; j++)
            {
               inverseMatrix[i, j] = augmentedMatrix[i, j + n];
            }
         }

         return inverseMatrix;
      }

      /*
      public static double[,] Invert(double[,] matrix)
      {
         int n = matrix.GetLength(0);
         if (matrix.GetLength(0) != matrix.GetLength(1))
         {
            throw new ArgumentException("Matrix must be square to invert.");
         }

         // Create augmented matrix [A | I]
         double[,] augmentedMatrix = new double[n, 2 * n];
         for (int i = 0; i < n; i++)
         {
            for (int j = 0; j < n; j++)
            {
               augmentedMatrix[i, j] = matrix[i, j];
               augmentedMatrix[i, j + n] = (i == j) ? 1 : 0; // Identity matrix part
            }
         }

         // Perform Gaussian elimination to transform [A | I] into [I | A^-1]
         for (int i = 0; i < n; i++)
         {
            // Ensure pivot element is non-zero
            if (augmentedMatrix[i, i] == 0)
            {
               throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
            }

            // Divide current row to make pivot element 1
            double pivot = augmentedMatrix[i, i];
            for (int j = 0; j < 2 * n; j++)
            {
               augmentedMatrix[i, j] /= pivot;
            }

            // Subtract current row from all other rows to make elements below the pivot zero
            for (int k = 0; k < n; k++)
            {
               if (k != i)
               {
                  double factor = augmentedMatrix[k, i];
                  for (int j = 0; j < 2 * n; j++)
                  {
                     augmentedMatrix[k, j] -= factor * augmentedMatrix[i, j];
                  }
               }
            }
         }

         // Extract inverse matrix [I | A^-1] from augmented matrix
         double[,] inverseMatrix = new double[n, n];
         for (int i = 0; i < n; i++)
         {
            for (int j = 0; j < n; j++)
            {
               inverseMatrix[i, j] = augmentedMatrix[i, j + n];
            }
         }

         return inverseMatrix;
      }
      */

      public static double[,] Multiply(double[,] matrixA, double[,] matrixB)
      {
         int rowsA = matrixA.GetLength(0);
         int colsA = matrixA.GetLength(1);
         int rowsB = matrixB.GetLength(0);
         int colsB = matrixB.GetLength(1);

         if (colsA != rowsB)
         {
            throw new ArgumentException("Number of columns in matrixA must equal number of rows in matrixB.");
         }

         double[,] result = new double[rowsA, colsB];

         for (int i = 0; i < rowsA; i++)
         {
            for (int j = 0; j < colsB; j++)
            {
               result[i, j] = 0;
               for (int k = 0; k < colsA; k++)
               {
                  result[i, j] += matrixA[i, k] * matrixB[k, j];
               }
            }
         }

         return result;
      }
   }
}
