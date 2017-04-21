using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using MyFilter = Tsinghua.Kinect.RemotePoint.DMAFilter;

namespace Tsinghua.Kinect.RemotePoint
{
    class Player
    {
        private const int FilterN = 3;

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

        private SpacePoint SkeletonPointToCameraPoint(SkeletonPoint point)
        {
            DepthImagePoint depthPoint = 
                this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(point, this.sensor.DepthStream.Format);

            return new SpacePoint(640 - depthPoint.X, depthPoint.Y, depthPoint.Depth);
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
            this.startPointInCameraCoordinates = SkeletonPointToCameraPoint(skeleton.Joints[JointType.Head].Position);
            this.endPointInColorFrame = this.painter.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
            this.endPointInCameraCoordinates = SkeletonPointToCameraPoint(skeleton.Joints[JointType.HandRight].Position);
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
