﻿//------------------------------------------------------------------------------
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
using System.Windows;
using System.Windows.Media.Imaging;

namespace NPI_2 {
    class InteractiveObject {

        private float frequency;    // Frequency the object is show
        private Image image;    // The object that will be shown
		private Calculator calculator;
		private int first_active_frame = -1;
		private int first_deactivate_frame = -1;
		private bool active = false;
		float delay = -1;

		private const float RenderWidth = 640.0f;
		private const float RenderHeight = 480.0f;

        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="img"></param>
        /// <param name="picture"></param>
        /// <param name="freq"></param>
        public InteractiveObject(ref Image img, string picture, float freq, float delay = 60, int first_frame = -1) {
            image = img;
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../images/" + picture)));
            frequency = freq;
			calculator = new Calculator();
			first_active_frame = first_frame;
			this.delay = delay;
        }

        /// <summary>
        /// This metod sets the position where the picture will show.
        /// </summary>
        /// <param name="img"></param>
		public void changePosition(int actual_frame, int widht = 765, int heigh = 589) {
			Thickness margin = image.Margin;

			double margin_left = calculator.getRandomNumber((int) (widht - image.Width));
			double margin_right = RenderWidth - margin_left - image.Width;
			double margin_top = calculator.getRandomNumber((int) (heigh - image.Height));

			margin.Left = margin_left;
			margin.Right = margin_right;
			margin.Top = margin_top;

			image.Margin = margin;

			first_active_frame = actual_frame;
			active = true;
			image.Visibility = Visibility.Visible;
		}

		public bool past_delay(int actual_frame) {
			return first_deactivate_frame + delay < actual_frame && first_active_frame < actual_frame;
		}

        /// <summary>
        /// This metod sets the picture that will be show.
        /// </summary>
        /// <param name="picture"></param>
		public void changeImage(int num) {
			string number = num.ToString();
			string name = "../../images/" + number + ".png";
			image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(name)));
		}

        /// <summary>
        /// This metod sets the frequency, in seconds, with which the picture will show.
        /// </summary>
        /// <param name="new_freq"></param>
        public void setFrequency(float new_freq) {
            frequency = new_freq;
        }

		/// <summary>
		/// This metods returns the frequency of the object.
		/// </summary>
		/// <returns></returns>
		public float getFrequency() {
			return frequency;
		}

		/// <summary>
		/// This metod says if the user has interactuated with the object.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="current_image"></param>
		/// <returns></returns>
		public bool isHit(Point point, int hit_frame) {
			bool hit = false;
			if (image.Margin.Top <= point.Y && image.Margin.Top + image.Height >= point.Y &&
				image.Margin.Left + image.Width >= point.X && image.Margin.Left <= point.X &&
				hit_frame > first_active_frame && active) {
					hit = true;
					active = false;
					first_deactivate_frame = hit_frame;
					image.Visibility = Visibility.Hidden;
			}
			return hit;
		}

		public bool isDeactivated(int actual_frame) {
			bool in_time = (actual_frame < first_active_frame + getFrequency() && actual_frame > first_active_frame);
			if (!in_time && active) {
				first_deactivate_frame = actual_frame;
				image.Visibility = Visibility.Hidden;
				active = false;
			}
			return !in_time;
		}

		public bool isActive() {
			return active;
		}

    }
}
