using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Tsinghua.Kinect.RemotePoint
{
    class Plate
    {
        SpacePoint A;
        SpacePoint B;
        SpacePoint C;
        SpacePoint D;

        public Plate(SpacePoint cA, SpacePoint cB, SpacePoint cC, SpacePoint cD)
        {
            A = cA;
            B = cB;
            C = cC;
            D = cD;
            if (System.Math.Abs(SpacePoint.DotProduct(SpacePoint.CrossProduct(B - A, C - A), D - A)) > 1e-6)
                System.Diagnostics.Debug.WriteLine("!!!Wall ERROR!!!!");

        }
    }
}
