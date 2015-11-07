using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NPI_2 {
    class InteractivObject {

        private float frequency;
        Image image;

        public InteractivObject(Image img, string picture, float freq) {
            image = img;
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../" + picture)));
            frequency = freq;
        }

        public int getRandomNumber(int max_number) {
            Random random = new Random();
            return (random.Next() % max_number) + 1;
        }

        public void setPosition(Image img) {
            image = img;
        }

        public void setPicture(string picture) {
            image.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("../../" + picture)));
        }

        public void setFrequency(float new_freq) {
            frequency = new_freq;
        }

    }
}
