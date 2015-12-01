//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Methods WindowLoaded
//     Variables RenderWidth, RenderHeight, drawingGroup
//     XAML Code
// </copyright>
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
// Authors: Anabel Gómez Ríos, Jacinto Carrasco Castillo
// Date: 26-11-2015
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
    enum States { SETTING_POSITION, MEASURING_USER, PLAYING, PAUSED, TUTORIAL };

    enum GameMode { POINTS, SURVIVE };

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

        /// <summary>
        /// Shoot object to interactuate with the application.
        /// </summary>
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
        private int points = 0;
        private int first_game_frame = 0;

        /// <summary>
        /// If the user's shot in the exit button
        /// </summary>
        bool exit_hit = false;

		/// <summary>
		/// Control the time of the tutorial images
		/// </summary>
		int first_tutorial_image_first_frame = -1;
		int second_tutorial_image_first_frame = -1;
		int third_tutorial_image_first_frame = -1;

        /// <summary>
        /// Interactive objects to interactuate with and buttons
        /// </summary>
		InteractiveObject dalton1, dalton2, fajita, lives_object;
        InteractiveObject exit_button, back_to_game_button, exit_tutorial, point_mode_button, survive_mode_button;

        /// <summary>
        /// Actual game mode
        /// </summary>
        GameMode game_mode;

        /// <summary>
        /// Calculator object to calculate distances and projections.
        /// </summary>
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
            if (state != States.PLAYING && state != States.PAUSED) {
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
                    case States.TUTORIAL:
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
                to_play_image.Visibility = Visibility.Hidden;
                exit_image.Visibility = Visibility.Hidden;
				imageDalton1.Visibility = Visibility.Hidden;
				imageDalton2.Visibility = Visibility.Hidden;
				life = 0;
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
			dalton2 = new InteractiveObject(ref imageDalton2, "JoeDalton2.png", 300, 120);
			lives_object = new InteractiveObject(ref life_image, "3.png", 0);
			fajita = new InteractiveObject(ref fajita_image, "fajita.png", 160, 500);

            exit_button = new InteractiveObject(ref exit_image, "salir.png", 0);
            back_to_game_button = new InteractiveObject(ref to_play_image, "volver_juego.png", 0);
            exit_tutorial = new InteractiveObject(ref exit_tutorial_image, "salir_tutorial.png", 0);
            point_mode_button = new InteractiveObject(ref points_image, "modo_puntuacion.png", 0);
            survive_mode_button = new InteractiveObject(ref survive_image, "modo_superviviente.png", 0);

			spock_hand.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/spock.png")));
			tutorial_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/gesto_1.png")));

			first_tutorial_image_first_frame = actual_frame;
			second_tutorial_image_first_frame = actual_frame + 250;
			third_tutorial_image_first_frame = actual_frame + 500;
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
				state = States.TUTORIAL;
                exit_tutorial.activate(actual_frame);
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
			int shot_frame = -1;
			Point shot_point;

            if (state == States.SETTING_POSITION) {

                if (Math.Abs(point_head.Y - height_up) < tolerance && Math.Abs(RenderWidth * 0.5 - point_head.X) < tolerance) {
                    situation_pen = new Pen(Brushes.Green, 6);

                    if (!measured)
                        state = States.MEASURING_USER;
                    else
                        beginPause(actual_frame);

                }
                else {
                    // We give the instructions to situate the user
                    situation_pen = new Pen(Brushes.Red, 6);

                    if (point_head.Y > height_up + tolerance) {
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
			
			if (state == States.MEASURING_USER) {
                measureUser(skel, actual_frame);
            }

			if (state == States.TUTORIAL) {
				if (actual_frame - first_tutorial_image_first_frame < 250) {
					tutorial_image.Visibility = Visibility.Visible;
					this.statusBarText.Text = "Ponte en esta posición para \n apuntar. Habrás apuntado \n cuando salga el círculo azul";
				}
				else if (actual_frame - second_tutorial_image_first_frame < 250) {
					tutorial_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/gesto_2.png")));
					this.statusBarText.Text = "Una vez hayas apuntado, \n sube el brazo rápido para \ndisparar. El círculo se pondrá \ngris.";
				}
				else if (actual_frame - third_tutorial_image_first_frame < 250) {
					tutorial_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/gesto_3.png")));
					this.statusBarText.Text = "Puedes ponerte en esta \nposición para ir al menú \npausa.";
				}
				else {
					this.statusBarText.Text = "";
					tutorial_image.Visibility = Visibility.Hidden;
					beginPause(actual_frame);
				}

				shoot.detect_shoot_movement(skel, actual_frame);
				shot_point = shoot.getShotPointAndFrame(ref shot_frame);

                shot_point.X += 100;

                //Comprueba si hemos dado al botón para saltarnos el tutorial
                if (exit_tutorial.isHit(shot_point, shot_frame)) {
                    this.statusBarText.Text = "";
                    tutorial_image.Visibility = Visibility.Hidden;
                    beginPause(actual_frame);
                }
			}

			if (state == States.PLAYING) {
				bool dead = false;
				bool dead_2 = false;

				shoot.detect_shoot_movement(skel, actual_frame);
				shot_point = shoot.getShotPointAndFrame(ref shot_frame);
				
                // Comprobamos si hemos matado a uno de los hermanos Dalton
				dead = dalton1.isHit(shot_point, shot_frame);
				dead_2 = dalton2.isHit(shot_point, shot_frame);

                // Comprobamos si no ha muerto y debe quitarnos una vida
				if (dalton1.isActive() && !dead && dalton1.isDeactivated(actual_frame)) {
                    if(game_mode == GameMode.SURVIVE)
                        takeALife(actual_frame);
				}

                // Comprobamos si debe volver a aparecer
				if (!dalton1.isActive() && dalton1.past_delay(actual_frame)) {
					dalton1.changePosition(actual_frame);
				}

                // Comprobamos si no ha muerto y debe quitarnos una vida
                if (dalton2.isActive() && !dead_2 && dalton2.isDeactivated(actual_frame)) {
                    if (game_mode == GameMode.SURVIVE)
                        takeALife(actual_frame);
				}

                // Comprobamos si debe volver a aparecer
				if (!dalton2.isActive() && dalton2.past_delay(actual_frame)) {
					dalton2.changePosition(actual_frame);
				}


                // Hacemos aparecer una fajita si ha pasado un determinado tiempo y el jugador tiene menos de 3 vidas
                if (life < 3 && !fajita.isActive() && fajita.past_delay(actual_frame)) {
                    fajita.changePosition(actual_frame, 300);
                }

                // Comprobamos que el jugador ha cogido la fajita con la mano izquierda
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

                // Acciones a realizar si el modo de juego es Puntuación
                if (game_mode == GameMode.POINTS) {
                    // Acaba el juego
                    if (actual_frame - first_game_frame > 2700) {
                        beginPause(actual_frame);
                    }

                    // Suma 
                    if (dead) {
                        points += dalton1.getFrequency() - (shot_frame - dalton1.getFirstFrame());
                    }

                    if (dead_2) {
                        points += dalton2.getFrequency() - (shot_frame - dalton2.getFirstFrame());
                    }

                    pointsText.Text = points.ToString();
                }

                // Comprobamos si el jugador quiere pausar el juego.
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

				shoot.detect_shoot_movement(skel, actual_frame);
				shot_point = shoot.getShotPointAndFrame(ref shot_frame);
                
				shot_point.X += 100;
				
                // Comprobamos si el jugador le ha dado a uno de los dos botones del menú
                if ( exit_button.isHit(shot_point, shot_frame)) {
                    exit_hit = true;
                }

                // Vuelve al juego pausado
                if (back_to_game_button.isHit(shot_point, shot_frame)) {
                    beginGame(actual_frame);
                }

                // Comienza el juego en el modo puntuación
                if (point_mode_button.isHit(shot_point, shot_frame)) {
                    beginPointsMode(actual_frame);
                }

                // Comienza el juego en el modo superviviente
                if (survive_mode_button.isHit(shot_point, shot_frame)) {
                    beginSurviveMode(actual_frame);
                }

            }
        }

        /// <summary>
        /// Realiza los cambios visuales necesarios para entrar en el juego
        /// </summary>
        /// <param name="frame"></param>
        private void beginGame(int frame) {
            state = States.PLAYING;

            this.statusBarText.Text = "";
            messages_image.Visibility = Visibility.Hidden;
            exit_button.deactivate(frame);
            back_to_game_button.deactivate(frame);
            survive_mode_button.deactivate(frame);
            point_mode_button.deactivate(frame);

            video_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/desert-landscape.png")));

            dalton1.activate(frame);
            fajita.setFirstActiveFrame(frame + 1000);

            // Pone el contador de vidas a 3 para iniciar un nuevo juego
            if (life == 0) {
                life = 3;
                lives_object.changeImage(3);
                dalton2.setFirstActiveFrame(frame+1000);

                dalton1.setFrequency(160);
                dalton2.setFrequency(300);
            }
        }

        /// <summary>
        /// Realiza los cambios visuales necesarios para entrar en el estado de pausa
        /// </summary>
        /// <param name="frame"></param>
        private void beginPause(int frame) {

            exit_tutorial.deactivate(frame);
            state = States.PAUSED;

			video_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/desert-landscape.png")));
            exit_button.activate(frame);
            survive_mode_button.activate(frame);
            point_mode_button.activate(frame);
            
        
            if(life != 0) {
                back_to_game_button.activate(frame);
            }

        }

        /// <summary>
        /// Realiza los cambios visuales necesarios para comenzar el juego en modo Superviviente
        /// </summary>
        /// <param name="frame"></param>
        private void beginSurviveMode(int frame) {
            game_mode = GameMode.SURVIVE;

            life = 0;
            lives_object.activate(frame);
            this.pointsText.Text = "";
            beginGame(frame);
        }

        /// <summary>
        /// Realiza los cambios visuales necesarios para comenzar el juego en modo Puntuación
        /// </summary>
        /// <param name="frame"></param>
        private void beginPointsMode(int frame) {
            game_mode = GameMode.POINTS;

            lives_object.deactivate(frame);
            first_game_frame = frame;
            points = 0;
            this.pointsText.Text = points.ToString();
            beginGame(frame);
        }

        /// <summary>
        /// Resta una vida
        /// </summary>
        /// <param name="frame"></param>
        private void takeALife(int frame) {
            life--;
            lives_object.changeImage(life);
            if (life == 0) {
                messages_image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/game_over.png")));
                messages_image.Visibility = Visibility.Visible;
                beginPause(frame);
            }
        }


    }
}