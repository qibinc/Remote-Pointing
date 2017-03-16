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
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.FaceTracking;

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
        private const int RenderWidth = 640;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const int RenderHeight = 480;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double HandThickness = 5;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        private const double MMInPixel = 1000f / 5.2f;

        private const double focalLengthInPixel = 2.9 * MMInPixel;

        private MyMatrix CameraMatrixInverse = new MyMatrix(3, 3);

        private MyMatrix CameraMatrix = new MyMatrix(3, 3);

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Orange, 6);

        /// <summary>
        /// Brush used for drawing hands that are currently tracked
        /// </summary>
        private readonly Brush trackedHandBrush = Brushes.Red;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Brush used for drawing hands that are currently inferred
        /// </summary>        
        private readonly Brush inferredHandBrush = Brushes.OrangeRed;

        private readonly Pen sightLinePen = new Pen(Brushes.LightGoldenrodYellow, 3);

        private bool eyesTracked = false;

        private bool leftHandTracked = false;

        private bool rightHandTracked = false;

        private Point leftEyePoint;

        private Point rightEyePoint;

        private Point headPoint;

        private Point leftHandPoint;

        private Point rightHandPoint;

        private DepthSpacePoint OutPoint = new DepthSpacePoint(-1, -1, -1);
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        private DrawingGroup wallDrawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        private WriteableBitmap wallColorBitmap;

        private short[] wallDepthImage;

        private DepthImagePixel[] wallDepthPixels;

        /// <summary>
        /// Intermediate storage for the color image background
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorImage;

        private ColorImagePoint[] colorCoordinates;

        private int[] wallColorCoordinates;

        private ColorImageFormat colorImageFormat = ColorImageFormat.Undefined;

        private short[] depthImage;

        private DepthImagePixel[] depthPixels;

        private DepthImageFormat depthImageFormat = DepthImageFormat.Undefined;

        private const int opaquePixelValue = -1;

        /// <summary>
        /// Intermediate storage for the player opacity mask
        /// </summary>
        private int[] playerPixelData;

        private readonly Dictionary<int, SkeletonFaceTracker> trackedSkeletons = new Dictionary<int, SkeletonFaceTracker>();
        
        private Skeleton[] skeletonData;

        private const uint MaxMissedFrames = 100;

        private struct DepthSpacePoint
        {
            public int X, Y, Z;
            public DepthSpacePoint(int X, int Y, int Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }

        private struct DepthSpaceVector
        {
            public int X, Y, Z;
            public DepthSpaceVector(int X, int Y, int Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }

        private DepthSpacePoint[] MapColorPointToDepthSpacePoint;

        short MapperUpdateCount = 0;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.checkBoxSeatedMode.SetCurrentValue(CheckBox.IsCheckedProperty, true);
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

            if (null != this.sensor)
            {
                // Turn on the skeleton, color, depth stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                this.depthImageFormat = this.sensor.DepthStream.Format;
                this.colorImageFormat = this.sensor.ColorStream.Format;

                // Create the drawing group we'll use for drawing
                this.drawingGroup = new DrawingGroup();
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
                // Create an image source that we can use in our image control
                this.imageSource = new DrawingImage(this.drawingGroup);

                BitmapImage bitmap = new BitmapImage(new Uri(@"C:\Users\cqb\Desktop\RemotePoint-WPF\Images\WallColorFrame.png", UriKind.Relative));
                this.wallColorBitmap = new WriteableBitmap(bitmap);

                this.wallDrawingGroup = new DrawingGroup();
                this.wallDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                this.SetCameraMatrix();
                /*
                FileStream fs = new FileStream(@"C:\Users\cqb\Desktop\RemotePoint-WPF\Images\WallColorFrame.png", FileMode.Open);
                fs.Read(wallColorPixels, 0, this.sensor.ColorStream.FramePixelDataLength);
                
                this.wallColorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.wallColorBitmap.PixelWidth, this.wallColorBitmap.PixelHeight),
                    wallColorPixels,
                    this.wallColorBitmap.PixelWidth * sizeof(int),
                0);
                */
                // Display the drawing using our image control
                Image.Source = this.imageSource;
                Image1.Source = new DrawingImage(this.wallDrawingGroup);

                // This is the bitmap we'll display on-screen

                // Allocate space to put the pixels we'll receive
                this.colorImage = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                this.colorCoordinates = new ColorImagePoint[this.sensor.DepthStream.FramePixelDataLength];

                this.wallColorCoordinates = new int[RenderWidth * RenderHeight];

                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                this.depthImage = new short[this.sensor.DepthStream.FramePixelDataLength];

                this.wallDepthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                this.wallDepthImage = new short[this.sensor.DepthStream.FramePixelDataLength];

                using (FileStream fsSource = new FileStream(@"C:\Users\cqb\Desktop\RemotePoint-WPF\Images\WallDepthFrame.raw",
                    FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                    for (int i = 0; i < this.sensor.DepthStream.FramePixelDataLength; i++)
                    {
                        wallDepthImage[i] = (short)BitConverter.ToUInt16(bytes, i * 2);
                    }
                    for (int i = 0; i < RenderHeight; i++)
                        for (int j = 0; j < RenderWidth / 2; j++)
                        {
                            short t = wallDepthImage[i * RenderWidth + j];
                            wallDepthImage[i * RenderWidth + j] = wallDepthImage[i * RenderWidth + RenderWidth - 1 - j];
                            wallDepthImage[i * RenderWidth + RenderWidth - 1 - j] = t;
                        }
                }

                using (FileStream fsSource = new FileStream(@"C:\Users\cqb\Desktop\RemotePoint-WPF\Images\ColorCoordinateData.txt", FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                    for (int i = 0; i < this.sensor.DepthStream.FramePixelDataLength; i++)
                    {
                        wallColorCoordinates[i] = bytes[i * 3] * 10000 + bytes[i * 3 + 1] * 100 + bytes[i * 3 + 2];
                    }
                }

                this.playerPixelData = new int[this.sensor.DepthStream.FramePixelDataLength];

                this.MapColorPointToDepthSpacePoint = new DepthSpacePoint[RenderWidth * RenderHeight];

                for (int i = 0; i < RenderWidth * RenderHeight; ++i)
                    this.MapColorPointToDepthSpacePoint[i].X = this.MapColorPointToDepthSpacePoint[i].Y = this.MapColorPointToDepthSpacePoint[i].Z = -1;

                // Add an event handler to be called whenever there is new color frame data
                //this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Add an event handler to be called whenever there is new color frame data
                //this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

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

        private void OnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            ColorImageFrame colorImageFrame = null;
            DepthImageFrame depthImageFrame = null;
            SkeletonFrame skeletonFrame = null;

            try
            {
                colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame();
                depthImageFrame = allFramesReadyEventArgs.OpenDepthImageFrame();
                skeletonFrame = allFramesReadyEventArgs.OpenSkeletonFrame();

                if (colorImageFrame == null || depthImageFrame == null || skeletonFrame == null)
                {
                    return;
                }

                // Get the skeleton information
                if (this.skeletonData == null || this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                {
                    this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                }

                colorImageFrame.CopyPixelDataTo(this.colorImage);
                depthImageFrame.CopyPixelDataTo(this.depthImage);
                depthImageFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                /// <summary>
                /// Event handler for Kinect sensor's ColorFrameReady event
                /// </summary>
                // Write the pixel data into bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorImage,
                    this.colorBitmap.PixelWidth * sizeof(int),
                    0);

                /// <summary>
                /// Event handler for Kinect sensor's SkeletonFrameReady event
                /// </summary>
                using (DrawingContext dc = this.drawingGroup.Open(), walldc = this.wallDrawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    // dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                    // Draw a ColorImage background
                    dc.DrawImage(colorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                    walldc.DrawImage(wallColorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                    if (this.skeletonData.Length != 0)
                    {
                        foreach (Skeleton skel in this.skeletonData)
                        {
                            // RenderClippedEdges(skel, dc);

                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                this.DrawBonesAndJoints(skel, dc);
                            }
                            else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                            {
                                dc.DrawEllipse(
                                this.centerPointBrush,
                                null,
                                this.SkeletonPointToScreen(skel.Position),
                                BodyCenterThickness,
                                BodyCenterThickness);
                            }
                        }
                    }
                    else
                    {
                        this.leftHandTracked = this.rightHandTracked = false;
                        this.eyesTracked = false;
                    }

                    // Update the list of trackers and the trackers with the current frame information
                    foreach (Skeleton skeleton in this.skeletonData)
                    {
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked
                            || skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            // We want keep a record of any skeleton, tracked or untracked.
                            if (!this.trackedSkeletons.ContainsKey(skeleton.TrackingId))
                            {
                                this.trackedSkeletons.Add(skeleton.TrackingId, new SkeletonFaceTracker());
                            }

                            // Give each tracker the upated frame.
                            SkeletonFaceTracker skeletonFaceTracker;
                            if (this.trackedSkeletons.TryGetValue(skeleton.TrackingId, out skeletonFaceTracker))
                            {
                                skeletonFaceTracker.OnFrameReady(this.sensor, colorImageFormat, colorImage, depthImageFormat, depthImage, skeleton, ref this.eyesTracked);
                                this.leftEyePoint = skeletonFaceTracker.leftEyePoint;
                                this.rightEyePoint = skeletonFaceTracker.rightEyePoint;
                                skeletonFaceTracker.LastTrackedFrame = skeletonFrame.FrameNumber;
                                skeletonFaceTracker.DrawFaceModel(dc);
                            }
                        }
                    }
                    
                    //  TO REDUCE THE COMPUTATION PROPERLY
                    if (MapperUpdateCount++ % 5 == 0)
                    {
                        this.UpdateMapper();
                    }
                    this.DrawSight(dc, walldc);

                    this.RemoveOldTrackers(skeletonFrame.FrameNumber);

                    this.InvalidateVisual();

                    // prevent drawing outside of our render area
                }
            }
            finally
            {
                if (colorImageFrame != null)
                {
                    colorImageFrame.Dispose();
                }

                if (depthImageFrame != null)
                {
                    depthImageFrame.Dispose();
                }

                if (skeletonFrame != null)
                {
                    skeletonFrame.Dispose();
                }
            }
        }

        private void UpdateMapper()
        {
            // Convert point to depth space.  
            this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(
                this.depthImageFormat,
                this.depthPixels,
                this.colorImageFormat,
                this.colorCoordinates);
            // !!! Caution: MAPPER NOT INITIALIZED

            // loop over each row and column of the depth
            for (int y = 0; y < RenderHeight; ++y)
            {
                for (int x = 0; x < RenderWidth; ++x)
                {
                    // calculate index into depth array
                    int depthIndex = x + (y * RenderWidth);

                    DepthImagePixel depthPixel = this.depthPixels[depthIndex];

                    // if we're tracking a player for the current pixel, sets it opacity to full
                    if (depthPixel.PlayerIndex > 0)
                    {
                        ColorImagePoint colorImagePoint = this.colorCoordinates[depthIndex];
                        if (colorImagePoint.X >= 0 && colorImagePoint.X < RenderWidth && colorImagePoint.Y >= 0 && colorImagePoint.Y < RenderHeight)
                        {
                            MapColorPointToDepthSpacePoint[colorImagePoint.X + (colorImagePoint.Y * RenderWidth)].X = x;
                            MapColorPointToDepthSpacePoint[colorImagePoint.X + (colorImagePoint.Y * RenderWidth)].Y = y;
                            MapColorPointToDepthSpacePoint[colorImagePoint.X + (colorImagePoint.Y * RenderWidth)].Z = depthPixel.Depth;
                        }
                    }
                }
            }

        }

        private DepthSpacePoint ColorImagePointToDepthSpacePoint(Point point)
        {
            if ((int)point.X >= 0 && (int)point.X < RenderWidth && (int)point.Y >= 0 && (int)point.Y < RenderHeight)
                return MapColorPointToDepthSpacePoint[(int)point.X + (int)point.Y * RenderWidth];
            else
                return this.OutPoint;
        }

        private DepthSpacePoint GetStartPoint()
        {
            DepthSpacePoint startPoint = new DepthSpacePoint(-1, -1, -1);
            if (eyesTracked)
            {
                startPoint = ColorImagePointToDepthSpacePoint(this.leftEyePoint);
                if (startPoint.X == -1)
                    startPoint = ColorImagePointToDepthSpacePoint(this.rightEyePoint);
            }
            if (startPoint.X == -1)
            {
                startPoint = ColorImagePointToDepthSpacePoint(this.headPoint);
            }
            return startPoint;
        }

        private DepthSpacePoint GetEndPoint()
        {
            DepthSpacePoint endPoint = new DepthSpacePoint(-1, -1, -1);
            if (rightHandTracked)
            {
                endPoint = ColorImagePointToDepthSpacePoint(this.rightHandPoint);
            }
            
            //  use left hand
            if (endPoint.X == -1 && leftHandTracked)
            {
                endPoint = ColorImagePointToDepthSpacePoint(this.leftHandPoint);
            }
            
            return endPoint;
        }

        private class MyMatrix
        {
            private double[] data;

            private int row, column;

            public double Get(int n, int m)
            {
                n--; m--;
                if (n < row && m < column)
                    return data[n * column + m];
                else
                    return 0;
            }

            public void Set(int n, int m, double val)
            {
                n--; m--;
                if (n < row && m < column)
                    data[n * column + m] = val;
            }

            public MyMatrix(int n, int m)
            {
                this.row = n;
                this.column = m;
                if (n * m > 0)
                {
                    data = new double[n * m];
                    for (int i = 0; i < n * m; i++)
                        data[i] = 0;
                }
            }

            public MyMatrix(DepthSpacePoint point)
            {
                this.row = 3;
                this.column = 1;
                data = new double[3];
                data[0] = point.X;
                data[1] = point.Y;
                data[2] = point.Z;
            }

            public MyMatrix(MyMatrix mat)
            {
                this.row = mat.row;
                this.column = mat.column;
                data = new double[row * column];
                for (int i = 0; i < row; i++)
                    for (int j = 0; j < column; j++)
                        this.data[i * column + j] = mat.data[i * column + j];
            }

            public static MyMatrix operator *(MyMatrix mat1, MyMatrix mat2)
            {
                if (mat1.column != mat2.row)
                    return new MyMatrix(0, 0);

                MyMatrix newMat = new MyMatrix(mat1.row, mat2.column);
                for (int i = 0; i < mat1.row; i++)
                    for (int j = 0; j < mat2.column; j++)
                        for (int k = 0; k < mat1.column; k++)
                            newMat.data[i * mat2.column] += mat1.data[i * mat1.column + k] * mat2.data[k * mat2.column + j];
                return newMat;
            }

            public static MyMatrix operator *(double k, MyMatrix mat)
            {
                MyMatrix newMat = new MyMatrix(mat.row, mat.column);
                for (int i = 0; i < mat.row; i++)
                    for (int j = 0; j < mat.column; j++)
                            newMat.data[i * mat.column + j] = k * mat.data[i * mat.column + j];
                return newMat;
            }

        }

        private void SetCameraMatrix()
        {
            this.CameraMatrix.Set(1, 1, focalLengthInPixel);
            this.CameraMatrix.Set(1, 2, 0);
            this.CameraMatrix.Set(1, 3, 320);
            this.CameraMatrix.Set(2, 1, 0);
            this.CameraMatrix.Set(2, 2, focalLengthInPixel);
            this.CameraMatrix.Set(2, 3, 240);
            this.CameraMatrix.Set(3, 1, 0);
            this.CameraMatrix.Set(3, 2, 0);
            this.CameraMatrix.Set(3, 3, 1);

            this.CameraMatrixInverse.Set(1, 1, 0.0017931);
            this.CameraMatrixInverse.Set(1, 2, 0);
            this.CameraMatrixInverse.Set(1, 3, -0.573793);
            this.CameraMatrixInverse.Set(2, 1, 0);
            this.CameraMatrixInverse.Set(2, 2, 0.0017931);
            this.CameraMatrixInverse.Set(2, 3, -0.430345);
            this.CameraMatrixInverse.Set(3, 1, 0);
            this.CameraMatrixInverse.Set(3, 2, 0);
            this.CameraMatrixInverse.Set(3, 3, 1);
        }

        //  Using two images, do the transformation:
        //  Camera1 coordinates -> real world coordinates -> Camera2 Coordinates
        DepthSpacePoint TransformCoordinatesFromLeftToRightImage(DepthSpacePoint point, int k)
        {
            double depth = point.Z * MMInPixel;
            MyMatrix leftFramePoint = new MyMatrix(point);
            leftFramePoint.Set(3, 1, 1);

            //if (k == 1)
            //    System.Console.WriteLine(Convert.ToString(leftFramePoint.Get(1, 1)) + " " + Convert.ToString(point.Y));
            //  left camera coordinate -> left real world coordinate
            MyMatrix leftWorldPoint = CameraMatrixInverse * (depth * leftFramePoint);
            //if (k == 1)
            //    System.Console.WriteLine(Convert.ToString((int) leftWorldPoint.Get(1, 1)/1000)  + " " + Convert.ToString((int)leftWorldPoint.Get(2, 1) / 1000) + " " + Convert.ToString((int)leftWorldPoint.Get(3, 1)/1000));

            //  left real world coordinate -> right real world coordiante
            MyMatrix rightWorldPoint = new MyMatrix(leftWorldPoint);
            //rightWorldPoint.Set(1, 1, -rightWorldPoint.Get(1, 1));
            rightWorldPoint.Set(3, 1, -rightWorldPoint.Get(3, 1) + 3200 * MMInPixel);

            //  right real world coordinate -> right camera coordinate
            double newdepth = rightWorldPoint.Get(3, 1);
            MyMatrix rightFramePoint = CameraMatrix * (1 / newdepth * rightWorldPoint);

            DepthSpacePoint newpoint = new DepthSpacePoint();
            newpoint.X = (int)rightFramePoint.Get(1, 1);
            newpoint.Y = (int)rightFramePoint.Get(2, 1);
            newpoint.Z = (int)(newdepth / MMInPixel);

            return newpoint;
        }

        private void DrawLeftPart(DrawingContext drawingContext, DepthSpacePoint startPoint, DepthSpacePoint endPoint)
        {
            DepthSpaceVector dir = new DepthSpaceVector(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y, endPoint.Z - startPoint.Z);
            double length = System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);

            //  Through hand
            double X = endPoint.X + dir.X / length * 40, Y = endPoint.Y + dir.Y / length * 40, Z = endPoint.Z + dir.Y / length * 40;

            while ((int)(X + dir.X / length) >= 0 && (int)(X + dir.X / length) < RenderWidth && (int)(Y + dir.Y / length) >= 0 && (int)(Y + dir.Y / length) < RenderHeight)
            {
                X += dir.X / length;
                Y += dir.Y / length;
                Z += dir.Z / length;
                if (depthImage[RenderWidth * (int)Y + (int)X] < Z)
                    break;
            }

            //drawingContext.DrawLine(this.sightLinePen, new Point(startPoint.X, startPoint.Y), new Point(X, Y

            // Drawing sight line
            /*
            if ((int)(X + dir.X / length) >= 0 && (int)(X + dir.X / length) < RenderWidth && (int)(Y + dir.Y / length) >= 0 && (int)(Y + dir.Y / length) < RenderHeight)
            {
                drawingContext.DrawLine(this.sightLinePen, new Point(this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].X, this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].Y), new Point(this.colorCoordinates[RenderWidth * (int)Y + (int)X].X, this.colorCoordinates[RenderWidth * (int)Y + (int)X].Y));
                drawingContext.DrawEllipse(Brushes.LightGoldenrodYellow, null, new Point(this.colorCoordinates[RenderWidth * (int)Y + (int)X].X, this.colorCoordinates[RenderWidth * (int)Y + (int)X].Y), HandThickness, HandThickness);
            }
            else
            {
                drawingContext.DrawLine(this.sightLinePen, new Point(this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].X, this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].Y),
                    new Point(-7 * this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].X + 8 * this.colorCoordinates[RenderWidth * (int)endPoint.Y + (int)endPoint.X].X, -7 * this.colorCoordinates[startPoint.Y * RenderWidth + startPoint.X].Y + 8 * this.colorCoordinates[RenderWidth * (int)endPoint.Y + (int)endPoint.X].Y));
                this.RenderClippedEdges(new Point(X + dir.X / length, Y + dir.Y / length), drawingContext);
            }
*/
        }

        private void DrawRightPart(DrawingContext drawingContext, DepthSpacePoint startPoint, DepthSpacePoint endPoint)
        {            
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
        }

        private void DrawSight(DrawingContext drawingContext, DrawingContext wallDrawingContext)
        {
            // Determine two valid points, priority:
            // LeftEye > RightEye > Head
            // RightHand > LeftHand
            DepthSpacePoint startPoint = GetStartPoint(), endPoint = GetEndPoint();
            if (startPoint.X == -1 || endPoint.X == -1)
            {
                return;
            }

            DrawLeftPart(drawingContext, startPoint, endPoint);
            DrawRightPart(wallDrawingContext, TransformCoordinatesFromLeftToRightImage(startPoint, 0), TransformCoordinatesFromLeftToRightImage(endPoint, 1));
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);
            
            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Hands
            if (skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.NotTracked)
            {
                leftHandTracked = false;
            }
            else
            if (skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
            {
                leftHandTracked = true;
                leftHandPoint = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position);
                drawingContext.DrawEllipse(this.trackedHandBrush, null, leftHandPoint, HandThickness, HandThickness);
            }
            else
            if (skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Inferred)
            {
                leftHandTracked = true;
                leftHandPoint = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position);
                drawingContext.DrawEllipse(this.inferredHandBrush, null, leftHandPoint, HandThickness, HandThickness);
            }

            if (skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.NotTracked)
            {
                rightHandTracked = false;
            }
            else
            if (skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
            {
                rightHandTracked = true;
                rightHandPoint = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
                drawingContext.DrawEllipse(this.trackedHandBrush, null, rightHandPoint, HandThickness, HandThickness);
            }
            else
            if (skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Inferred)
            {
                rightHandTracked = true;
                rightHandPoint = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
                drawingContext.DrawEllipse(this.inferredHandBrush, null, rightHandPoint, HandThickness, HandThickness);
            }
            
            // Render Head
            if (eyesTracked == false)
            {
                this.headPoint = this.SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position);
                drawingContext.DrawEllipse(this.trackedHandBrush, null, this.headPoint, HandThickness, HandThickness);
            }
            // Render Joints
            /*
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
            */
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            ColorImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
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

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
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

        private void RemoveTracker(int trackingId)
        {
            this.trackedSkeletons[trackingId].Dispose();
            this.trackedSkeletons.Remove(trackingId);
        }

        private void ResetFaceTracking()
        {
            foreach (int trackingId in new List<int>(this.trackedSkeletons.Keys))
            {
                this.RemoveTracker(trackingId);
            }
        }

        private void RemoveOldTrackers(int currentFrameNumber)
        {
            var trackersToRemove = new List<int>();

            foreach (var tracker in this.trackedSkeletons)
            {
                uint missedFrames = (uint)currentFrameNumber - (uint)tracker.Value.LastTrackedFrame;
                if (missedFrames > MaxMissedFrames)
                {
                    // There have been too many frames since we last saw this skeleton
                    trackersToRemove.Add(tracker.Key);
                }
            }

            foreach (int trackingId in trackersToRemove)
            {
                this.RemoveTracker(trackingId);
            }
        }

        private class SkeletonFaceTracker : IDisposable
        {
            //private static FaceTriangle[] faceTriangles;

            private EnumIndexableCollection<FeaturePoint, PointF> facePoints;

            private FaceTracker faceTracker;

            private readonly Brush TrackedEyeBrush = Brushes.Red;

            private const double EyeThickness = 5;

            private bool lastFaceTrackSucceeded;

            private SkeletonTrackingState skeletonTrackingState;

            public Point leftEyePoint;

            public Point rightEyePoint;

            public int LastTrackedFrame { get; set; }

            public void Dispose()
            {
                if (this.faceTracker != null)
                {
                    this.faceTracker.Dispose();
                    this.faceTracker = null;
                }
            }

            public void DrawFaceModel(DrawingContext drawingContext)
            {
                if (!this.lastFaceTrackSucceeded || this.skeletonTrackingState != SkeletonTrackingState.Tracked)
                {
                    return;
                }

                /*
                var faceModelPts = new List<Point>();
                var faceModel = new List<FaceModelTriangle>();
                for (int i = 0; i < this.facePoints.Count; i++)
                {
                    faceModelPts.Add(new Point(this.facePoints[i].X + 0.5f, this.facePoints[i].Y + 0.5f));
                }
                */

                drawingContext.DrawEllipse(this.TrackedEyeBrush, null, this.leftEyePoint, EyeThickness, EyeThickness);
                drawingContext.DrawEllipse(this.TrackedEyeBrush, null, this.rightEyePoint, EyeThickness, EyeThickness);

                /*
                foreach (var t in faceTriangles)
                {
                    var triangle = new FaceModelTriangle();
                    //if (t.First != (int)FeaturePoint.MiddleBottomRightEyelid && t.Second != (int)FeaturePoint.MiddleBottomRightEyelid && t.Third != (int)FeaturePoint.MiddleBottomRightEyelid
                    //    && t.First != (int)FeaturePoint.MiddleBottomLeftEyelid && t.Second != (int)FeaturePoint.MiddleBottomLeftEyelid && t.Third != (int)FeaturePoint.MiddleBottomLeftEyelid
                    //    && t.First != (int)FeaturePoint.MiddleTopRightEyelid && t.Second != (int)FeaturePoint.MiddleTopRightEyelid && t.Third != (int)FeaturePoint.MiddleTopRightEyelid
                    //    && t.First != (int)FeaturePoint.MiddleTopLeftEyelid && t.Second != (int)FeaturePoint.MiddleTopLeftEyelid && t.Third != (int)FeaturePoint.MiddleTopLeftEyelid)
                    //    continue;
                    triangle.P1 = faceModelPts[t.First];
                    triangle.P2 = faceModelPts[t.Second];
                    triangle.P3 = faceModelPts[t.Third];
                    faceModel.Add(triangle);
                }

                var faceModelGroup = new GeometryGroup();

                for (int i = 0; i < faceModel.Count; i++)
                {
                    var faceTriangle = new GeometryGroup();
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P1, faceModel[i].P2));
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P2, faceModel[i].P3));
                    faceTriangle.Children.Add(new LineGeometry(faceModel[i].P3, faceModel[i].P1));
                    faceModelGroup.Children.Add(faceTriangle);
                }
                
                drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Red, 1.0), faceModelGroup);
                */
            }

            /// <summary>
            /// Updates the face tracking information for this skeleton
            /// </summary>
            internal void OnFrameReady(KinectSensor kinectSensor, ColorImageFormat colorImageFormat, byte[] colorImage, DepthImageFormat depthImageFormat, short[] depthImage, Skeleton skeletonOfInterest, ref bool eyesTracked)
            {
                this.skeletonTrackingState = skeletonOfInterest.TrackingState;

                if (this.skeletonTrackingState != SkeletonTrackingState.Tracked)
                {
                    // nothing to do with an untracked skeleton.
                    return;
                }

                if (this.faceTracker == null)
                {
                    try
                    {
                        this.faceTracker = new FaceTracker(kinectSensor);
                    }
                    catch (InvalidOperationException)
                    {
                        // During some shutdown scenarios the FaceTracker
                        // is unable to be instantiated.  Catch that exception
                        // and don't track a face.
                        Debug.WriteLine("AllFramesReady - creating a new FaceTracker threw an InvalidOperationException");
                        this.faceTracker = null;
                    }
                }
                
                if (this.faceTracker != null)
                {
                    FaceTrackFrame frame = this.faceTracker.Track(
                        colorImageFormat, colorImage, depthImageFormat, depthImage, skeletonOfInterest);

                    this.lastFaceTrackSucceeded = frame.TrackSuccessful;
                    if (eyesTracked = this.lastFaceTrackSucceeded)
                    {
                        /*if (faceTriangles == null)
                        {
                            // only need to get this once.  It doesn't change.
                            faceTriangles = frame.GetTriangles();
                        }*/

                        this.facePoints = frame.GetProjected3DShape();

                        this.leftEyePoint = new Point(
                            (facePoints[FeaturePoint.MiddleBottomLeftEyelid].X + facePoints[FeaturePoint.MiddleTopLeftEyelid].X) / 2,
                            (facePoints[FeaturePoint.MiddleBottomLeftEyelid].Y + facePoints[FeaturePoint.MiddleTopLeftEyelid].Y) / 2);
                        this.rightEyePoint = new Point(
                            (facePoints[FeaturePoint.MiddleBottomRightEyelid].X + facePoints[FeaturePoint.MiddleTopRightEyelid].X) / 2,
                            (facePoints[FeaturePoint.MiddleBottomRightEyelid].Y + facePoints[FeaturePoint.MiddleTopRightEyelid].Y) / 2);
                    }
                }
            }
            /*
            private struct FaceModelTriangle
            {
                public Point P1;
                public Point P2;
                public Point P3;
            }*/
        }
    }
}
