//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace NPI_1 {
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Microsoft.Kinect;



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    enum States { SETTING_POSITION, CHECKING_GESTURE, MOVEMENT_ONE, MOVEMENT_TWO, MOVEMENT_THREE };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

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
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        // Sensor de Kinect que usaremos
        private KinectSensor my_KinectSensor;

        // Punto en la pantalla para guiar al usuario
        private SkeletonPoint gesture_point = new SkeletonPoint();
        private SkeletonPoint gesture_point_2 = new SkeletonPoint();
        private SkeletonPoint move_1_point = new SkeletonPoint();
        private SkeletonPoint move_2_point = new SkeletonPoint();
        private SkeletonPoint move_3_point = new SkeletonPoint();
        private SkeletonPoint exit = new SkeletonPoint();

        // Tolerancia del error de la posición
        private double tolerance = 20;
        private double tolerance_3d = 0.15;

        // Estado de la aplicación
        private States state = States.SETTING_POSITION;

        bool situated = false;
        int first_wrong_frame = -1;

        // Alturas para situar al usuario
        private double height_up = 0.05 * RenderHeight;

        // Pen para pintar las lineas para situar al usuario
        private Pen situation_pen = new Pen(Brushes.Blue, 6);
        private Pen gesture_pen = new Pen(Brushes.Blue, 6);
        private Pen gesture_pen_2 = new Pen(Brushes.Blue, 6);
        private Pen movement_1_pen = new Pen(Brushes.Blue, 6);
        private Pen movement_2_pen = new Pen(Brushes.Blue, 6);
        private Pen movement_3_pen = new Pen(Brushes.Blue, 6);
        private Pen exit_pen = new Pen(Brushes.LightSalmon, 6);

        private Skeleton[] skeletons = new Skeleton[0];
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext) {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right)) {
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
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            draw_image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors) {
                if (potentialSensor.Status == KinectStatus.Connected) {
                    this.my_KinectSensor = potentialSensor;
                    break;
                }
            }

            if (null != this.my_KinectSensor) {
                // Turn on the skeleton stream to receive skeleton frames
                this.my_KinectSensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.my_KinectSensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                this.my_KinectSensor.ColorStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.my_KinectSensor.ColorFrameReady += Sensor_ColorFrameReady;

                // Start the sensor!
                try {
                    this.my_KinectSensor.Start();
                }
                catch (IOException) {
                    this.my_KinectSensor = null;
                }
            }

        }

        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
            using (ColorImageFrame es = e.OpenColorImageFrame()) {
                if (es != null) {
                    byte[] bits = new byte[es.PixelDataLength];
                    es.CopyPixelDataTo(bits);
                    video_image.Source = BitmapSource.Create(es.Width, es.Height, 96, 96, PixelFormats.Bgr32, null, bits, es.Width * es.BytesPerPixel);
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (null != this.my_KinectSensor) {
                this.my_KinectSensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
            skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
                if (skeletonFrame != null) {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
                else {
                    situation_pen.Brush = Brushes.Purple;
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open()) {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                bool skeleton_tracked = false;

                if (skeletons.Length != 0) {
                    foreach (Skeleton skel in skeletons) {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                            this.DrawBonesAndJoints(skel, dc);
                            detect_skeletons_position(sender, e);
                            skeleton_tracked = true;
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly) {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                if (skeletons.Length == 0 || !skeleton_tracked)
                    situation_pen.Brush = Brushes.DarkRed;


                if (state == States.SETTING_POSITION) {
                    Point left = new Point(0.4 * RenderWidth, 0.05 * RenderHeight);
                    Point right = new Point(0.6 * RenderWidth, 0.05 * RenderHeight);
                    dc.DrawLine(situation_pen, left, right);

                }
                else if (state == States.CHECKING_GESTURE) {
                    Point centre_ellipse = this.SkeletonPointToScreen(gesture_point);
                    Point centre_ellipse_2 = this.SkeletonPointToScreen(gesture_point_2);
                    dc.DrawEllipse(null, gesture_pen, centre_ellipse, 20, 20);
                    dc.DrawEllipse(null, gesture_pen_2, centre_ellipse_2, 20, 20);
                }
                else if (state == States.MOVEMENT_ONE) {
                    Point centre_ellipse_1 = this.SkeletonPointToScreen(move_1_point);
                    dc.DrawEllipse(null, movement_1_pen, centre_ellipse_1, 20, 20);
                }
                else if (state == States.MOVEMENT_TWO) {
                    Point centre_ellipse_2 = this.SkeletonPointToScreen(move_2_point);
                    Point centre_ellipse_1 = this.SkeletonPointToScreen(move_1_point);
                    dc.DrawLine(movement_2_pen, centre_ellipse_2, centre_ellipse_1);
                    dc.DrawEllipse(null, movement_2_pen, centre_ellipse_2, 20, 20);
                }
                else if (state == States.MOVEMENT_THREE) {
                    Point centre_ellipse_3 = this.SkeletonPointToScreen(move_3_point);
                    Point centre_ellipse_2 = this.SkeletonPointToScreen(move_2_point);
                    dc.DrawLine(movement_3_pen, centre_ellipse_3, centre_ellipse_2);
                    dc.DrawEllipse(null, movement_3_pen, centre_ellipse_3, 20, 20);
                }


                if(situated) {
                    Point exit_screen = this.SkeletonPointToScreen(exit);
                    dc.DrawLine(exit_pen, new Point(exit_screen.X - 10, exit_screen.Y + 10), new Point(exit_screen.X + 10, exit_screen.Y - 10));
                    dc.DrawLine(exit_pen, new Point(exit_screen.X - 10, exit_screen.Y - 10), new Point(exit_screen.X + 10, exit_screen.Y + 10));
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

            }

        }


        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext) {
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

            // Render Joints
            foreach (Joint joint in skeleton.Joints) {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked) {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred) {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null) {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.my_KinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1) {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked) {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred) {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked) {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
        
        private void My_KinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
            throw new NotImplementedException();
        }

        private void adjustColor(SkeletonPoint destination, SkeletonPoint joint,Pen pen) {
            double distance = Math.Sqrt((double)((destination.X - joint.X) * (destination.X - joint.X) +
                                                 (destination.Y - joint.Y) * (destination.Y - joint.Y) +
                                                 (destination.Z - joint.Z) * (destination.Z - joint.Z)));

            if (distance < tolerance_3d)
                pen.Brush = Brushes.Green;
            else if (distance < 1.5 * tolerance_3d)
                pen.Brush = Brushes.Yellow;
            else
                pen.Brush = Brushes.Red;

        }

        private void detect_skeletons_position(object sender, SkeletonFrameReadyEventArgs e) {
            Skeleton[] skeletons = new Skeleton[0];
            int actual_frame=-1;

            using (SkeletonFrame skeleton_frame = e.OpenSkeletonFrame()) {
                if (skeleton_frame != null) {
                    skeletons = new Skeleton[skeleton_frame.SkeletonArrayLength];
                    skeleton_frame.CopySkeletonDataTo(skeletons);
                    actual_frame = skeleton_frame.FrameNumber;
                }
            }

            if (skeletons.Length != 0) {
                bool humano_encontrado = false;
                Skeleton skel = skeletons[0];
                for (int i = 0; i < skeletons.Length && !humano_encontrado; i++) {
                    skel = skeletons[i];
                    if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                        humano_encontrado = true;
                    }

                }



                Point point_head = SkeletonPointToScreen(skel.Joints[JointType.Head].Position);

                if (state == States.SETTING_POSITION) {

                    if (Math.Abs(point_head.Y - height_up) < tolerance && Math.Abs(RenderWidth * 0.5 - point_head.X) < tolerance) {
                        situation_pen = new Pen(Brushes.Green, 6);
                        state = States.CHECKING_GESTURE;

                        gesture_point = skel.Joints[JointType.ShoulderRight].Position;
                        gesture_point.X += (float)0.25;
                        gesture_point.Y += (float)0.2;
                        gesture_point.Z -= (float)0.1;

                        gesture_point_2 = skel.Joints[JointType.ShoulderRight].Position;
                        gesture_point_2.X += (float)0.25;
                        gesture_point_2.Y -= (float)0.1;
                        gesture_point_2.Z -= (float)0.1;

                        move_1_point = skel.Joints[JointType.HipRight].Position;
                        move_1_point.X += (float)0.1;
                        move_1_point.Y -= (float)0.1;

                        move_2_point = skel.Joints[JointType.ShoulderLeft].Position;
                        move_2_point.X -= (float)0.1;
                        move_2_point.Z -= (float)0.6;

                        move_3_point = skel.Joints[JointType.ShoulderRight].Position;
                        move_3_point.Z -= (float)0.6;

                        situated = true;
                        exit = skel.Joints[JointType.Head].Position;
                        exit.X -= (float)0.95;
                        exit.Y += (float)0.05;

                        this.statusBarText.Text = "";

                    }
                    else {
                        situation_pen = new Pen(Brushes.Red, 6);

                        if(actual_frame - first_wrong_frame < 90) {
                            this.statusBarText.Text = "Vamos a volver a coger \n la posición";
                        }
                        else if (point_head.Y > height_up) {
                            this.statusBarText.Text = "Acércate";
                        }
                        else {
                            this.statusBarText.Text = "Aléjate";
                        }

                    }
                }
                else {

                    if (Math.Abs(point_head.Y - height_up) > tolerance || Math.Abs(RenderWidth * 0.5 - point_head.X) > tolerance) {
                        state = States.SETTING_POSITION;
                        first_wrong_frame = actual_frame;
                    }

                }

                if (state == States.CHECKING_GESTURE) {
                    SkeletonPoint right_hand = skel.Joints[JointType.HandRight].Position;
                    SkeletonPoint right_elbow = skel.Joints[JointType.ElbowRight].Position;
                    adjustColor(gesture_point, right_hand, gesture_pen);
                    adjustColor(gesture_point_2, right_elbow, gesture_pen_2);

                    if (gesture_pen.Brush == Brushes.Green)
                        state = States.MOVEMENT_ONE;
                }
                else if (state == States.MOVEMENT_ONE) {
                    SkeletonPoint right_hand = skel.Joints[JointType.HandRight].Position;
                    adjustColor(move_1_point, right_hand, movement_1_pen);

                    if (movement_1_pen.Brush == Brushes.Green)
                        state = States.MOVEMENT_TWO;
                }
                else if (state == States.MOVEMENT_TWO) {
                    SkeletonPoint right_hand = skel.Joints[JointType.HandRight].Position;
                    adjustColor(move_2_point, right_hand, movement_2_pen);

                    if (movement_2_pen.Brush == Brushes.Green)
                        state = States.MOVEMENT_THREE;
                }
                else if (state == States.MOVEMENT_THREE) {
                    SkeletonPoint right_hand = skel.Joints[JointType.HandRight].Position;
                    adjustColor(move_3_point, right_hand, movement_3_pen);

                    if (movement_3_pen.Brush == Brushes.Green)
                        state = States.MOVEMENT_ONE;
                }

                if (situated) { 
                    SkeletonPoint left_hand = skel.Joints[JointType.HandLeft].Position;
                    adjustColor(exit, left_hand, exit_pen);

                    if (exit_pen.Brush == Brushes.Green) {
                        this.WindowClosing(sender, new System.ComponentModel.CancelEventArgs());
                        for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                            App.Current.Windows[intCounter].Close();
                    }
                }
            }

        }

    }
}