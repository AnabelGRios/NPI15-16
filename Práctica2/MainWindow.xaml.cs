//------------------------------------------------------------------------------
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
	using System.Windows.Controls;

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
        Gesture measuring;
        Gesture pause;

        Shoot shoot;

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

		/// <sumary>
		/// User's life in the game
		/// </sumary>
		private int life = 0;

        bool exit_hit = false;

		InteractiveObject dalton1, dalton2, fajita, lives_object;
        InteractiveObject exit_button, start_to_play_button;

        ///
        private Calculator calculator = new Calculator();

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
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


            calculator = new Calculator();

        }

        /// <summary>
        /// Show the color image that the sensor is receiving
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
            if (state == States.MEASURING_USER || state == States.SETTING_POSITION) {
                using (ColorImageFrame es = e.OpenColorImageFrame()) {
                    if (es != null) {
                        byte[] bits = new byte[es.PixelDataLength];
                        es.CopyPixelDataTo(bits);
                        video_image.Source = BitmapSource.Create(es.Width, es.Height, 96, 96, PixelFormats.Bgr32, null, bits, es.Width * es.BytesPerPixel);
                    }
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
                    case States.PLAYING:
                        pause.drawCircle(dc, 10, 0);
                        pause.drawCircle(dc, 10, 1);
                        shoot.draw(dc, actual_frame);
                        break;
                    case States.PAUSED:
                        shoot.draw(dc,actual_frame);
                        break;
                }

                if (exit_hit)
                    this.WindowClosing(sender, new System.ComponentModel.CancelEventArgs());

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
        /// Determine gestures and guides positions
        /// </summary>
        /// <param name="skel">Skeleton tracked to determine guides positions</param>
        private void initializeElements(Skeleton skel, int actual_frame) {
            SkeletonPoint[] pause_points = new SkeletonPoint[2];
            pause_points[0] = calculator.sum(skel.Joints[JointType.ShoulderRight].Position, arm, forearm, -0.1);
            pause_points[1] = calculator.sum(skel.Joints[JointType.ShoulderLeft].Position, -arm, -forearm, -0.1);
            JointType[] pause_joints = new JointType[2];
            pause_joints[0] = JointType.HandRight;
            pause_joints[1] = JointType.HandLeft;
            pause = new Gesture(pause_points, pause_joints, my_KinectSensor, 2);

            pause.setDistanceColor(1, Brushes.LightGray);
            pause.setDistanceColor(2, Brushes.Transparent);

            SkeletonPoint hand = skel.Joints[JointType.HandRight].Position;
            SkeletonPoint elbow = skel.Joints[JointType.ElbowRight].Position;

            shoot = new Shoot(my_KinectSensor , skel, forearm);

			dalton1 = new InteractiveObject(ref imageDalton1, "JoeDalton.png", 160, 60);
			dalton2 = new InteractiveObject(ref imageDalton2, "JoeDalton2.png", 300, 120, actual_frame+1000);
			lives_object = new InteractiveObject(ref life_image, "3.png", 0);
			fajita = new InteractiveObject(ref fajita_image, "fajita.png", 160, 500);

            exit_button = new InteractiveObject(ref exit_image, "salir.png", 0);
            start_to_play_button = new InteractiveObject(ref to_play_image, "iniciar_juego.png", 0);
			spock_hand.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/spock.png")));
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

            arm = calculator.distance(right_shoulder, right_elbow);
            forearm = calculator.distance(right_wrist, right_elbow);

            SkeletonPoint[] measuring_points = new SkeletonPoint[4];
            measuring_points[0] = calculator.sum(skel.Joints[JointType.ShoulderRight].Position, arm + 0.05, -0.05, 0);
            measuring_points[1] = calculator.sum(skel.Joints[JointType.ShoulderRight].Position, (arm + forearm) + 0.05, -0.05, 0);
            measuring_points[2] = calculator.sum(skel.Joints[JointType.ShoulderLeft].Position, -arm - 0.05, -0.05, 0);
            measuring_points[3] = calculator.sum(skel.Joints[JointType.ShoulderLeft].Position, -(arm + forearm) - 0.05, -0.05, 0);

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

            if (measuring.isCompleted() && !measured) {
                measured = true;
                first_frame_measure = -1;   // To ensure that the next time that an user need to be measured, he is
                this.measure_imagen.Visibility = Visibility.Hidden;
                initializeElements(skel, actual_frame);
                beginGame(actual_frame);
            }
        }


        /// <summary>
        /// Acts according to user positions and application state
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void detect_skeletons_position(Skeleton skel, int actual_frame) {
            // The head point projection to the screen is compared with the guide line
            Point point_head = calculator.SkeletonPointToScreen(my_KinectSensor,skel.Joints[JointType.Head].Position);

            if (state == States.SETTING_POSITION) {

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
                /*if (actual_frame - first_wrong_frame > 30)
                    state = States.SETTING_POSITION;*/

                if (situated)
                    situated = false;
            }
            else {
                situated = true;
                first_wrong_frame = actual_frame;
            }

            if (state == States.MEASURING_USER) {
                measureUser(skel, actual_frame);
            }

			if (state == States.PLAYING) {
				int shot_frame = -1;
				Point shot_point;
				bool dead = false;
				bool dead_2 = false;
				
				shoot.detect_shoot_movement(skel, actual_frame);
				shot_point = shoot.getShotPointAndFrame(ref shot_frame);
				
				dead = dalton1.isHit(shot_point, shot_frame);
				dead_2 = dalton2.isHit(shot_point, shot_frame);

				if (dead || dead_2) {
					if (shot_frame < dalton1.getFirstFrame())
						dead = false;
					if (shot_frame < dalton2.getFirstFrame())
						dead_2 = false;
				}

				if (dalton1.isActive() && dalton1.isDeactivated(actual_frame)) {
					if (!dead) {
						life--;
						lives_object.changeImage(life);
						if (life == 0) {
							beginPause(actual_frame);
						}
					}
				}

				if (!dalton1.isActive() && dalton1.past_delay(actual_frame)) {
					dalton1.changePosition(actual_frame);
				}

				if (dalton2.isActive() && dalton2.isDeactivated(actual_frame)) {
					if (!dead_2) {
						life--;
						lives_object.changeImage(life);
						if (life == 0) {
                            beginPause(actual_frame);
						}
					}
				}

				if (!dalton2.isActive() && dalton2.past_delay(actual_frame)) {
					dalton2.changePosition(actual_frame);
				}

				if (life < 3 && fajita.isActive() && !fajita.isDeactivated(actual_frame)) {
					Point left_hand = calculator.SkeletonPointToScreen(my_KinectSensor, skel.Joints[JointType.HandLeft].Position);
					if (fajita.isHit(left_hand, actual_frame)) {
						life++;
						lives_object.changeImage(life);
					}

					Thickness margin = spock_hand.Margin;

					double margin_left = left_hand.X - spock_hand.Width/2;
					double margin_right = RenderWidth - margin_left - spock_hand.Width;
					double margin_top = left_hand.Y - spock_hand.Height / 2;

					margin.Left = margin_left;
					margin.Right = margin_right;
					margin.Top = margin_top;

					spock_hand.Margin = margin;
					spock_hand.Visibility = Visibility.Visible;
				}
				else {
					spock_hand.Visibility = Visibility.Hidden;
				}

				if (life < 3 && !fajita.isActive() && fajita.past_delay(actual_frame)) {
					fajita.changePosition(actual_frame, 300);
				}

                pause.adjustColor(skel, actual_frame);
                SkeletonPoint[] pause_points = new SkeletonPoint[2];
                pause_points[0] = calculator.sum(skel.Joints[JointType.ShoulderRight].Position, arm, forearm, -0.1);
                pause_points[1] = calculator.sum(skel.Joints[JointType.ShoulderLeft].Position, -arm, forearm, -0.1);
                pause.adjustLocations(pause_points);
                if (pause.isCompleted()) {
                    beginPause(actual_frame);
                }

			}

            if( state== States.PAUSED) {
                int shot_frame = -1;
                Point shot_point;
                shoot.detect_shoot_movement(skel, actual_frame);
                shot_point = shoot.getShotPointAndFrame(ref shot_frame);
                
				shot_point.X += 100;
				
                if ( exit_button.isHit(shot_point, shot_frame)) {
                        exit_hit = true;
                }

                if ( start_to_play_button.isHit(shot_point, shot_frame) ) {
                    beginGame(actual_frame);
                }
            }
        }

        private void beginGame(int frame) {
            state = States.PLAYING;
            this.statusBarText.Text = "";
            messages_image.Visibility = Visibility.Hidden;
            lives_object.activate(frame);
            dalton1.activate(frame);
            exit_button.deactivate(frame);
            start_to_play_button.deactivate(frame);
            video_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/desert-landscape.png")));
            fajita.setFirstActiveFrame(frame + 500);

            if (life == 0) {
                life = 3;
                lives_object.changeImage(3);
                dalton2.setFirstActiveFrame(frame+1000);
            }
        }

        private void beginPause(int frame) {
            state = States.PAUSED;

            exit_button.activate(frame);
            start_to_play_button.activate(frame);

            if (life == 0) {
                start_to_play_button.changeImage("../../images/iniciar_juego.png");
                messages_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/game_over.png")));
                messages_image.Visibility = Visibility.Visible;
            }
            else {
                start_to_play_button.changeImage("../../images/volver_juego.png");
            }

        }
    }
}