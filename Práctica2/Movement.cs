//------------------------------------------------------------------------------
// Authors: Anabel Gómez Ríos, Jacinto Carrasco Castillo
// Date: 03-11-2015
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
    class Movement {
        private int actual_gesture;
        protected Gesture[] gestures;

        public Movement(Gesture[] gestures) {
            actual_gesture = 0;
            this.gestures = new Gesture[gestures.Length];
            this.gestures = gestures;
        }

        public Movement() { 

}

        public Gesture actualGesture() {
            return gestures[actual_gesture];
        }

        public bool isCompleted() {
            return gestures[gestures.Length].isCompleted();
        }

        /// <summary>
        /// Changes the movement status according to skeleton position
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="actual_frame"></param>
        public void adjustColor(Skeleton skeleton, int actual_frame) {
            gestures[actual_gesture].adjustColor(skeleton, actual_frame);

            if (gestures[actual_gesture].isCompleted()) {
                actual_gesture = (actual_gesture + 1) % gestures.Length;
            }
        }


    }
}
