//------------------------------------------------------------------------------
// Authors: Anabel Gómez Ríos, Jacinto Carrasco Castillo
// Date: 07-11-2015
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
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NPI_2 {
    class InteractiveObject {

        private float frequency;    // Frequency the object is shown
        Image image;    // The object that will be shown


        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="img"></param>
        /// <param name="picture"></param>
        /// <param name="freq"></param>
        public InteractiveObject(Image img, string picture, float freq) {
            image = img;
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../" + picture)));
            frequency = freq;
        }

        /// <summary>
        /// This metod sets the position where the picture will show.
        /// </summary>
        /// <param name="img"></param>
        public void setPosition(Image img) {
            image = img;
        }

        /// <summary>
        /// This metod sets the picture that will be show.
        /// </summary>
        /// <param name="picture"></param>
        public void setPicture(string picture) {
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../" + picture)));
        }

        /// <summary>
        /// This metod sets the frequency, in seconds, with which the picture will show.
        /// </summary>
        /// <param name="new_freq"></param>
        public void setFrequency(float new_freq) {
            frequency = new_freq;
        }

    }
}
