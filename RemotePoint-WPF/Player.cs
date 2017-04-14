using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
//using System.Windows.Controls;
using Microsoft.Kinect;

namespace Tsinghua.Kinect.RemotePoint
{
    class Player
    {
        public Skeleton skeleton;

        public KinectSensor _sensor;
        public KinectSensor sensor
        {
            get
            {
                return _sensor;
            }
            set
            {
                _sensor = value;
                painter = new Painter();
                painter.sensor = _sensor;
            }
        }

        private Painter painter;

        public bool headAndHandValid = false;

        private Point startPointInColorFrame;

        public Point endPointInColorFrame;

        public Matrix startPointInCameraCoordinates;

        public Matrix endPointInCameraCoordinates;

        private Matrix ScreenPointToCameraPoint(Point point)
        {

            return new Matrix(3, 1);
            /*
            //  Using two images, do the transformation:
            //  Camera1 coordinates -> real world coordinates -> Camera2 Coordinates
            DepthSpacePoint TransformCoordinatesFromLeftToRightImage(DepthSpacePoint point, int k)
            {
                double depth = point.Z * MMInPixel;
                Matrix leftFramePoint = new Matrix(point);
                leftFramePoint.Set(3, 1, 1);

                //if (k == 1)
                //    System.Console.WriteLine(Convert.ToString(leftFramePoint.Get(1, 1)) + " " + Convert.ToString(point.Y));
                //  left camera coordinate -> left real world coordinate
                Matrix leftWorldPoint = CameraMatrixInverse * (depth * leftFramePoint);
                //if (k == 1)
                //    System.Console.WriteLine(Convert.ToString((int) leftWorldPoint.Get(1, 1)/1000)  + " " + Convert.ToString((int)leftWorldPoint.Get(2, 1) / 1000) + " " + Convert.ToString((int)leftWorldPoint.Get(3, 1)/1000));

                //  left real world coordinate -> right real world coordiante
                Matrix rightWorldPoint = new Matrix(leftWorldPoint);
                //rightWorldPoint.Set(1, 1, -rightWorldPoint.Get(1, 1));
                rightWorldPoint.Set(3, 1, -rightWorldPoint.Get(3, 1) + 3200 * MMInPixel);

                //  right real world coordinate -> right camera coordinate
                double newdepth = rightWorldPoint.Get(3, 1);
                Matrix rightFramePoint = CameraMatrix * (1 / newdepth * rightWorldPoint);

                DepthSpacePoint newpoint = new DepthSpacePoint();
                newpoint.X = (int)rightFramePoint.Get(1, 1);
                newpoint.Y = (int)rightFramePoint.Get(2, 1);
                newpoint.Z = (int)(newdepth / MMInPixel);

                return newpoint;
            }
            */
        }

        /// <summary>
        ///  Called to update point info
        /// </summary>
        public void AnalyzeHeadAndHands()
        {
            // If we can't find either head or right hand, exit
            if (skeleton.Joints[JointType.Head].TrackingState == JointTrackingState.NotTracked ||
                skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.NotTracked)
            {
                this.headAndHandValid = false;
                return;
            }

            // Don't analyze if both points are inferred
            if (skeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Inferred &&
                skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Inferred)
            {
                this.headAndHandValid = false;
                return;
            }

            this.headAndHandValid = true;
            this.startPointInColorFrame = this.painter.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position);
            this.startPointInCameraCoordinates = ScreenPointToCameraPoint(this.startPointInColorFrame);
            this.endPointInColorFrame = this.painter.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
            this.endPointInCameraCoordinates = ScreenPointToCameraPoint(this.endPointInColorFrame);
        }

        /// <summary>
        /// Draw the sight on the screen
        /// </summary>
        public void DrawSight(DrawingContext dc)
        {
            this.painter.DrawSight(dc, startPointInColorFrame, endPointInColorFrame);
        }

        /// <summary>
        /// Draw the skeletons on the screen
        /// </summary>
        public void DrawSkeleton(DrawingContext dc)
        {
            this.painter.DrawSkeleton(dc, skeleton);
        }

    }
}
