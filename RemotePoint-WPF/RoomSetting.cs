using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsinghua.Kinect.RemotePoint
{
    class RoomSetting
    {
        //private static double MMInPixel = 1000f / 5.2f;

        private static double focalLengthInPixel = 531.15f;

        private static Matrix CameraMatrixInverse;

        private static Matrix CameraMatrix;

        private static Plate[] roomPlates = new Plate[5];

        public static void SetPlanes()
        {
            roomPlates[0] = new Plate(new SpacePoint(-2, -2, -1), new SpacePoint(-2, 2, -1),
                                      new SpacePoint(2, -2, -1), new SpacePoint(2, 2, -1));
        }

        public static void SetCameraMatrix()
        {
            CameraMatrix = new Matrix(3, 3);
            CameraMatrix.Set(0, 0, focalLengthInPixel); CameraMatrix.Set(0, 1, 0); CameraMatrix.Set(0, 2, 640 / 2);
            CameraMatrix.Set(1, 0, 0); CameraMatrix.Set(1, 1, focalLengthInPixel); CameraMatrix.Set(1, 2, 480 / 2);
            CameraMatrix.Set(2, 0, 0); CameraMatrix.Set(2, 1, 0); CameraMatrix.Set(2, 2, 1);

            CameraMatrixInverse = new Matrix(3, 3);
            CameraMatrixInverse.Set(0, 0, 0.00188271); CameraMatrixInverse.Set(0, 1, 0); CameraMatrixInverse.Set(0, 2, -0.60246635);
            CameraMatrixInverse.Set(1, 0, 0); CameraMatrixInverse.Set(1, 1, 0.00188271); CameraMatrixInverse.Set(1, 2, -0.45184976);
            CameraMatrixInverse.Set(2, 0, 0); CameraMatrixInverse.Set(2, 1, 0); CameraMatrixInverse.Set(2, 2, 1);
        }

        /// <summary>
        /// Map point from camera coordinates to room coordinates
        /// </summary>
        public static SpacePoint CameraPointToRoomPoint(SpacePoint cameraPoint)
        {
            double depth = cameraPoint.Z;
            cameraPoint.Z = 1;

            SpacePoint cameraCoordinatesPoint = new SpacePoint(CameraMatrixInverse * (depth * cameraPoint));

            //    暂时设房间坐标系为深度摄像头坐标系，右手系，单位 MM
            SpacePoint roomPoint = cameraCoordinatesPoint;
            //
            //    对cameraCoordinatesPoint进行rotate得到roompoint
            //
            //System.Diagnostics.Debug.WriteLine(roomPoint.X.ToString() + " " + roomPoint.Y.ToString() + " " + roomPoint.Z.ToString());
            return roomPoint;
        }

        /// <summary>
        /// Map point from camera coordinates to room coordinates
        /// </summary>
        public static SpacePoint RoomPointToCameraPoint(SpacePoint roomPoint)
        {
            SpacePoint cameraCoordinatesPoint = roomPoint;
            //
            //  对roomPoint进行rotate得到cameraCoordinatesMatrix
            //

            double depth = cameraCoordinatesPoint.Z;

            SpacePoint cameraPoint = new SpacePoint((1 / depth) * (CameraMatrix * cameraCoordinatesPoint));
            return cameraPoint;
        }

        /// <summary>
        /// Fine the intersection on the room's walls
        /// </summary>
        public static SpacePoint FindTheIntersection(SpacePoint start, SpacePoint end)
        {
            /*
             DepthSpaceVector dir = new DepthSpaceVector(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y, endPoint.Z - startPoint.Z);
            
            double length = System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);

            double X = endPoint.X, Y = endPoint.Y, Z = endPoint.Z;

            if (!((int)X >= 0 && (int)X < RenderWidth && (int)Y >= 0 && (int)Y < RenderHeight))
            {
                return;
            }

            //drawingContext.DrawEllipse(Brushes.LightGoldenrodYellow, null, new Point(X, Y), HandThickness, HandThickness);

            while ((int)(X + dir.X / length) >= 0 && (int)(X + dir.X / length) < RenderWidth && (int)(Y + dir.Y / length) >= 0 && (int)(Y + dir.Y / length) < RenderHeight)
            {
                X += dir.X / length;
                Y += dir.Y / length;
                Z += dir.Z / length;
                //if (wallDepthImage[RenderWidth * (int)Y + (int)X] < Z)
                if (2500 < Z)
                    break;
            }

            if ((int)(X + dir.X / length) >= 0 && (int)(X + dir.X / length) < RenderWidth && (int)(Y + dir.Y / length) >= 0 && (int)(Y + dir.Y / length) < RenderHeight)
            {
                drawingContext.DrawEllipse(Brushes.LightGoldenrodYellow, null, new Point(X, Y), HandThickness, HandThickness);

                //drawingContext.DrawEllipse(Brushes.LightGoldenrodYellow, null, new Point(RenderWidth - 1 - wallColorCoordinates[RenderWidth * (int)Y + RenderWidth - 1 - (int)X] % 640, wallColorCoordinates[RenderWidth * (int)Y + RenderWidth - 1 - (int)X] / 640), HandThickness, HandThickness);
            }
            else
            {
                this.RenderClippedEdges(new Point(X + dir.X / length, Y + dir.Y / length), drawingContext);
            }
            */
            return end;
        }

    }
}
