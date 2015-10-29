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

namespace NPI_1 {
    class Gesture {
        private SkeletonPoint[] locations;
        private Point[] screen_locations;
        private JointType[] joints;
        private Brush[] distance_colors;
        private Brush time_color;
        private Pen[] pens;
        private float tolerance;

        private bool situated = false;
        private bool timing = false;
        private bool completed = false;

        private float seconds;
        private int first_frame;

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint, KinectSensor sensor) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

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

        public Gesture(SkeletonPoint location, JointType joint, KinectSensor sensor, float seconds, float tolerance = (float)0.15) {
            this.locations = new SkeletonPoint[1];
            this.locations[0] = location;

            this.joints = new JointType[1];
            this.joints[0] = joint;

            this.pens = new Pen[1];
            this.pens[0] = new Pen(Brushes.Blue, 6);

            this.screen_locations = new Point[1];
            this.screen_locations[0] = SkeletonPointToScreen(locations[0], sensor);

            this.seconds = seconds;
            this.tolerance = tolerance;


            initializeColors();
        }

        public Gesture(SkeletonPoint[] locations, JointType[] joints, KinectSensor sensor, float seconds, float tolerance = (float)0.15) {
            this.locations = new SkeletonPoint[locations.Length];
            this.locations = locations;

            this.joints = new JointType[joints.Length];
            this.joints = joints;

            this.pens = new Pen[locations.Length];

            this.screen_locations = new Point[locations.Length];
            for (int i = 0; i < locations.Length; i++) {
                this.screen_locations[i] = SkeletonPointToScreen(locations[i], sensor);
            }

            this.seconds = seconds;
            this.tolerance = tolerance;

            initializeColors();
        }

        public void setDistanceColor(int distance, Brush color) {
            if (distance_colors.Length > distance)
                distance_colors[distance] = color;
        }

        public void setTimeColor(Brush color) {
            time_color = color;
        }

        private void changePensTimeColor(bool timing) {
            Brush color;
            if (timing) 
                color = time_color;
            else
                color = distance_colors[0];

            foreach(Pen pen in pens) {
                pen.Brush = color;
            }
        }

        public void adjustColor(Skeleton skeleton, int actual_frame) {
            int frames = (int)(seconds * 30);
            situated = true;

            for (int i = 0; i < locations.Length; i++){
                SkeletonPoint joint_point = skeleton.Joints[joints[i]].Position;
                double distance = Math.Sqrt((double)((locations[i].X - joint_point.X) * (locations[i].X - joint_point.X) +
                                     (locations[i].Y - joint_point.Y) * (locations[i].Y - joint_point.Y) +
                                     (locations[i].Z - joint_point.Z) * (locations[i].Z - joint_point.Z)));

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
                if(!timing) {
                    timing = true;
                    first_frame = actual_frame;
                }
                else {
                    if(actual_frame-first_frame < frames) {
                        completed = false;
                        changePensTimeColor((actual_frame - first_frame) > (frames / 2));
                    }
                    else {
                        completed = true;
                    }
                }
            }
            else {
                timing = false;
                completed = false;
                first_frame = -1;
            }
        }

        public bool isCompleted() {
            return completed;
        }

        public SkeletonPoint getLocation(int i) {
            return locations[i];
        }

        public void drawCircle(DrawingContext dc, float radius, int i = 0) {
            dc.DrawEllipse(null, pens[i], screen_locations[i], radius, radius);
        }

        public void drawCross(DrawingContext dc, float radius, int i = 0) {
            dc.DrawLine(pens[i], new Point(screen_locations[i].X - radius, screen_locations[i].Y + radius), new Point(screen_locations[i].X + radius, screen_locations[i].Y - radius));
            dc.DrawLine(pens[i], new Point(screen_locations[i].X - radius, screen_locations[i].Y - radius), new Point(screen_locations[i].X + radius, screen_locations[i].Y + radius));
        }

        public Pen getPen(int i = 0) {
            return pens[i];
        }

        public Point getPoint(int i = 0) {
            return screen_locations[i];
        }
    }

}
