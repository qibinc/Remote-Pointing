using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    interface Filter
    {
        void SetValue(double newData);

        double GetValue();
    }
}
