using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace Tsinghua.Kinect.RemotePoint
{
    class Player
    {
        public Skeleton skeleton;

        public KinectSensor sensor;

        private readonly double HeadHandThickness = 4;

        private readonly double BodyCenterThickness = 10;

        private readonly Brush CenterPointBrush = Brushes.Blue;
      
        private readonly Pen TrackedBonePen = new Pen(Brushes.LightBlue, 6);

        private readonly Pen InferredBonePen = new Pen(Brushes.Gray, 2);

        private readonly Pen SightLinePen = new Pen(Brushes.LightGoldenrodYellow, 3);

        private readonly Brush HeadHandBrush = Brushes.LightGreen;
        
        public bool headAndHandTracked = false;

        private Point startPointInColorFrame
        {
            get
            {
                return this.startPointInColorFrame;
            }
            set
            {
                this.startPointInColorFrame = value;
                this.startPointInCameraCoordinates = ScreenPointToCameraPoint(this.startPointInColorFrame);
            }
        }

        private Point endPointInColorFrame
        {
            get
            {
                return this.endPointInColorFrame;
            }
            set
            {
                this.endPointInColorFrame = value;
                this.endPointInCameraCoordinates = ScreenPointToCameraPoint(this.endPointInColorFrame);
            }
        }

        private SpacePoint ScreenPointToCameraPoint(Point point)
        {

            return new Point(0, 0);
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

        public SpacePoint startPointInCameraCoordinates
        {
            get
            {
                return this.startPointInCameraCoordinates;
            }
            set
            {
                // Filtering
                this.startPointInCameraCoordinates = value;
            }
        }

        public SpacePoint endPointInCameraCoordinates
        {
            get
            {
                return this.endPointInCameraCoordinates;
            }
            set
            {
                // Filtering
                this.endPointInCameraCoordinates = value;
            }
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
                this.headAndHandTracked = false;
                return;
            }

            // Don't analyze if both points are inferred
            if (skeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Inferred &&
                skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Inferred)
            {
                this.headAndHandTracked = false;
                return;
            }

            this.headAndHandTracked = true;
            this.startPointInColorFrame = this.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position);
            this.endPointInColorFrame = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
        }

        /// <summary>
        /// Draw the sight on the screen
        /// </summary>
        public void DrawSight(DrawingContext dc)
        {
            dc.DrawEllipse(this.HeadHandBrush, null, startPointInColorFrame, HeadHandThickness, HeadHandThickness);
            dc.DrawEllipse(this.HeadHandBrush, null, endPointInColorFrame, HeadHandThickness, HeadHandThickness);
            dc.DrawLine(this.SightLinePen, this.startPointInColorFrame, this.endPointInColorFrame);
        }

        /// <summary>
        /// Draw the skeletons on the screen
        /// </summary>
        public void DrawSkeleton(DrawingContext dc)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                this.DrawBones(dc);
            }
            else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
            {
                dc.DrawEllipse(this.centerPointBrush, null, this.SkeletonPointToScreen(skeleton.Position), BodyCenterThickness, BodyCenterThickness);
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="dc">drawing context to draw to</param>
        private void DrawBones(DrawingContext dc)
        {
            // Render Torso
            this.DrawBone(dc, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(dc, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(dc, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(dc, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(dc, JointType.Spine, JointType.HipCenter);
            this.DrawBone(dc, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(dc, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(dc, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(dc, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(dc, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(dc, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(dc, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(dc, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(dc, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(dc, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(dc, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(dc, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(dc, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(dc, JointType.AnkleRight, JointType.FootRight);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(DrawingContext dc, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            dc.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to color space.  
            ColorImagePoint colorPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(colorPoint.X, colorPoint.Y);
        }

    }
}
