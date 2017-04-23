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

        private static double roomLength = 6000;
        private static double roomWidth = 4000;
        private static double roomHeight = 3300;

        private static Matrix CameraMatrix;
        private static Matrix CameraMatrixInverse;
        private static Matrix ObserveMatrix;
        private static Matrix Kinect2RoomRotation;
        private static Matrix Kinect2RoomTranslation;
        private static Matrix Room2ObserveRotation;
        private static Matrix Room2ObserveTranslation;

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

            ObserveMatrix = new Matrix(3, 3);
            ObserveMatrix.Set(0, 0, focalLengthInPixel / 1.2);
            ObserveMatrix.Set(1, 1, focalLengthInPixel / 1.2);
            ObserveMatrix.Set(0, 2, 640 / 2);
            ObserveMatrix.Set(1, 2, 480 / 2);
            ObserveMatrix.Set(2, 2, 1);

            //  Kinect坐标系到房间坐标系
            Kinect2RoomRotation = Matrix.RotationMatrix(90, 0, 90);
            Kinect2RoomTranslation = new SpacePoint(roomLength / 3, roomWidth / 2, 1300);

            //  房间坐标系到观察坐标系
            Room2ObserveRotation = Matrix.RotationMatrix(-80, 0, 0) * Matrix.RotationMatrix(0, 0, 105);
            Room2ObserveTranslation = new SpacePoint(roomLength + 300, roomWidth - 500, 500);
        }

        public static void SetPlates()
        {
            //  正面
            roomPlates[0] = new Plate(new SpacePoint(0, 0, 0), new SpacePoint(0, roomWidth, 0),
                                      new SpacePoint(0, roomWidth, roomHeight), new SpacePoint(0, 0, roomHeight));

            //  地板
            roomPlates[1] = new Plate(new SpacePoint(0, 0, 0), new SpacePoint(0, roomWidth, 0),
                                      new SpacePoint(roomLength, roomWidth, 0), new SpacePoint(roomLength, 0, 0));

            //  左边墙
            roomPlates[2] = new Plate(new SpacePoint(0, 0, 0), new SpacePoint(roomLength, 0, 0),
                                      new SpacePoint(roomLength, 0, roomHeight), new SpacePoint(0, 0, roomHeight));

            //  右边墙
            roomPlates[3] = new Plate(new SpacePoint(0, roomWidth, 0), new SpacePoint(roomLength, roomWidth, 0),
                                      new SpacePoint(roomLength, roomWidth, roomHeight), new SpacePoint(0, roomWidth, roomHeight));

            //  天花板
            roomPlates[4] = new Plate(new SpacePoint(0, 0, roomHeight), new SpacePoint(0, roomWidth, roomHeight),
                                      new SpacePoint(roomLength, roomWidth, roomHeight), new SpacePoint(roomLength, 0, roomHeight));

        }

        /// <summary>
        /// Map point from camera coordinates to room coordinates
        /// </summary>
        public static SpacePoint CameraPointToRoomPoint(SpacePoint cameraPoint)
        {
            double depth = cameraPoint.Z;
            cameraPoint.Z = 1;

            SpacePoint cameraCoordinatesPoint = new SpacePoint(CameraMatrixInverse * (depth * cameraPoint));

            //    对cameraCoordinatesPoint进行旋转平移得到roompoint
            SpacePoint roomPoint = new SpacePoint(Kinect2RoomRotation * cameraCoordinatesPoint + Kinect2RoomTranslation);

            //System.Diagnostics.Debug.WriteLine(roomPoint.X.ToString() + " " + roomPoint.Y.ToString() + " " + roomPoint.Z.ToString());

            return roomPoint;
        }

        /// <summary>
        /// Map point from room coordinates to observe window
        /// </summary>
        public static Point RoomPointToObservePoint(SpacePoint roomPoint)
        {
            //  对 roomPoint 进行旋转平移得到 observe matrix
            SpacePoint ObservePoint = new SpacePoint(Room2ObserveRotation * (roomPoint - Room2ObserveTranslation));
            
            //System.Diagnostics.Debug.WriteLine(roomPoint.X.ToString() + " " + roomPoint.Y.ToString() + " " + roomPoint.Z.ToString());

            double depth = ObservePoint.Z;

            SpacePoint ObserveScreenPoint = new SpacePoint((1 / depth) * (ObserveMatrix * ObservePoint));

            //System.Diagnostics.Debug.WriteLine(roomPoint.X.ToString() + " " + roomPoint.Y.ToString() + " " + roomPoint.Z.ToString());
            //System.Diagnostics.Debug.WriteLine(ObservePoint.X.ToString() + " " + ObservePoint.Y.ToString() + " " + ObservePoint.Z.ToString());
            //System.Diagnostics.Debug.WriteLine(ObserveScreenPoint.X.ToString() + " " + ObserveScreenPoint.Y.ToString() + " " + ObserveScreenPoint.Z.ToString());
            
            return new Point(ObserveScreenPoint.X, ObserveScreenPoint.Y);
        }

        public static Point CameraPointToObservePoint(SpacePoint cameraPoint)
        {
            return RoomPointToObservePoint(CameraPointToRoomPoint(cameraPoint));
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

                    //System.Diagnostics.Debug.WriteLine(intersection.X);

                    //System.Diagnostics.Debug.WriteLine(intersection.X.ToString() + " " + intersection.Y.ToString() + " " + intersection.Z.ToString());

                    if (t > 0 && plate.InsidePlate(intersection))
                    {
                        plate.Active = true;

                        return intersection;
                    }
                }
            return null;
        }

        private static Pen ActivePlatePen = new Pen(Brushes.Red, 3);
        private static Pen InactivePlatePen = new Pen(Brushes.Blue, 3);

        private static void PaintPlate(DrawingContext dc, Plate plate, Pen pen)
        {
            if (plate != null)
            {
                dc.DrawLine(pen, RoomPointToObservePoint(plate.A), RoomPointToObservePoint(plate.B));
                dc.DrawLine(pen, RoomPointToObservePoint(plate.B), RoomPointToObservePoint(plate.C));
                dc.DrawLine(pen, RoomPointToObservePoint(plate.C), RoomPointToObservePoint(plate.D));
                dc.DrawLine(pen, RoomPointToObservePoint(plate.D), RoomPointToObservePoint(plate.A));
            }
        }

        public static void PaintPlatesAndCoordinates(DrawingContext dc)
        {
            //  画板块
            //  先画inactive 再画active
            foreach (Plate plate in roomPlates)
                if (plate != null)
                {
                    if (!plate.Active)
                    {
                        PaintPlate(dc, plate, InactivePlatePen);
                    }
                }

            foreach (Plate plate in roomPlates)
                if (plate != null)
                {
                    if (plate.Active)
                    {
                        PaintPlate(dc, plate, ActivePlatePen);
                        plate.Active = false;
                    }
                }

            //  画kinect
            dc.DrawLine(new Pen(Brushes.LightGoldenrodYellow, 2), RoomPointToObservePoint(new SpacePoint(2000, 2000, 0)), RoomPointToObservePoint(new SpacePoint(2000, 2000, 1300)));
            dc.DrawLine(new Pen(Brushes.LightGoldenrodYellow, 2), RoomPointToObservePoint(new SpacePoint(2000, 1997, 1300)), RoomPointToObservePoint(new SpacePoint(2000, 2003, 1300)));
            dc.DrawEllipse(Brushes.LightGoldenrodYellow, new Pen(Brushes.LightGoldenrodYellow, 2), RoomPointToObservePoint(new SpacePoint(2000, 2000, 1306)), 3, 3);

        }

    }
}
