using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace Tsinghua.Kinect.RemotePoint
{
    class RoomSetting
    {
        //private static double MMInPixel = 1000f / 5.2f;
        private static double focalLengthInPixel = 531.15f;

        private static Matrix CameraMatrix;
        private static Matrix CameraMatrixInverse;

        private static Plate[] roomPlates = new Plate[5];

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

        public static void SetPlanes()
        {
            roomPlates[0] = new Plate(new SpacePoint(-2000, -2000, -1000), new SpacePoint(-2000, 2000, -1000),
                                      new SpacePoint(2000, 2000, -1000), new SpacePoint(2000, -2000, -1000));
            roomPlates[1] = new Plate(new SpacePoint(2000, 2000, -1000), new SpacePoint(-2000, 2000, -1000),
                                      new SpacePoint(-2000, 2000, 3000), new SpacePoint(2000, 2000, 3000));
            roomPlates[2] = new Plate(new SpacePoint(2000, -2000, -1000), new SpacePoint(2000, 2000, -1000),
                                      new SpacePoint(2000, 2000, 3000), new SpacePoint(2000, -2000, 3000));
            roomPlates[3] = new Plate(new SpacePoint(-2000, -2000, -1000), new SpacePoint(-2000, 2000, -1000),
                                      new SpacePoint(-2000, 2000, 3000), new SpacePoint(-2000, -2000, 3000));

        }

        public static void PaintPlanesAndCoordinates(DrawingContext dc)
        {
            //  画坐标系
            dc.DrawLine(new Pen(Brushes.LightGoldenrodYellow, 3), RoomPointToObserveScreenPoint(new SpacePoint(0, 0, 0)), RoomPointToObserveScreenPoint(new SpacePoint(1000, 0, 0)));
            dc.DrawLine(new Pen(Brushes.LightGoldenrodYellow, 3), RoomPointToObserveScreenPoint(new SpacePoint(0, 0, 0)), RoomPointToObserveScreenPoint(new SpacePoint(0, 1000, 0)));
            dc.DrawLine(new Pen(Brushes.LightGoldenrodYellow, 3), RoomPointToObserveScreenPoint(new SpacePoint(0, 0, 0)), RoomPointToObserveScreenPoint(new SpacePoint(0, 0, 1000)));
        
            //  画板块
            foreach (Plate plate in roomPlates)
                if (plate != null)
                {
                    dc.DrawLine(new Pen(Brushes.Blue, 3), RoomPointToObserveScreenPoint(plate.A), RoomPointToObserveScreenPoint(plate.B));
                    dc.DrawLine(new Pen(Brushes.Blue, 3), RoomPointToObserveScreenPoint(plate.B), RoomPointToObserveScreenPoint(plate.C));
                    dc.DrawLine(new Pen(Brushes.Blue, 3), RoomPointToObserveScreenPoint(plate.C), RoomPointToObserveScreenPoint(plate.D));
                    dc.DrawLine(new Pen(Brushes.Blue, 3), RoomPointToObserveScreenPoint(plate.D), RoomPointToObserveScreenPoint(plate.A));
                }
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
        /// Map point from room coordinates to observe window
        /// </summary>
        public static Point RoomPointToObserveScreenPoint(SpacePoint roomPoint)
        {
            //  set observe matrix
            SpacePoint ObservePoint = new SpacePoint(0, 0, 0);
            //
            //  对roomPoint进行rotate得到observe matrix
            //

            ObservePoint.X = -roomPoint.X - 1000;
            ObservePoint.Y = roomPoint.Y - 1000;
            ObservePoint.Z = -roomPoint.Z + 5000;

            double depth = ObservePoint.Z;

            SpacePoint ObserveScreenPoint = new SpacePoint((1 / depth) * (CameraMatrix * ObservePoint));
            return new Point(ObserveScreenPoint.X, ObserveScreenPoint.Y);
        }

        /// <summary>
        /// Fine the intersection on the room's walls
        /// </summary>
        public static SpacePoint FindTheIntersection(SpacePoint start, SpacePoint end)
        {
            foreach (Plate plate in roomPlates)
                if (plate != null)
                {
                    SpacePoint normal = plate.NormalVector;

                    double t = -((start.X - plate.A.X) * normal.X + (start.Y - plate.A.Y) * normal.Y + (start.Z - plate.A.Z) * normal.Z)
                                / ((end - start).X * normal.X + (end - start).Y * normal.Y + (end - start).Z * normal.Z);

                    SpacePoint intersection = start + t * (end - start);

                    //System.Diagnostics.Debug.WriteLine(intersection.X.ToString() + " " + intersection.Y.ToString() + " " + intersection.Z.ToString());

                    return intersection;
                }
            return null;
        }

    }
}
