using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    class SpacePoint : Matrix
    {
        public double X
        {
            get
            {
                return this.value[0];
            }
            set
            {
                this.value[0] = value;
            }
        }
        public double Y
        {
            get
            {
                return this.value[1];
            }
            set
            {
                this.value[1] = value;
            }
        }
        public double Z
        {
            get
            {
                return this.value[2];
            }
            set
            {
                this.value[2] = value;
            }
        }

        public SpacePoint(double cX, double cY, double cZ) : base(3, 1)
        {
            X = cX;
            Y = cY;
            Z = cZ;
        }

        public SpacePoint(Matrix mat) : base(3, 1)
        {
            if (mat.column == 1 && mat.row == 3)
            {
                X = mat.Get(0, 0);
                Y = mat.Get(1, 0);
                Z = mat.Get(2, 0);
            }
        }

        public static SpacePoint operator +(SpacePoint left, SpacePoint right)
        {
            return new SpacePoint(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static SpacePoint operator -(SpacePoint left, SpacePoint right)
        {
            return new SpacePoint(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static SpacePoint CrossProduct(SpacePoint left, SpacePoint right)
        {
            return new SpacePoint(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);
        }

        public static double DotProduct(SpacePoint left, SpacePoint right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

    }
}
