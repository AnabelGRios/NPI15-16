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
    
    
    enum States { SETTING_POSITION, CHECKING_GESTURE, MOVEMENT_ONE, MOVEMENT_TWO, MOVEMENT_THREE, MEASURING_USER };

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

        // Gestos o posturas que el usuario realizará
        Gesture exit;
        Gesture gesture;
        Gesture movement_1;
        Gesture movement_2;
        Gesture movement_3;

        // Tolerancia del error de la posición
        private double tolerance = 20;

        // Estado de la aplicación
        private States state = States.SETTING_POSITION;

        bool situated = false;
        int first_wrong_frame = -1;

        bool measured = false;
        int first_frame_measure = -1;
        float arm = 0, forearm = 0;
        
        // Alturas para situar al usuario
        private double height_up = 0.05 * RenderHeight;

        // Pen para pintar las lineas para situar al usuario
        private Pen situation_pen = new Pen(Brushes.Blue, 6);

        private Skeleton[] skeletons = new Skeleton[0];
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
        }
               
        public static SkeletonPoint sum(SkeletonPoint first, SkeletonPoint second) {
            SkeletonPoint sum = first;
            sum.X += second.X;
            sum.Y += second.Y;
            sum.Z += second.Z;
            return sum;
        }

        public static SkeletonPoint sum(SkeletonPoint point, double x, double y, double z) {
            SkeletonPoint sum = point;
            sum.X +=(float)x;
            sum.Y +=(float)y;
            sum.Z +=(float)z;
            return sum;
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
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();
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
            }

            using (DrawingContext dc = this.drawingGroup.Open()) {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                bool skeleton_tracked = false;

                if (skeletons.Length != 0) {
                    foreach (Skeleton skel in skeletons) {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                            detect_skeletons_position(sender, e);
                            skeleton_tracked = true;
                        }
                    }
                }

                if (skeletons.Length == 0 || !skeleton_tracked)
                    situation_pen.Brush = Brushes.DarkRed;

                switch (state) {
                    case States.SETTING_POSITION:
                        dc.DrawLine(situation_pen, new Point(0.4 * RenderWidth, 0.05 * RenderHeight), new Point(0.6 * RenderWidth, 0.05 * RenderHeight));
                        break;
                    case States.CHECKING_GESTURE:
                        gesture.drawCircle(dc, 20);
                        gesture.drawCircle(dc, 10, 1);
                        break;
                    case States.MOVEMENT_ONE:
                        movement_1.drawCircle(dc, 20);
                        break;
                    case States.MOVEMENT_TWO:
                        movement_2.drawCircle(dc, 20);
                        dc.DrawLine(movement_2.getPen(), movement_2.getPoint(), movement_1.getPoint());
                        break;
                    case States.MOVEMENT_THREE:
                        movement_3.drawCircle(dc, 20);
                        dc.DrawLine(movement_3.getPen(), movement_2.getPoint(), movement_3.getPoint());
                        break;
                }


                if(situated && measured) {
                    exit.drawCross(dc, 10);
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

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
      


        private void initializeElements(Skeleton skel) {
            SkeletonPoint[] gesture_points = new SkeletonPoint[2];
            gesture_points[0] = sum(skel.Joints[JointType.ShoulderRight].Position, 0.9*arm, 0.9*forearm, -0.1);
            gesture_points[1] = sum(skel.Joints[JointType.ShoulderRight].Position, 0.9*arm, -0.1, -0.1);
            JointType[] gesture_joints = new JointType[2];
            gesture_joints[0] = JointType.HandRight;
            gesture_joints[1] = JointType.ElbowRight;
            gesture = new Gesture(gesture_points, gesture_joints, my_KinectSensor, 2);

            movement_1 = new Gesture(sum(skel.Joints[JointType.HipRight].Position, 0.1, -0.1, 0), JointType.HandRight, my_KinectSensor, 2);
            movement_2 = new Gesture(sum(skel.Joints[JointType.ShoulderLeft].Position, -0.05, 0, -0.8*(arm+ forearm)), JointType.HandRight, my_KinectSensor, 2);
            movement_3 = new Gesture(sum(skel.Joints[JointType.ShoulderRight].Position, 0, 0, -0.9*(arm+ forearm)), JointType.HandRight, my_KinectSensor, 2);

            exit = new Gesture(sum(skel.Joints[JointType.Head].Position, -2*(arm+ forearm), -0.05, 0), JointType.HandLeft, my_KinectSensor, 3);
            exit.setDistanceColor(0, Brushes.Purple);
            exit.setDistanceColor(1, Brushes.Blue);
            exit.setDistanceColor(2, Brushes.Gray);
            exit.setTimeColor(Brushes.Red);

            situated = true;
        }

        private void measureUser(Skeleton skel, int actual_frame) {
            this.statusBarText.Text = "Ponte en esta posición. \n Vamos a medirte.";
            this.measure_imagen.Visibility = Visibility.Visible;
            this.measure_imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/img.png")));

            if (first_frame_measure == -1) {
                first_frame_measure = actual_frame;
            }
            if (actual_frame - first_frame_measure > 120) {
                SkeletonPoint right_shoulder = skel.Joints[JointType.ShoulderRight].Position;
                SkeletonPoint right_elbow = skel.Joints[JointType.ElbowRight].Position;
                SkeletonPoint right_wrist = skel.Joints[JointType.WristRight].Position;
                arm = (float)Math.Sqrt((double)(Math.Pow((right_shoulder.X - right_elbow.X), 2) +
                    Math.Pow((right_shoulder.Y - right_elbow.Y), 2) +
                    Math.Pow((right_shoulder.Z - right_elbow.Z), 2)));
                forearm = (float)Math.Sqrt((double)(Math.Pow((right_wrist.X - right_elbow.X), 2) +
                    Math.Pow((right_wrist.Y - right_elbow.Y), 2) +
                    Math.Pow((right_wrist.Z - right_elbow.Z), 2)));

                measured = true;
                state = States.CHECKING_GESTURE;
            }
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
                bool human_found = false;
                Skeleton skel = skeletons[0];
                for (int i = 0; i < skeletons.Length && !human_found; i++) {
                    skel = skeletons[i];
                    if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                        human_found = true;
                    }
                }

                Point point_head = SkeletonPointToScreen(skel.Joints[JointType.Head].Position);

                if (state == States.SETTING_POSITION) {
                    this.imagen.Visibility = Visibility.Hidden;

                    if (Math.Abs(point_head.Y - height_up) < tolerance && Math.Abs(RenderWidth * 0.5 - point_head.X) < tolerance) {
                        situation_pen = new Pen(Brushes.Green, 6);

                        if (!measured) 
                            state = States.MEASURING_USER;
                        else 
                            state = States.CHECKING_GESTURE;
                    }
                    else {
                        situation_pen = new Pen(Brushes.Red, 6);

                        if(actual_frame - first_wrong_frame < 60) {
                            this.statusBarText.Text = "Vamos a volver a coger \n la posición";
                        }
                        else if (point_head.Y > height_up + tolerance) {
                            this.statusBarText.Text = "Acércate";
                        }
                        else if (point_head.Y < height_up - tolerance) {
                            this.statusBarText.Text = "Aléjate";
                        }
                        else if (point_head.X > 0.5*RenderWidth + tolerance) {
                            this.statusBarText.Text = "Muévete a la izquierda";
                        }
                        else if (point_head.X < 0.5 * RenderWidth - tolerance) {
                            this.statusBarText.Text = "Muévete a la derecha";
                        }

                    }
                }
                else if (Math.Abs(point_head.Y - height_up) > tolerance || Math.Abs(RenderWidth * 0.5 - point_head.X) > tolerance) {
                    state = States.SETTING_POSITION;
                    first_wrong_frame = actual_frame;
                }

                if (state == States.MEASURING_USER) {
                    measureUser(skel, actual_frame);
                    initializeElements(skel);
                }

                if (state == States.CHECKING_GESTURE) {
                    this.statusBarText.Text = "";
                    this.measure_imagen.Visibility = Visibility.Hidden;
                    this.imagen.Visibility = Visibility.Visible;
                    this.imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/gesto.png")));

                    gesture.adjustColor(skel,actual_frame);

                    if (gesture.isCompleted() ) 
                        state = States.MOVEMENT_ONE;

                }
                else if (state == States.MOVEMENT_ONE) {
                    this.imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/movimiento1.png")));
                    movement_1.adjustColor(skel,actual_frame);

                    if (movement_1.isCompleted() ) 
                        state = States.MOVEMENT_TWO;

                }
                else if (state == States.MOVEMENT_TWO) {
                    this.imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/movimiento2.png")));
                    movement_2.adjustColor(skel,actual_frame);

                    if (movement_2.isCompleted()) 
                        state = States.MOVEMENT_THREE;

                }
                else if (state == States.MOVEMENT_THREE) {
                    this.imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/movimiento3.png")));
                    movement_3.adjustColor(skel,actual_frame);
                    
                    if ( movement_3.isCompleted() )
                        state = States.MOVEMENT_ONE;

                }

                if (situated && measured) {
                    exit.adjustColor(skel,actual_frame);

                    if (exit.isCompleted()) 
                        this.WindowClosing(sender, new System.ComponentModel.CancelEventArgs());
                }
            }
        }
    }
}