//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Methods SkeletonPointToScreen
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

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace NPI_2 {
    /// <summary>
    /// Métodos que realizan cálculos con la clase SkeletonPoint accesibles en varias clases
    /// </summary>
    class Calculator {
        /// <summary>
        /// Distancia euclídea entre dos puntos del espacio
        /// </summary>
        /// <param name="a">Primer punto</param>
        /// <param name="b">Segundo punto</param>
        /// <returns></returns>
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

        /// <summary>
        /// Método para obtener un entero aleatorio
        /// </summary>
        /// <param name="max_number"> Extremo superior del rango de números posibles</param>
        /// <returns></returns>
        public int getRandomNumber(int max_number) {
            Random random = new Random();
            return (random.Next() % max_number) + 1;
        }


    }
}
