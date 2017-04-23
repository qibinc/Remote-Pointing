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

        public bool Active;

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

            NormalVector = SpacePoint.CrossProduct(B - A, D - A);

            Active = false;

            if (SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(B - A, C - A)) * SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(D - A, C - A)) > 0)
            {
                SpacePoint t = C;
                C = D;
                D = t;
            }
        }

        public bool InsidePlate(SpacePoint point)
        {
            return SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(B - A, point - A)) * SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(D - A, point - A)) < 0
                && SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(A - B, point - B)) * SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(C - B, point - B)) < 0
                && SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(B - C, point - C)) * SpacePoint.DotProduct(NormalVector, SpacePoint.CrossProduct(D - C, point - C)) < 0;
        }
    }
}
