using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    class NoneFilter : Filter
    {
        private double data;

        public NoneFilter(int n)
        {

        }

        public void SetValue(double newData)
        {
            data = newData;
        }
        public double GetValue()
        {
            return data;
        }
    }
}
