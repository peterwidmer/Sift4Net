using System.Linq;

namespace Sift4Net.Helpers
{
   using System;

   public class SingularValueDecomposition
   {
      public double[,] U { get; private set; }
      public double[,] S { get; private set; }
      public double[,] V { get; private set; }

      public SingularValueDecomposition(double[,] A)
      {
         Decompose(A);
      }

      private void Decompose(double[,] A)
      {
         int m = A.GetLength(0);
         int n = A.GetLength(1);

         // Step 1: Initialize U, S, V
         U = new double[m, m];
         S = new double[m, n];
         V = new double[n, n];

         double[] e = new double[n];
         double[] work = new double[m];

         double[,] B = (double[,])A.Clone();
         double[,] C = new double[m, n];

         // Step 2: Reduce A to bidiagonal form using Householder transformations
         for (int k = 0; k < Math.Min(m, n); k++)
         {
            double[] a = new double[m - k];
            for (int i = k; i < m; i++)
            {
               a[i - k] = B[i, k];
            }

            double norm = Norm(a);
            if (B[k, k] < 0)
            {
               norm = -norm;
            }

            for (int i = k; i < m; i++)
            {
               B[i, k] /= norm;
            }
            B[k, k] += 1;

            for (int j = k + 1; j < n; j++)
            {
               double sum = 0;
               for (int i = k; i < m; i++)
               {
                  sum += B[i, k] * B[i, j];
               }

               for (int i = k; i < m; i++)
               {
                  B[i, j] -= sum * B[i, k];
               }
            }

            for (int i = 0; i < m; i++)
            {
               U[i, k] = B[i, k];
            }

            for (int j = k + 1; j < n; j++)
            {
               double[] b = new double[n - j];
               for (int i = j; i < n; i++)
               {
                  b[i - j] = B[k, i];
               }

               norm = Norm(b);
               if (B[k, j] < 0)
               {
                  norm = -norm;
               }

               for (int i = j; i < n; i++)
               {
                  B[k, i] /= norm;
               }
               B[k, j] += 1;

               for (int i = k + 1; i < m; i++)
               {
                  double sum = 0;
                  for (int l = j; l < n; l++)
                  {
                     sum += B[k, l] * B[i, l];
                  }

                  for (int l = j; l < n; l++)
                  {
                     B[i, l] -= sum * B[k, l];
                  }
               }

               for (int i = 0; i < n; i++)
               {
                  V[i, j] = B[k, i];
               }
            }
         }

         // Step 3: Extract the singular values and form the matrix S
         for (int i = 0; i < Math.Min(m, n); i++)
         {
            S[i, i] = U[i, i];
         }

         // Step 4: Normalize U and V
         for (int i = 0; i < m; i++)
         {
            double norm = 0;
            for (int j = 0; j < m; j++)
            {
               norm += U[j, i] * U[j, i];
            }
            norm = Math.Sqrt(norm);

            for (int j = 0; j < m; j++)
            {
               U[j, i] /= norm;
            }
         }

         for (int i = 0; i < n; i++)
         {
            double norm = 0;
            for (int j = 0; j < n; j++)
            {
               norm += V[j, i] * V[j, i];
            }
            norm = Math.Sqrt(norm);

            for (int j = 0; j < n; j++)
            {
               V[j, i] /= norm;
            }
         }
      }

      private double Norm(double[] a)
      {
         double sum = 0;
         foreach (var val in a)
         {
            sum += val * val;
         }
         return Math.Sqrt(sum);
      }
   }

   public static class MatrixExtensions
   {
      public static double[] GetColumn(this double[,] matrix, int columnNumber)
      {
         return Enumerable.Range(0, matrix.GetLength(0))
                          .Select(x => matrix[x, columnNumber])
                          .ToArray();
      }
   }

}
