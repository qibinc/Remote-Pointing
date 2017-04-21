using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Tsinghua.Kinect.RemotePoint
{
    class FramePointFiltering
    {
        Filter FilterX;
        Filter FilterY;

        public FramePointFiltering(Filter FilterX, Filter FilterY)
        {
            this.FilterX = FilterX;
            this.FilterY = FilterY;
        }

        public void SetValue(Point newData)
        {
            FilterX.SetValue(newData.X);
            FilterY.SetValue(newData.Y);
        }

        public Point GetValue()
        {
            return new Point(FilterX.GetValue(), FilterY.GetValue());
        }
    }
}
