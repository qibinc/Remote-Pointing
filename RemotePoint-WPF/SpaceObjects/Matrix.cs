using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace Tsinghua.Kinect.RemotePoint
{
    class Matrix
    {
        protected double[] value;

        public int row, column;

        public Matrix(int n, int m)
        {
            this.row = n;
            this.column = m;
            if (n * m > 0)
            {
                value = new double[n * m];
                for (int i = 0; i < n * m; i++)
                    value[i] = 0;
            }
        }

        public Matrix(Matrix mat)
        {
            this.row = mat.row;
            this.column = mat.column;
            value = new double[row * column];
            mat.value.CopyTo(this.value, 0);
        }

        public static Matrix operator +(Matrix mat1, Matrix mat2)
        {
            if (mat1.row != mat2.row || mat1.column != mat2.column)
                return null;

            Matrix newMat = new Matrix(mat1.row, mat1.column);
            for (int i = 0; i < mat1.row; i++)
                for (int j = 0; j < mat1.column; j++)
                    newMat.value[i * mat1.column + j] = mat1.value[i * mat1.column + j] + mat2.value[i * mat1.column + j];
            return newMat;
        }

        public static Matrix operator -(Matrix mat1, Matrix mat2)
        {
            if (mat1.row != mat2.row || mat1.column != mat2.column)
                return null;

            Matrix newMat = new Matrix(mat1.row, mat1.column);
            for (int i = 0; i < mat1.row; i++)
                for (int j = 0; j < mat1.column; j++)
                    newMat.value[i * mat1.column + j] = mat1.value[i * mat1.column + j] - mat2.value[i * mat1.column + j];
            return newMat;
        }

        public static Matrix operator *(Matrix mat1, Matrix mat2)
        {
            if (mat1.column != mat2.row)
                return null;

            Matrix newMat = new Matrix(mat1.row, mat2.column);
            for (int i = 0; i < mat1.row; i++)
                for (int j = 0; j < mat2.column; j++)
                    for (int k = 0; k < mat1.column; k++)
                        newMat.value[i * mat2.column + j] += mat1.value[i * mat1.column + k] * mat2.value[k * mat2.column + j];
            return newMat;
        }

        public static Matrix operator *(double k, Matrix mat)
        {
            Matrix newMat = new Matrix(mat.row, mat.column);
            for (int i = 0; i < mat.row * mat.column; i++)
                    newMat.value[i] = k * mat.value[i];
            return newMat;
        }

        public double Get(int row, int col)
        {
            if (row >= 0 && row < this.row && col >= 0 && col < this.column)
                return value[row * this.column + col];
            else
                return 0;
        }

        public void Set(int row, int col, double val)
        {
            if (row >= 0 && row < this.row && col >= 0 && col < this.column)
                value[row * this.column + col] = val;
        }

        private static Matrix XRotationMatrix(double rad)
        {
            Matrix mat = new Matrix(3, 3);
            mat.Set(0, 0, 1);
            mat.Set(1, 1, System.Math.Cos(rad));
            mat.Set(1, 2, System.Math.Sin(rad));
            mat.Set(2, 1, -System.Math.Sin(rad));
            mat.Set(2, 2, System.Math.Cos(rad));
            return mat;
        }

        private static Matrix YRotationMatrix(double rad)
        {
            Matrix mat = new Matrix(3, 3);
            mat.Set(0, 0, System.Math.Cos(rad));
            mat.Set(0, 2, -System.Math.Sin(rad));
            mat.Set(1, 1, 1);
            mat.Set(2, 0, System.Math.Sin(rad));
            mat.Set(2, 2, System.Math.Cos(rad));
            return mat;
        }

        private static Matrix ZRotationMatrix(double rad)
        {
            Matrix mat = new Matrix(3, 3);
            mat.Set(0, 0, System.Math.Cos(rad));
            mat.Set(0, 1, System.Math.Sin(rad));
            mat.Set(1, 0, -System.Math.Sin(rad));
            mat.Set(1, 1, System.Math.Cos(rad));
            mat.Set(2, 2, 1);
            return mat;
        }

        public static Matrix RotationMatrix(double thetax, double thetay, double thetaz)
        {
            return ZRotationMatrix(thetaz / 180 * System.Math.PI) * YRotationMatrix(thetay / 180 * System.Math.PI) * XRotationMatrix(thetax / 180 * System.Math.PI);
        }
    }
}
