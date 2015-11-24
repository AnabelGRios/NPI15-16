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

using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace NPI_2 {
    /// <summary>
    /// 3D Points and joints collections to user interaction
    /// </summary>
    class Gesture {
        /// <summary>
        /// Kinect sensor to proyect the points that will be displayed
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// 3D-Locations, 2D-Proyected locations and joints that have to be in the correct positions.
        /// </summary>
        private SkeletonPoint[] locations;
        private Point[] screen_locations;
        private JointType[] joints;

        /// <summary>
        /// Colors and pens that will be used to indicate de distance or time that a joint has been the location
        /// </summary>
        private Brush[] distance_colors;
        private Brush time_color;
        private Pen[] pens;
        private float tolerance;

        /// <summary>
        /// Private states to determine if a gesture's been completed, the user's well situated...
        /// </summary>
        private bool situated = false;
        private bool timing = false;
        private bool completed = false;

        /// <summary>
        /// Second the joint's to be in the location and first frame it reaches the location
        /// </summary>
        private float seconds;
        private int first_frame;

        private Calculator calculator;

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        public Point SkeletonPointToScreen(SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Initialize distance and time colors.
        /// </summary>
        private void initializeColors() {
            distance_colors = new Brush[3];
            distance_colors[0] = Brushes.Green;
            distance_colors[1] = Brushes.Yellow;
            distance_colors[2] = Brushes.Red;

            for (int i = 0; i < pens.Length; i++) {
                pens[i] = new Pen(distance_colors[2], 6);
            }

            time_color = Brushes.ForestGreen;

        }

        /// <summary>
        /// Create a new instance of Gesture
        /// </summary>
        /// <param name="location">3Dpoint</param>
        /// <param name="joint">Joint must be in the location</param>
        /// <param name="sensor">Sensor to Maps a Skeleton to the screen</param>
        /// <param name="seconds">Seconds to maintain the position</param>
        /// <param name="tolerance">Tolerance of the error</param>
        public Gesture(SkeletonPoint location, JointType joint, KinectSensor sensor, float seconds, float tolerance = (float)0.15) {
            this.locations = new SkeletonPoint[1];
            this.locations[0] = location;

            this.joints = new JointType[1];
            this.joints[0] = joint;

            this.pens = new Pen[1];
            this.pens[0] = new Pen(Brushes.Blue, 6);

            this.sensor = sensor;
            this.screen_locations = new Point[1];
            this.screen_locations[0] = SkeletonPointToScreen(locations[0]);

            this.seconds = seconds;
            this.tolerance = tolerance;

            initializeColors();
            calculator = new Calculator();
        }

        /// <summary>
        /// Create a new instance of Gesture
        /// </summary>
        /// <param name="location">3Dpoint collection</param>
        /// <param name="joint">Joint collection must be in the location</param>
        /// <param name="sensor">Sensor to Maps a Skeleton to the screen</param>
        /// <param name="seconds">Seconds to maintain the position</param>
        /// <param name="tolerance">Tolerance of the error</param>
        public Gesture(SkeletonPoint[] locations, JointType[] joints, KinectSensor sensor, float seconds, float tolerance = (float)0.15) {
            this.locations = new SkeletonPoint[locations.Length];
            this.locations = locations;

            this.joints = new JointType[joints.Length];
            this.joints = joints;

            this.pens = new Pen[locations.Length];

            this.sensor = sensor;
            this.screen_locations = new Point[locations.Length];
            for (int i = 0; i < locations.Length; i++) {
                this.screen_locations[i] = SkeletonPointToScreen(locations[i]);
            }

            this.seconds = seconds;
            this.tolerance = tolerance;

            initializeColors();
            calculator = new Calculator();
        }

        /// <summary>
        /// Changes the distance colors
        /// </summary>
        /// <param name="distance">Selects the color to be changed</param>
        /// <param name="color">Gives the new color</param>
        public void setDistanceColor(int distance, Brush color) {
            if (distance_colors.Length > distance)
                distance_colors[distance] = color;
        }

        /// <summary>
        /// Changes the time color
        /// </summary>
        /// <param name="color">Gives the new color</param>
        public void setTimeColor(Brush color) {
            time_color = color;
        }

        /// <summary>
        /// Changes the color to all pens
        /// </summary>
        /// <param name="timing">Change pens colors if it's timing</param>
        private void changePensTimeColor(bool timing) {
            Brush color;
            if (timing)
                color = time_color;
            else
                color = distance_colors[0];

            foreach (Pen pen in pens) {
                pen.Brush = color;
            }
        }

        /// <summary>
        /// Changes the pens colors according to skeleton joint's positions
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="actual_frame"></param>
        public void adjustColor(Skeleton skeleton, int actual_frame) {
            int frames = (int)(seconds * 30);
            situated = true;

            for (int i = 0; i < locations.Length; i++) {
                SkeletonPoint joint_point = skeleton.Joints[joints[i]].Position;
                double distance = calculator.distance(joint_point, locations[i]);

                //Change colors according to distance
                if (distance < tolerance) {
                    pens[i].Brush = distance_colors[0];
                }
                else {
                    situated = false;
                    if (distance < 1.5 * tolerance)
                        pens[i].Brush = distance_colors[1];
                    else
                        pens[i].Brush = distance_colors[2];
                }
            }

            if (situated) {
                if (!timing) {
                    // Starts to timing
                    timing = true;
                    first_frame = actual_frame;
                }
                else {
                    if (actual_frame - first_frame < frames) {
                        completed = false;
                        // Use of the time_color if (actual_frame - first_frame) > (frames / 2)
                        changePensTimeColor((actual_frame - first_frame) > (frames / 2));
                    }
                    else {
                        completed = true;
                    }
                }
            }
            else {
                // End of timing
                timing = false;
                completed = false;
                first_frame = -1;
            }
        }

        /// <summary>
        /// Return if the gesture is completed
        /// </summary>
        /// <returns></returns>
        public bool isCompleted() {
            return completed;
        }

        /// <summary>
        /// Return if the joint is situated
        /// </summary>
        /// <returns></returns>
        public bool isSituated() {
            return situated;
        }

        /// <summary>
        /// Returns i Point
        /// </summary>
        /// <param name="i">Point identifier</param>
        /// <returns></returns>
        public SkeletonPoint getLocation(int i) {
            return locations[i];
        }

        /// <summary>
        /// Draw a circle
        /// </summary>
        /// <param name="dc">drawing context</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="i">Screen location of the centre</param>
        public void drawCircle(DrawingContext dc, float radius, int i = 0) {
            dc.DrawEllipse(null, pens[i], screen_locations[i], radius, radius);
        }

        /// <summary>
        /// Draw a circle
        /// </summary>
        /// <param name="dc">drawing context</param>
        /// <param name="radius">Radius of the cross</param>
        /// <param name="i">Screen location of the centre</param>
        public void drawCross(DrawingContext dc, float radius, int i = 0) {
            Point centre = screen_locations[i];
            if (centre.X < radius) {
                centre.X = radius;
            }
            if (centre.Y < radius) {
                centre.Y = radius;
            }
            dc.DrawLine(pens[i], new Point(centre.X - radius, centre.Y + radius), new Point(centre.X + radius, centre.Y - radius));
            dc.DrawLine(pens[i], new Point(centre.X - radius, centre.Y - radius), new Point(centre.X + radius, centre.Y + radius));
        }

        /// <summary>
        /// Get a specific Pen
        /// </summary>
        /// <param name="i">Number of the pen</param>
        public Pen getPen(int i = 0) {
            return pens[i];
        }

        /// <summary>
        /// Get a specific Point
        /// </summary>
        /// <param name="i">Number of point </param>
        public Point getPoint(int i = 0) {
            return screen_locations[i];
        }
        /// <summary>
        /// Adjust locations of the gesture
        /// </summary>
        /// <param name="new_locations">New locations of the gesture</param>
        public void adjustLocations(SkeletonPoint new_location) {
            locations[0] = new_location;
            screen_locations[0] = SkeletonPointToScreen(new_location);
        }


        /// <summary>
        /// Adjust locations of the gesture
        /// </summary>
        /// <param name="new_locations">New locations of the gesture</param>
        public void adjustLocations(SkeletonPoint[] new_locations) {
            for (int i = 0; i < locations.Length; i++) {
                locations[i] = new_locations[i];
                screen_locations[i] = SkeletonPointToScreen(new_locations[i]);
            }
        }

        /// <summary>
        /// Returns tolerance
        /// </summary>
        /// <returns></returns>
        public float getTolerance() {
            return tolerance;
        }
    }
}

