using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Tsinghua.Kinect.RemotePoint
{
    class PointFiltering
    {
        Filter FilterX;
        Filter FilterY;
        Filter FilterZ;

        public PointFiltering(Filter FilterX, Filter FilterY, Filter FilterZ)
        {
            this.FilterX = FilterX;
            this.FilterY = FilterY;
            this.FilterZ = FilterZ;
        }

        public void SetValue(SpacePoint newData)
        {
            FilterX.SetValue(newData.X);
            FilterY.SetValue(newData.Y);
            FilterZ.SetValue(newData.Z);
        }

        public SpacePoint GetValue()
        {
            return new SpacePoint(FilterX.GetValue(), FilterY.GetValue(), FilterZ.GetValue());
        }

    }
}
