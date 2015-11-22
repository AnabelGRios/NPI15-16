﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Methods WindowLoaded, SkeletonPointToScreen
//     Variables RenderWidth, RenderHeight, drawingGroup
//     XAML Code
// </copyright>
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
// Authors: Anabel Gómez Ríos, Jacinto Carrasco Castillo
// Date: 30-10-2015
//------------------------------------------------------------------------------

/*    This file is part of NPI_2.
    NPI_2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    NPI_2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    See<http://www.gnu.org/licenses/>.
*/



namespace NPI_2 {
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Possible states of the application
    /// </summary>
    enum States { SETTING_POSITION, MEASURING_USER, PLAYING, PAUSED };

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
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Kinect Sensor for the application
        /// </summary>
        private KinectSensor my_KinectSensor;

        /// <summary>
        /// Gestures the user is going to do
        /// </summary>
        Gesture exit;
        Gesture measuring;
        Gesture pause;

        Gesture shoot_0;
        Gesture shoot_1;

        /// <summary>
        /// Tolerance for the initial position
        /// </summary>
        private double tolerance = 20;

        /// <summary>
        /// State of the application
        /// </summary>
        private States state = States.SETTING_POSITION;

        /// <summary>
        /// Information to know if the user is situated
        /// </summary>
        bool situated = false;
        int first_wrong_frame = -1;

        /// <summary>
        /// Information to know if the user's been measured
        /// </summary>
        bool measured = false;
        int first_frame_measure = -1;

        /// <summary>
        /// Information to know if the shot can be completed
        /// </summary>
        bool pointed = false;
        bool shooting = false;
        int first_frame_shooting = -1;

        /// <summary>
        /// Tolerance for the shoots
        /// </summary>
        private double tolerance_shoot = 0.05;
        private Point shoot_objective;

        private int first_frame_shoot = -1;
        private Point actual_shot_point;

        private Pen shoot_pen = new Pen(Brushes.Black, 6);

        /// <summary>
        /// Measures of the user's arm
        /// </summary>
        float arm = 0, forearm = 0;

        /// <summary>
        /// Height to situate the user
        /// </summary>
        private double height_up = 0.05 * RenderHeight;

        /// <summary>
        /// Pen to draw the situation line
        /// </summary>
        private Pen situation_pen = new Pen(Brushes.Blue, 6);

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Sum two SkeletonPoints
        /// </summary>
        /// <param name="first"> First point </param> 
        /// <param name="second"> Second point </param>
        /// <returns> Sum </returns> 
        public static SkeletonPoint sum(SkeletonPoint first, SkeletonPoint second) {
            SkeletonPoint sum = first;
            sum.X += second.X;
            sum.Y += second.Y;
            sum.Z += second.Z;
            return sum;
        }

        /// <summary>
        /// Sum two 3D points, one given by a SkeletonPoint and the other by its coordinates
        /// </summary>
        /// <param name="point">SkeletonPoint to sum</param>
        /// <param name="x"> 1st coordinate</param>
        /// <param name="y"> 2nd coordinate</param>
        /// <param name="z"> 3rd coordinate</param>
        /// <returns> Sum </returns>
        public static SkeletonPoint sum(SkeletonPoint point, double x, double y, double z) {
            SkeletonPoint sum = point;
            sum.X += (float)x;
            sum.Y += (float)y;
            sum.Z += (float)z;
            return sum;
        }

        public int getRandomNumber(int max_number) {
            Random random = new Random();
            return (random.Next() % max_number) + 1;
        }


        public float distance(SkeletonPoint a, SkeletonPoint b) {
            float distance = (float) Math.Sqrt((double)(Math.Pow((a.X - b.X), 2) +
                Math.Pow((a.Y - b.Y), 2) +
                Math.Pow((a.Z - b.Z), 2)));

            return distance;
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

        /// <summary>
        /// Show the color image that the sensor is receiving
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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
        /// Draw the scene
        /// </summary>
        /// <param name="e">event arguments</param>
        private void DrawScene(object sender, int actual_frame) {

            using (DrawingContext dc = this.drawingGroup.Open()) {

                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
                // Draw the guides
                switch (state) {
                    case States.SETTING_POSITION:
                        dc.DrawLine(situation_pen, new Point(0.4 * RenderWidth, 0.05 * RenderHeight), new Point(0.6 * RenderWidth, 0.05 * RenderHeight));
                        break;
                    case States.MEASURING_USER:
                        measuring.drawCircle(dc, 10, 0);
                        measuring.drawCircle(dc, 10, 1);
                        measuring.drawCircle(dc, 10, 2);
                        measuring.drawCircle(dc, 10, 3);
                        break;
                    case States.PAUSED:
                        if(!shooting && !pointed)
                            dc.DrawEllipse(null, situation_pen, shoot_objective, 10, 10);
                        if (shooting || pointed)
                            shoot_1.drawCross(dc, 10);

                        if (shoot_1.isCompleted() && actual_frame-first_frame_shoot < 80)
                            dc.DrawEllipse(null, shoot_pen, actual_shot_point, 30, 30);

                        break;
                }

                if (measured) {
                    exit.drawCross(dc, 10);

                    if (exit.isCompleted())
                        this.WindowClosing(sender, new System.ComponentModel.CancelEventArgs());
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

            }
        }
        
        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// Draw the guides according to the state
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
            Skeleton[] skeletons = new Skeleton[0];
            bool skeleton_tracked = false;

            // Copy the Skeleton data to skeletons
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
                if (skeletonFrame != null) {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    // Find a tracked object of Skeleton class 
                    foreach (Skeleton skel in skeletons) {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                            detect_skeletons_position(skel, skeletonFrame.FrameNumber);
                            skeleton_tracked = true;
                        }
                    }

                    DrawScene(sender, skeletonFrame.FrameNumber);
                }
            }

            // Change colors and states if no skeleton is recorded
            if (skeletons.Length == 0 || !skeleton_tracked) {
                situation_pen.Brush = Brushes.DarkRed;
                this.statusBarText.Text = "";
                this.measured = false;                      //Remeasure the user if it's not captured by the sensor
                state = States.SETTING_POSITION;
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
        /// Determine gestures and guides positions
        /// </summary>
        /// <param name="skel">Skeleton tracked to determine guides positions</param>
        private void initializeElements(Skeleton skel) {
            SkeletonPoint[] pause_points = new SkeletonPoint[2];
            pause_points[0] = sum(skel.Joints[JointType.HipRight].Position, 0.2, 0, -0.1);
            pause_points[1] = sum(skel.Joints[JointType.HipLeft].Position, -0.2, 0, -0.1);
            JointType[] pause_joints = new JointType[2];
            pause_joints[0] = JointType.HandRight;
            pause_joints[1] = JointType.HandLeft;
            pause = new Gesture(pause_points, pause_joints, my_KinectSensor, 2);

            pause.setDistanceColor(1, Brushes.LightGray);
            pause.setDistanceColor(2, Brushes.Transparent);

            SkeletonPoint hand = skel.Joints[JointType.HandRight].Position;
            SkeletonPoint elbow = skel.Joints[JointType.ElbowRight].Position;

            shoot_0 = new Gesture(hand, JointType.HandRight,my_KinectSensor,2,0.05f);
            shoot_1 = new Gesture(sum(elbow, 0.05 * Math.Sign(hand.X - elbow.X), 0.9*forearm, 0.9*forearm), JointType.HandRight, my_KinectSensor, 0.1f, 0.3f);
            
            exit = new Gesture(sum(skel.Joints[JointType.Head].Position, -2 * (arm + forearm), -0.05, 0), JointType.HandLeft, my_KinectSensor, 1);
            exit.setDistanceColor(0, Brushes.Purple);
            exit.setDistanceColor(1, Brushes.Blue);
            exit.setDistanceColor(2, Brushes.Gray);
            exit.setTimeColor(Brushes.Red);
        }

        /// <summary>
        /// Measure the user to give a more personalizated positions
        /// </summary>
        /// <param name="skel">Skeleton tracked to determine user measures</param>
        /// <param name="actual_frame">Number of the actual frame to determine the waiting time</param>
        private void measureUser(Skeleton skel, int actual_frame) {
            this.statusBarText.Text = "Ponte en esta posición. \n Vamos a medirte.";
            this.measure_imagen.Visibility = Visibility.Visible;
            // Show the guide image
            this.measure_imagen.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/img.png")));

            SkeletonPoint right_shoulder = skel.Joints[JointType.ShoulderRight].Position;
            SkeletonPoint right_elbow = skel.Joints[JointType.ElbowRight].Position;
            SkeletonPoint right_wrist = skel.Joints[JointType.WristRight].Position;

            arm = distance(right_shoulder, right_elbow);
            forearm = distance(right_wrist, right_elbow);

            SkeletonPoint[] measuring_points = new SkeletonPoint[4];
            measuring_points[0] = sum(skel.Joints[JointType.ShoulderRight].Position, arm + 0.05, -0.05, 0);
            measuring_points[1] = sum(skel.Joints[JointType.ShoulderRight].Position, (arm + forearm) + 0.05, -0.05, 0);
            measuring_points[2] = sum(skel.Joints[JointType.ShoulderLeft].Position, -arm - 0.05, -0.05, 0);
            measuring_points[3] = sum(skel.Joints[JointType.ShoulderLeft].Position, -(arm + forearm) - 0.05, -0.05, 0);

            if (first_frame_measure == -1) {
                //Defines the measuring gesture
                JointType[] measuring_joints = new JointType[4];
                measuring_joints[0] = JointType.ElbowRight;
                measuring_joints[1] = JointType.HandRight;
                measuring_joints[2] = JointType.ElbowLeft;
                measuring_joints[3] = JointType.HandLeft;
                measuring = new Gesture(measuring_points, measuring_joints, my_KinectSensor, 1);

                // Begin the count
                first_frame_measure = actual_frame;
            }
            measuring.adjustLocations(measuring_points);
            measuring.adjustColor(skel, actual_frame);

            if (measuring.isCompleted()) {
                measured = true;
                first_frame_measure = -1;   // To ensure that the next time that an user need to be measured, he is
                state = States.PAUSED;
                this.statusBarText.Text = "";
                this.measure_imagen.Visibility = Visibility.Hidden;
            }
        }


        /// <summary>
        /// Acts according to user positions and application state
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void detect_skeletons_position(Skeleton skel, int actual_frame) {
            // The head point projection to the screen is compared with the guide line
            Point point_head = SkeletonPointToScreen(skel.Joints[JointType.Head].Position);

            if (state == States.SETTING_POSITION) {
                this.imagen.Visibility = Visibility.Hidden;

                if (Math.Abs(point_head.Y - height_up) < tolerance && Math.Abs(RenderWidth * 0.5 - point_head.X) < tolerance) {
                    situation_pen = new Pen(Brushes.Green, 6);

                    if (!measured)
                        state = States.MEASURING_USER;
                    else
                        state = States.PAUSED;

                    situated = true;
                }
                else {
                    // We give the instructions to situate the user
                    situation_pen = new Pen(Brushes.Red, 6);

                    if (actual_frame - first_wrong_frame < 60) {
                        this.statusBarText.Text = "Vamos a volver a coger \n la posición";
                    }
                    else if (point_head.Y > height_up + tolerance) {
                        this.statusBarText.Text = "Acércate";
                    }
                    else if (point_head.Y < height_up - tolerance) {
                        this.statusBarText.Text = "Aléjate";
                    }
                    else if (point_head.X > 0.5 * RenderWidth + tolerance) {
                        this.statusBarText.Text = "Muévete a la izquierda";
                    }
                    else if (point_head.X < 0.5 * RenderWidth - tolerance) {
                        this.statusBarText.Text = "Muévete a la derecha";
                    }

                }
            }
            else if (Math.Abs(point_head.Y - height_up) > tolerance || Math.Abs(RenderWidth * 0.5 - point_head.X) > tolerance) {
                if (actual_frame - first_wrong_frame > 30)
                    state = States.SETTING_POSITION;

                if (situated)
                    situated = false;
            }
            else {
                situated = true;
                first_wrong_frame = actual_frame;
            }

            if (state == States.MEASURING_USER) {
                measureUser(skel, actual_frame);
                initializeElements(skel);
            }
            if( state== States.PAUSED) {
                detect_shoot_movement(skel, actual_frame);
            }

            if (measured)
                exit.adjustColor(skel, actual_frame);
        }

        private Point compute_shoot(SkeletonPoint hand, SkeletonPoint elbow){
            Point point = new Point(-1,-1);
            SkeletonPoint proyection_point = new SkeletonPoint();
            proyection_point.Z =  hand.Z - 0.5f;

            if (elbow.Z - hand.Z > 0.05f) {
                float factor = (proyection_point.Z - hand.Z) / (elbow.Z - hand.Z);
                proyection_point.X =  elbow.X + factor * (elbow.X - hand.X);
                proyection_point.Y =  elbow.Y + factor * (elbow.Y - hand.Y);

                point = SkeletonPointToScreen(proyection_point);
            }

            return point;
        }

        private void detect_shoot_movement(Skeleton skel, int actual_frame) {
            Point shot_point = new Point(-1, -1);
            shoot_0.adjustColor(skel, actual_frame);
            SkeletonPoint hand = skel.Joints[JointType.HandRight].Position;
            SkeletonPoint elbow = skel.Joints[JointType.ElbowRight].Position;

            shoot_objective = compute_shoot(hand, elbow);

            if (!shoot_0.isSituated() && !pointed && !shooting) 
                shoot_0.adjustLocations(hand);

            if (shoot_0.isCompleted() && !shooting) {
                pointed = true;
                actual_shot_point = shoot_objective;
            }

            if (pointed && !shoot_0.isSituated()) {
                shooting = true;
                pointed = false;
                first_frame_shooting = actual_frame;
                shoot_1.adjustLocations(sum(elbow, -0.1 * Math.Sign(hand.X - elbow.X), 0.7 * forearm, 0));
            }

            if (shooting) {
                if (actual_frame - first_frame_shooting > 120) {
                    shooting = false;
                }

                shoot_1.adjustColor(skel, actual_frame);
                if (shoot_1.isCompleted()) {
                    first_frame_shoot = actual_frame;
                    shooting = false;
                }
            }

        }
    }


}