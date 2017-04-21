using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Tsinghua.Kinect.RemotePoint
{
    class Plate
    {
        public SpacePoint A;
        public SpacePoint B;
        public SpacePoint C;
        public SpacePoint D;
        public SpacePoint NormalVector;
        public Plate(SpacePoint cA, SpacePoint cB, SpacePoint cC, SpacePoint cD)
        {
            A = cA;
            B = cB;
            C = cC;
            D = cD;
            //  ABC三点共线或ABCD四点不在同一平面上
            if (SpacePoint.Norm(SpacePoint.CrossProduct(B - A, C - A)) < 1e-6
                || System.Math.Abs(SpacePoint.DotProduct(SpacePoint.CrossProduct(B - A, C - A), D - A)) > 1e-6)
            {
                System.Diagnostics.Debug.WriteLine("!!!Wall ERROR!!!!");
            }

            NormalVector = SpacePoint.CrossProduct(B - A, C - A);

        }
    }
}
