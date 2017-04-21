//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Tsinghua.Kinect.RemotePoint
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Point = System.Windows.Point;
    using Rect = System.Windows.Rect;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private int RenderWidth;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private int RenderHeight;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        private ColorImageFormat colorImageFormat = ColorImageFormat.Undefined;
        private DepthImageFormat depthImageFormat = DepthImageFormat.Undefined;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing group for output
        /// </summary>
        private DrawingGroup outputDrawingGroup;

        /// <summary>
        /// Intermediate storage for the color image background
        /// </summary>
        private WriteableBitmap colorBitmap;

        private WriteableBitmap blankColorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorImage;

        private readonly Brush PointBrush = Brushes.Red;

        private readonly double PointThickness = 5;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private readonly double ClipBoundsThickness = 10;

        private const double MMInPixel = 1000f / 5.2f;

        private const double focalLengthInPixel = 531.15f;
        
        private Matrix CameraMatrixInverse = new Matrix(3, 3);

        private Matrix CameraMatrix = new Matrix(3, 3);
        
        private Skeleton[] skeletonData;

        private Player[] players;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                /*
                // Turn on the skeleton, color, depth stream to receive skeleton frames
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.1f;
                    smoothingParam.MaxDeviationRadius = 0.1f;
                };
                this.sensor.SkeletonStream.Enable(smoothingParam);
                */
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.checkBoxSeatedMode.SetCurrentValue(CheckBox.IsCheckedProperty, true);
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                this.RenderHeight = 480;
                this.RenderWidth = 640;
                this.depthImageFormat = this.sensor.DepthStream.Format;
                this.colorImageFormat = this.sensor.ColorStream.Format;

                // Create the drawing group we'll use for drawing
                this.drawingGroup = new DrawingGroup();
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
                this.outputDrawingGroup = new DrawingGroup();
                this.outputDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderHeight, RenderHeight));
                
                // Display the drawing using our image control
                Image.Source = new DrawingImage(this.drawingGroup);
                // Allocate space to put the pixels we'll receive
                this.colorImage = new byte[this.sensor.ColorStream.FramePixelDataLength];
                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.blankColorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                OutputImage.Source = new DrawingImage(this.outputDrawingGroup);

                this.SetCameraMatrix();

                // Add an event handler to be called whenever there is new all frame data
                this.sensor.AllFramesReady += this.OnAllFramesReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's OnAllFramesReady event
        /// </summary>
        private void OnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            ColorImageFrame colorImageFrame = null;
            //DepthImageFrame depthImageFrame = null;
            SkeletonFrame skeletonFrame = null;

            try
            {
                colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame();
                //depthImageFrame = allFramesReadyEventArgs.OpenDepthImageFrame();
                skeletonFrame = allFramesReadyEventArgs.OpenSkeletonFrame();

                if (colorImageFrame == null || /*depthImageFrame == null || */skeletonFrame == null)
                {
                    return;
                }

                HandleColorImage(colorImageFrame);
                //HandleDepthImage(depthImageFrame);
                HandleSkeleton(skeletonFrame);

                DrawOutput();
            }
            finally
            {
                if (colorImageFrame != null)
                {
                    colorImageFrame.Dispose();
                }
                /*
                if (depthImageFrame != null)
                {
                    depthImageFrame.Dispose();
                }
                */
                if (skeletonFrame != null)
                {
                    skeletonFrame.Dispose();
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        private void HandleColorImage(ColorImageFrame colorImageFrame)
        {
            colorImageFrame.CopyPixelDataTo(this.colorImage);

            // Write the pixel data into bitmap
            this.colorBitmap.WritePixels(
                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                this.colorImage,
                this.colorBitmap.PixelWidth * sizeof(int),
                0);
        }

        /*
        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        private void HandleDepthImage(DepthImageFrame depthImageFrame)
        {
            depthImageFrame.CopyPixelDataTo(this.depthImage);
        }
        */

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        private void HandleSkeleton(SkeletonFrame skeletonFrame)
        {
            // Get the skeleton information
            if (this.skeletonData == null || this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
            {
                this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                this.players = new Player[skeletonFrame.SkeletonArrayLength];
                for (int i = 0; i < this.skeletonData.Length; i++)
                {
                    this.players[i] = new Player();
                    this.players[i].sensor = this.sensor;
                }
            }

            //  Copy skeleton data
            skeletonFrame.CopySkeletonDataTo(this.skeletonData);
            for (int i = 0; i < this.skeletonData.Length; i++)
            {
                this.players[i].skeleton = this.skeletonData[i];
            }

            //  Update start point and End point
            foreach (Player player in this.players)
            {
                player.AnalyzeHeadAndHands();
            }

            //  Draw Players' skeletons
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw the color scene background
                dc.DrawImage(colorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                // Draw skeletons
                foreach (Player player in this.players)
                {
                    player.DrawSkeleton(dc);

                    if (player.headAndHandValid == true)
                    {
                        player.DrawSight(dc);
                    }
                }
            }

            //this.InvalidateVisual();
        }
        
        private void SetCameraMatrix()
        {
            this.CameraMatrix.Set(0, 0, focalLengthInPixel); this.CameraMatrix.Set(0, 1, 0); this.CameraMatrix.Set(0, 2, RenderWidth / 2);
            this.CameraMatrix.Set(1, 0, 0); this.CameraMatrix.Set(1, 1, focalLengthInPixel); this.CameraMatrix.Set(1, 2, RenderHeight / 2);
            this.CameraMatrix.Set(2, 0, 0); this.CameraMatrix.Set(2, 1, 0); this.CameraMatrix.Set(2, 2, 1);

            this.CameraMatrixInverse.Set(0, 0, 0.00188271); this.CameraMatrixInverse.Set(0, 1, 0); this.CameraMatrixInverse.Set(0, 2, -0.60246635);
            this.CameraMatrixInverse.Set(1, 0, 0); this.CameraMatrixInverse.Set(1, 1, 0.00188271); this.CameraMatrixInverse.Set(1, 2, -0.45184976);
            this.CameraMatrixInverse.Set(2, 0, 0); this.CameraMatrixInverse.Set(2, 1, 0); this.CameraMatrixInverse.Set(2, 2, 1);
        }
        
        /// <summary>
        /// Map point from camera coordinates to room coordinates
        /// </summary>
        private SpacePoint CameraPointToRoomPoint(SpacePoint cameraPoint)
        {            
            double depth = cameraPoint.Z;
            cameraPoint.Z = 1;

            Matrix cameraCoordinatesVector = CameraMatrixInverse * (depth * new Matrix(cameraPoint));

            SpacePoint cameraCoordinatesPoint = new SpacePoint(cameraCoordinatesVector);
            
            //    暂时设房间坐标系为深度摄像头坐标系，右手系，单位 MM
            SpacePoint roomPoint = cameraCoordinatesPoint;
            //
            //    对cameraCoordinatesPoint进行rotate得到roompoint
            //
            System.Diagnostics.Debug.WriteLine(roomPoint.X.ToString() + " " + roomPoint.Y.ToString() + " " + roomPoint.Z.ToString());
            return roomPoint;
        }

        /// <summary>
        /// Map point from camera coordinates to room coordinates
        /// </summary>
        private SpacePoint RoomPointToCameraPoint(SpacePoint roomPoint)
        {
            Matrix cameraCoordinatesVector = new Matrix(roomPoint);
            //
            //  对roomPoint进行rotate得到cameraCoordinatesMatrix
            //

            double depth = cameraCoordinatesVector.Get(2, 0);

            SpacePoint cameraPoint = new SpacePoint((1 / depth) * (CameraMatrix * cameraCoordinatesVector));
            return cameraPoint;
        }

        /// <summary>
        /// Fine the intersection on the room's walls
        /// </summary>
        private SpacePoint FindTheIntersection(SpacePoint start, SpacePoint end)
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


        /*
        private const int DMAFilterN = 3;
        private Filter DMAFilterX = new DMAFilter(DMAFilterN);
        private Filter DMAFilterY = new DMAFilter(DMAFilterN);

        private const int MedianFilterN = 4;
        private Filter MedianFilterX = new MedianFilter(MedianFilterN);
        private Filter MedianFilterY = new MedianFilter(MedianFilterN);
        */
        /// <summary>
        /// draw output points on the screen
        /// </summary>
        private void DrawOutput()
        {
            using (DrawingContext dc = this.outputDrawingGroup.Open())
            {
                dc.DrawImage(blankColorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                foreach (Player player in this.players)
                {
                    if (player.headAndHandValid == true)
                    {                        
                        /*
                        DMAFilterX.SetValue(player.endPointInColorFrame.X);
                        DMAFilterY.SetValue(player.endPointInColorFrame.Y);
                        Point DMAPoint = new Point(DMAFilterX.GetValue(), DMAFilterY.GetValue());
                        
                        MedianFilterX.SetValue(player.endPointInColorFrame.X);
                        MedianFilterY.SetValue(player.endPointInColorFrame.Y);
                        Point MedianPoint = new Point(MedianFilterX.GetValue(), MedianFilterY.GetValue());
                        */

                        SpacePoint intersection = FindTheIntersection(CameraPointToRoomPoint(player.startPointInCameraCoordinates),
                                                                      CameraPointToRoomPoint(player.endPointInCameraCoordinates));
                        /*
                        if (showPoint.X >= 0 && showPoint.X < RenderHeight && showPoint.Y >= 0 && showPoint.Y < RenderHeight)
                        {
                            dc.DrawEllipse(Brushes.White, null, showPoint, this.PointThickness, this.PointThickness);
                            //dc.DrawEllipse(Brushes.Purple, null, DMAPoint, this.PointThickness, this.PointThickness);
                            //dc.DrawEllipse(Brushes.Orange, null, MedianPoint, this.PointThickness, this.PointThickness);
                        }
                        else
                        {
                            RenderClippedEdges(showPoint, dc);
                        }
                         */
                    }
                }
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void RenderClippedEdges(Point point, DrawingContext drawingContext)
        {
            if (point.Y >= RenderHeight)
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (point.Y < 0)
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (point.X < 0)
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (point.X >= RenderWidth)
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

    }
}
