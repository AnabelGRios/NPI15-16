using System;
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

namespace NPI_1
{
    enum States {SETTING_POSITION,CHECKING_GESTURE,CHECKING_MOVEMENT};

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        // Drawing group para la salida por pantalla
        private DrawingGroup drawingGroup;

        // Drawing image para la salida por pantalla
        private DrawingImage imageSource;

        // Sensor de Kinect que usaremos
        private KinectSensor my_KinectSensor;

        // Punto en la pantalla para guiar al usuario
        private Point guide_point;

        // Tolerancia del error de la posición
        private float tolerance;

        // Estado de la aplicación
        private States state;

        // Alturas para situar al usuario
        private float height_up, height_down;

        // Pen para pintar las lineas para situar al usuario
        private Pen situation_pen = new Pen(Brushes.Green, 6);

        public MainWindow(){
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e){

            this.drawingGroup = new DrawingGroup();
            this.imageSource = new DrawingImage(drawingGroup);
            draw_image.Source = imageSource;

            // Comprobamos si hay un sensor Kinect conectado
            if ( KinectSensor.KinectSensors.Count == 0){
                MessageBox.Show("Kinect Sensor is not connected");
            }
            else{
                //Comprobamos si el sensor conectado está disponible
                if (KinectSensor.KinectSensors[0].Status == KinectStatus.Connected)
                    my_KinectSensor = KinectSensor.KinectSensors[0];
                else
                    MessageBox.Show("Kinect Sensor is not ready");

                // Añadimos el manejador de eventos y el stream de la imagen en color
                my_KinectSensor.ColorFrameReady += My_KinectSensor_ColorFrameReady;
                my_KinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Añadimos el manejador de eventos y el stream del esqueleto
                my_KinectSensor.SkeletonFrameReady += My_KinectSensor_SkeletonFrameReady;
                my_KinectSensor.SkeletonStream.Enable();

                // Añadimos el manejador de eventos y el stream de la profundidad
                //my_KinectSensor.DepthFrameReady += My_KinectSensor_DepthFrameReady;
                //my_KinectSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Activamos el sensor Kinect
                my_KinectSensor.Start();
            }

        }

        private void My_KinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
            throw new NotImplementedException();
        }

        private void My_KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeleton_frame = e.OpenSkeletonFrame()) {
                if (skeleton_frame != null) {
                    skeletons = new Skeleton[skeleton_frame.SkeletonArrayLength];
                    skeleton_frame.CopySkeletonDataTo(skeletons);
                }
            }

            if (skeletons.Length != 0) {
                Skeleton skel = skeletons[0];

                if(state == States.SETTING_POSITION) {
                    Point point_head=SkeletonPointToScreen(skel.Joints[JointType.Head].Position);
                    Point point_foot_left = SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position);
                    Point point_foot_right = SkeletonPointToScreen(skel.Joints[JointType.FootRight].Position);
                   
                }
            }

        }
        
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.my_KinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        // Mostramos por pantalla la imagen obtenida
        private void My_KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
            using (ColorImageFrame es = e.OpenColorImageFrame()) {
                if (es != null) {
                    byte[] bits = new byte[es.PixelDataLength];
                    es.CopyPixelDataTo(bits);
                    Video_img.Source = BitmapSource.Create(es.Width, es.Height, 96, 96, PixelFormats.Bgr32, null, bits, es.Width * es.BytesPerPixel);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open()) {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));


                if (state == States.SETTING_POSITION) {
                    dc.DrawLine(situation_pen, new Point(0.4 * RenderWidth, 0.9 * RenderHeight), new Point(0.6 * RenderWidth, 0.9 * RenderHeight));
                    dc.DrawLine(situation_pen, new Point(0.4 * RenderWidth, 0.1 * RenderHeight), new Point(0.6 * RenderWidth, 0.1 * RenderHeight));
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (null != this.my_KinectSensor) {
                this.my_KinectSensor.Stop();
            }
        }


    }
}

