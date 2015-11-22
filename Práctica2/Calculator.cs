using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace NPI_2 {
    class Calculator {

        public float distance(SkeletonPoint a, SkeletonPoint b) {
            float distance = (float)Math.Sqrt((double)(Math.Pow((a.X - b.X), 2) +
                Math.Pow((a.Y - b.Y), 2) +
                Math.Pow((a.Z - b.Z), 2)));

            return distance;
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="sensor">Sensor to proyect point</param>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        public Point SkeletonPointToScreen(KinectSensor sensor, SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Sum two SkeletonPoints
        /// </summary>
        /// <param name="first"> First point </param> 
        /// <param name="second"> Second point </param>
        /// <returns> Sum </returns> 
        public SkeletonPoint sum(SkeletonPoint first, SkeletonPoint second) {
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
        public SkeletonPoint sum(SkeletonPoint point, double x, double y, double z) {
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


    }
}
