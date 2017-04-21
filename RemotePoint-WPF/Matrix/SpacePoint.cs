using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    class SpacePoint
    {
        public double X;
        public double Y;
        public double Z;

        public SpacePoint(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public SpacePoint(Matrix mat)
        {
            if (mat.column == 1 && mat.row == 3)
            {
                this.X = mat.Get(0, 0);
                this.Y = mat.Get(1, 0);
                this.Z = mat.Get(2, 0);
            }
        }
    }
}
