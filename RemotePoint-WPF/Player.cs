using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using MyFilter = Tsinghua.Kinect.RemotePoint.NoneFilter;

namespace Tsinghua.Kinect.RemotePoint
{
    class Player
    {
        public Player(KinectSensor sensor, Brush color)
        {
            this.sensor = sensor;
            this.color = color;
            this.painter = new Painter(sensor, color);
        }

        private KinectSensor sensor;

        public Brush color;

        private const int FilterN = 3;
        
        public Skeleton skeleton;

        private Painter painter;


        public bool headAndHandValid = false;

        /*
        private FramePointFiltering _startFramePointFiltering = new FramePointFiltering(new MyFilter(FilterN), new MyFilter(FilterN));
        private Point startPointInColorFrame
        {
            get
            {
                return _startFramePointFiltering.GetValue();
            }
            set
            {
                _startFramePointFiltering.SetValue(value);
            }
        }

        private FramePointFiltering _endFramePointFiltering = new FramePointFiltering(new MyFilter(FilterN), new MyFilter(FilterN));
        private Point endPointInColorFrame
        {
            get
            {
                return _endFramePointFiltering.GetValue();
            }
            set
            {
                _endFramePointFiltering.SetValue(value);
            }
        }
        */
        private PointFiltering _startPointFiltering = new PointFiltering(new MyFilter(FilterN), new MyFilter(FilterN), new MyFilter(FilterN));
        public SpacePoint startPointInCameraCoordinates
        {
            get
            {
                return _startPointFiltering.GetValue();
            }
            set
            {
                _startPointFiltering.SetValue(value);
            }
        }

        private PointFiltering _endPointFiltering = new PointFiltering(new MyFilter(FilterN), new MyFilter(FilterN), new MyFilter(FilterN));
        public SpacePoint endPointInCameraCoordinates
        {
            get
            {
                return _endPointFiltering.GetValue();
            }
            set
            {
                _endPointFiltering.SetValue(value);
            }
        }

        /// <summary>
        ///  Called to update point info
        /// </summary>
        public void AnalyzeHeadAndHands()
        {
            if (skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
            {
                //System.Diagnostics.Debug.WriteLine(RoomSetting.CameraPointToRoomPoint(this.painter.SkeletonPointToCameraPoint(skeleton.Joints[JointType.HandLeft].Position)).Z);

                if (RoomSetting.CameraPointToRoomPoint(this.painter.SkeletonPointToCameraPoint(skeleton.Joints[JointType.HandLeft].Position)).Z > 1200)
                {
                    RoomSetting.move = true;
                }
                else
                {
                    RoomSetting.move = false;
                }
            }

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
            //this.startPointInColorFrame = this.painter.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position);
            this.startPointInCameraCoordinates = this.painter.SkeletonPointToCameraPoint(skeleton.Joints[JointType.Head].Position);
            //this.endPointInColorFrame = this.painter.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
            this.endPointInCameraCoordinates = this.painter.SkeletonPointToCameraPoint(skeleton.Joints[JointType.HandRight].Position);
        }

        /// <summary>
        /// Draw the skeletons on the screen
        /// </summary>
        public void DrawSkeleton(DrawingContext dc)
        {
            this.painter.DrawSkeleton(dc, skeleton);
        }

        /*
        /// <summary>
        /// Draw the sight on the screen
        /// </summary>
        public void DrawSight(DrawingContext dc)
        {
            this.painter.DrawSight(dc, startPointInColorFrame, endPointInColorFrame);
        }
        */

    }
}
