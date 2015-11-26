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
using System.Windows;
using System.Windows.Media.Imaging;

namespace NPI_2 {
    /// <summary>
    /// Objeto dinámico que incluye una imagen y con el cual el usuario puede interactuar.
    /// </summary>
    class InteractiveObject {

        
        private Image image;    // The image where the object will be shown

		private bool active = false;    // Estado del objeto

        private float frequency;    // Tiempo activo en pantalla
		float delay = -1;           // Retardo entre apariciones.

        private int first_active_frame = -1;        
        private int first_deactivate_frame = -1;

        private Calculator calculator;

		private const float RenderWidth = 640.0f;
		private const float RenderHeight = 480.0f;

        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="img">Image in the .xaml</param>
        /// <param name="picture">Path to the image</param>
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
        /// This metod changes the position where the picture will show randomly.
        /// Width and height are greater than RenderWidth and RenderHeight to force the images to reach the borders
        /// </summary>
        /// <param name="img"></param>
		public void changePosition(int actual_frame, int width = 745, int height = 570) {
			Thickness margin = image.Margin;

            double margin_left = calculator.getRandomNumber((int)(width - image.Width));
            double margin_right = RenderWidth - margin_left - image.Width;
            double margin_top = calculator.getRandomNumber((int)(height - image.Height));

			margin.Left = margin_left;
			margin.Right = margin_right;
			margin.Top = margin_top;

			image.Margin = margin;

            activate(actual_frame);
		}

        /// <summary>
        /// Devuelve si ha pasado el delay desde la última vez que fue desactivado el objeto
        /// </summary>
        /// <param name="actual_frame"></param>
        /// <returns></returns>
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
        /// This metod sets the picture that will be shown.
        /// </summary>
        /// <param name="picture"></param>
        public void changeImage(string path) {
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(path)));
        }

        /// <summary>
        /// This metod sets the frequency, in seconds, with which the picture will shown.
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

            // Adjust of the point due to the apparently different reference system
			point.X *= 1.16;
			point.Y *= 1.18;

            // Check if the shot is between the image margins and it reached after the object was active.
			if (image.Margin.Top <= point.Y && image.Margin.Top + image.Height >= point.Y &&
				image.Margin.Left + image.Width >= point.X && image.Margin.Left <= point.X &&
				hit_frame > first_active_frame && active) {
					hit = true;
                    deactivate(hit_frame);
			}
			return hit;
		}

        /// <summary>
        /// Método que comprueba si, dado el frame actual, el objeto está aún activo y lo cambia si no debe estarlo.
        /// </summary>
        /// <param name="actual_frame"></param>
        /// <returns></returns>
		public bool isDeactivated(int actual_frame) {
			bool in_time = (actual_frame < first_active_frame + getFrequency() && actual_frame >= first_active_frame);
			if (!in_time && active) {
                deactivate(actual_frame);
			}
			return !in_time;
		}

        /// <summary>
        /// Devuelve si el método debe estar activo
        /// </summary>
        /// <returns></returns>
		public bool isActive() {
			return active;
		}

        /// <summary>
        /// Activa el objeto
        /// </summary>
        /// <param name="frame"></param>
        public void activate(int frame) {
            first_active_frame = frame;
            active = true;
            image.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Desactiva el objeto
        /// </summary>
        /// <param name="frame"></param>
        public void deactivate(int frame) {
            first_deactivate_frame = frame;
            active = false;
            image.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Cambia el primer frame en el que se activará el objeto
        /// </summary>
        /// <param name="frame"></param>
        public void setFirstActiveFrame(int frame) {
            first_active_frame = frame;
        }

        /// <summary>
        /// Devuelve el frame en el que se activó por última vez el objeto.
        /// </summary>
        /// <returns></returns>
		public int getFirstFrame() {
			return first_active_frame;
		}

    }
}
