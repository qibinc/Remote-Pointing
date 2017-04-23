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

        private readonly double PointThickness = 5;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private readonly double ClipBoundsThickness = 10;
                
        private Skeleton[] skeletonData;

        private Player[] players;

        private const int maxPlayerNumber = 5;
        private Brush[] playerColors = new Brush[maxPlayerNumber] { Brushes.MediumPurple, Brushes.LightGreen, Brushes.Yellow, Brushes.Orange, Brushes.Pink };

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
                
                //this.sensor.SkeletonStream.Enable();

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
                this.outputDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
                // Display the drawing using our image control
                Image.Source = new DrawingImage(this.drawingGroup);
                // Allocate space to put the pixels we'll receive
                this.colorImage = new byte[this.sensor.ColorStream.FramePixelDataLength];
                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.blankColorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                OutputImage.Source = new DrawingImage(this.outputDrawingGroup);

                RoomSetting.SetCameraMatrix();

                RoomSetting.SetPlates();

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

            for (int i = 0; i < RenderHeight; i++)
            {
                for (int j = 0; j < RenderWidth / 2; j++)
                {
                    for (int k = 0; k < sizeof(int); k++)
                    {
                        byte t = this.colorImage[(RenderWidth * i + j) * sizeof(int) + k];
                        this.colorImage[(RenderWidth * i + j) * sizeof(int) + k] = this.colorImage[(RenderWidth * i + RenderWidth - j - 1) * sizeof(int) + k];
                        this.colorImage[(RenderWidth * i + RenderWidth - j - 1) * sizeof(int) + k] = t;
                    }
                }
            }

            // Write the pixel data into bitmap
            this.colorBitmap.WritePixels(
                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                this.colorImage,
                this.colorBitmap.PixelWidth * sizeof(int),
                0);

            // Draw the color scene background

            using (DrawingContext dc = drawingGroup.Open())
            {
                dc.DrawImage(colorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        private void HandleSkeleton(SkeletonFrame skeletonFrame)
        {
            // Get the skeleton information
            if (this.skeletonData == null || this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
            {
                this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                this.players = new Player[System.Math.Min(skeletonFrame.SkeletonArrayLength, maxPlayerNumber)];
                for (int i = 0; i < this.skeletonData.Length && i < maxPlayerNumber; i++)
                {
                    this.players[i] = new Player(this.sensor, playerColors[i]);
                }
            }

            //  Copy skeleton data
            skeletonFrame.CopySkeletonDataTo(this.skeletonData);
            for (int i = 0; i < this.skeletonData.Length && i < maxPlayerNumber; i++)
            {
                this.players[i].skeleton = this.skeletonData[i];
            }

            //  Update start point and End point
            foreach (Player player in this.players)
            {
                player.AnalyzeHeadAndHands();
            }

            DrawOutput();
            //this.InvalidateVisual();
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
                RoomSetting.PaintPlatesAndCoordinates(dc);

                foreach (Player player in this.players)
                {
                    player.DrawSkeleton(dc);

                    if (player.headAndHandValid == true)
                    {
                        SpacePoint intersection = RoomSetting.FindTheIntersection(RoomSetting.CameraPointToRoomPoint(player.startPointInCameraCoordinates),
                                                                      RoomSetting.CameraPointToRoomPoint(player.endPointInCameraCoordinates));

                        if (intersection != null)
                        {
                            Point showPoint = RoomSetting.RoomPointToObservePoint(intersection);
                            dc.DrawLine(new Pen(player.color, 2), RoomSetting.CameraPointToObservePoint(player.startPointInCameraCoordinates),
                                      showPoint);

                            if (showPoint.X >= 0 && showPoint.X < RenderWidth && showPoint.Y >= 0 && showPoint.Y < RenderHeight)
                            {
                                dc.DrawEllipse(player.color, null, showPoint, this.PointThickness, this.PointThickness);
                            }
                            else
                            {
                                RenderClippedEdges(showPoint, dc);
                            }
                        }
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
