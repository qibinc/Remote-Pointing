using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    class MedianFilter : Filter
    {
        private double[] data;
        private double[] sortedData;
        private int N;
        private int cur;
        public MedianFilter(int n)
        {
            N = n;
            data = new double[N];
            sortedData = new double[N];
            cur = 0;
        }

        public void SetValue(double newData)
        {
            data[cur] = newData;
            cur = (cur + 1) % N;
        }
        public double GetValue()
        {
            data.CopyTo(sortedData, 0);
            Array.Sort(sortedData);
            return sortedData[N / 2];
        }

    }
}
