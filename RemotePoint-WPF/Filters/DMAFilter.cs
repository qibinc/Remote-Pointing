using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    //  Double Moving Average Filter
    class DMAFilter : Filter
    {
        private double[] data;
        private double[] MA1;
        private double MA2;
        private int N;

        public DMAFilter(int n)
        {
            N = n;
            data = new double[2 * N + 1];
            MA1 = new double[N + 1];
        }

        public void SetValue(double newData)
        {
            for (int i = 0; i < 2 * N; i++)
                data[i] = data[i + 1];
            data[2 * N] = newData;

            for (int i = 0; i <= N; i++)
            {
                MA1[i] = 0;
                for (int j = i; j <= i + N; j++)
                    MA1[i] += data[i];
                MA1[i] /= (N + 1);
            }
            MA2 = 0;
            for (int j = 0; j <= N; j++)
                MA2 += MA1[j];
            MA2 /= (N + 1);
        }

        public double GetValue()
        {
            return 2 * MA1[N] - MA2;
        }
    }
}
