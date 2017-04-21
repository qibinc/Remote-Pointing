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

        //public static implicit operator SpacePoint(Matrix mat)
        //{
        //    return new SpacePoint(mat);
        //}

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
    }
}
