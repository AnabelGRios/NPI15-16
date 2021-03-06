﻿//------------------------------------------------------------------------------
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
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace NPI_2 {

    class Shoot{
        /// <summary>
        /// Information to know if the shot can be completed
        /// </summary>
        bool pointed = false;
        bool shooting = false;
        int first_frame_shooting = -1;

        /// <summary>
        /// Information to know the point we are aiming
        /// </summary>
        private Point shoot_objective;
        private int frame_shoot = -1;
        private Point actual_shot_point;

        private KinectSensor sensor;

        /// <summary>
        /// Array of gestures of the shooting movement
        /// </summary>
        private Gesture[] gestures;

        private Calculator calculator;
        private float forearm;

        /// <summary>
        /// Constructor of the shoot object
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="skel"></param>
        /// <param name="forearm"></param>
        public Shoot(KinectSensor sensor, Skeleton skel, float forearm) {
            SkeletonPoint hand = skel.Joints[JointType.HandRight].Position;
            SkeletonPoint elbow = skel.Joints[JointType.ElbowRight].Position;
            calculator = new Calculator();
            gestures = new Gesture[2];
            gestures[0] = new Gesture(hand, JointType.HandRight, sensor, 1, 0.08f);
            gestures[1] = new Gesture(calculator.sum(elbow, 0.05 * Math.Sign(hand.X - elbow.X), 0.9 * forearm, 0.9 * forearm), JointType.HandRight, sensor, 0.1f, 0.3f);
            this.sensor = sensor;
            this.forearm = forearm;
        }

        /// <summary>
        /// Returns the last point was shot and the frame the shoot happened
        /// </summary>
        /// <param name="frame_number"></param>
        /// <returns></returns>
		public Point getShotPointAndFrame(ref int frame_number) {
			frame_number = frame_shoot;
			return actual_shot_point;
		}

        /// <summary>
        /// Proyect the line of the arm in the plane
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="elbow"></param>
        /// <returns></returns>
        public Point compute_shoot(SkeletonPoint hand, SkeletonPoint elbow) {
            Point point = new Point(-1, -1);
            SkeletonPoint proyection_point = new SkeletonPoint();
            proyection_point.Z = hand.Z - 0.5f;

            if (elbow.Z - hand.Z > 0.05f ) {
                float factor = (proyection_point.Z - hand.Z) / (elbow.Z - hand.Z);
                proyection_point.X = elbow.X + factor * (elbow.X - hand.X);
                proyection_point.Y = elbow.Y + factor * (elbow.Y - hand.Y);
                point = calculator.SkeletonPointToScreen(sensor, proyection_point);
            }

            return point;
        }

        /// <summary>
        /// Draw the shoot points
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="actual_frame"></param>
        public void draw(DrawingContext dc, int actual_frame) {
           if (!shooting && !pointed) {
			   dc.DrawEllipse(null, new Pen(Brushes.LightSalmon, 6), shoot_objective, 20, 20);
           }
           else {
			   dc.DrawEllipse(null, new Pen(Brushes.LightSalmon, 6), shoot_objective, 20, 20);
               dc.DrawEllipse(null, new Pen(Brushes.Blue, 6), actual_shot_point, 15, 15);
           }

           if (gestures[1].isCompleted() && actual_frame - frame_shoot < 50) {
               dc.DrawEllipse(null, new Pen(Brushes.Gainsboro, 6), actual_shot_point, 20, 20);
           }
        }

        /// <summary>
        /// Adjust the point the user is shooting, if the shoot's been completed
        /// </summary>
        /// <param name="skel"></param>
        /// <param name="actual_frame"></param>
        public void detect_shoot_movement(Skeleton skel, int actual_frame) {
            Point shot_point = new Point(-1, -1);
            gestures[0].adjustColor(skel, actual_frame);
            SkeletonPoint hand = skel.Joints[JointType.HandRight].Position;
            SkeletonPoint elbow = skel.Joints[JointType.ElbowRight].Position;

            shoot_objective = compute_shoot(hand, elbow);

            // Change the first position if the hand is not still
            if (!gestures[0].isSituated() && !pointed)
                gestures[0].adjustLocations(hand);

            //  Change de actual shot point if the hand's been still for one second
            //  Don't change if it's pointed to avoid that the actual_shot_point follow the shoot_objective
            if (gestures[0].isCompleted() && !pointed && !shooting) {
                pointed = true;
                actual_shot_point = shoot_objective;
            }

            // Begin to shoot and change the gesture is checking
            if (pointed && !gestures[0].isSituated()) {
                shooting = true;
                pointed = false;
                first_frame_shooting = actual_frame;

                SkeletonPoint shoot_1_position = calculator.sum(elbow, - 0.1f * Math.Sign(hand.X - elbow.X),  0.7f * calculator.distance(hand, elbow), 0);
                gestures[1].adjustLocations(shoot_1_position);
            }

            if (shooting) {
                // Stop to check if the shoot is completed
                if (actual_frame - first_frame_shooting > 30) {
                    shooting = false;
                }


                gestures[1].adjustColor(skel, actual_frame);
                if (gestures[1].isCompleted()) {
                    frame_shoot = actual_frame;
                    shooting = false;
                }
            }

            // Deactive the last shoot 
            if (actual_frame - frame_shoot > 10) {
                frame_shoot = -1;
            }

        }
    }
}
