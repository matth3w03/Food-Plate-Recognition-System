using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace plateDetection
{
    public partial class Form2 : Form
    {
        private const string SouthCentralUsEndpoint = "https://southcentralus.api.cognitive.microsoft.com";
        string predictionKey = "22bdf38731ec4be3b5890968bbdff4e1";
        string trainingKey = "4223fdb54c8d41dc8a8ad50591d1e035";
        private VideoCapture capture = null;
        private string projectId = "df31870b-2fe1-482e-a6d5-be21c13f6c7b";
        bool empty = false;

        public Form2()
        {
            InitializeComponent();
        }

        private void Capture_ImageGrabbed1(object sender, EventArgs e)
        {
            try
            {
                Mat m = new Mat();

                capture.Retrieve(m);
                Mat gray = new Mat(m.Size, DepthType.Cv8U, 1);
                CvInvoke.CvtColor(m, gray, ColorConversion.Bgr2Gray);
                Mat blur = new Mat(gray.Size, DepthType.Cv8U, 1);
                CvInvoke.MedianBlur(gray, blur, 11);
                pictureBox1.Image = m.ToImage<Bgr, byte>().Bitmap;

                CircleF[] circles = CvInvoke.HoughCircles(blur, HoughType.Gradient, 2, gray.Rows / 16, 20, 150, 80, 100);

                if (circles.Count() >= 1)
                {
                    Mat copy = m.Clone();
                    //img is the image you applied Hough to
                    m.Save("test.png");
                    for (int i = 0; i < circles.Count(); i++)
                    {
                        CvInvoke.Circle(copy, Point.Round(circles[i].Center), (int)circles[i].Radius, new MCvScalar(255, 0, 0), 3, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                        pictureBox1.Image = copy.ToImage<Bgr, byte>().Bitmap;
                    }
                    for (int i = 0; i < circles.Count(); i++)
                    {
                        string num = i.ToString();
                        string filepath = "test" + num + ".png";
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }

                        Image<Gray, byte> mask = new Image<Gray, byte>(m.Width, m.Height);
                        CvInvoke.Circle(mask, Point.Round(circles[i].Center), (int)circles[i].Radius, new MCvScalar(255, 255, 255), -1, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                        Image<Bgr, byte> dest = new Image<Bgr, byte>(m.Width, m.Height);
                        m.CopyTo(dest, mask);
                        dest.Save(filepath);

                        MakePredictionRequest(filepath);

                    }


                    //-1 is to fill the area
                    //detectPlate();
                    if (capture != null)
                    {
                        capture.ImageGrabbed -= Capture_ImageGrabbed1;
                        capture.Stop();
                        capture = null;
                    }

                }
                if (empty == true)
                {
                    Console.WriteLine("The tray contains an empty plate.");
                }
                m.Dispose();

            }
            catch (Exception)
            {

            }
        }

        private void MakePredictionRequest(string filepath)
        {
            CustomVisionTrainingClient trainingClient = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = SouthCentralUsEndpoint
            };
            // Create a prediction endpoint, passing in obtained prediction key
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = SouthCentralUsEndpoint
            };
            var project = trainingClient.GetProjects().FirstOrDefault();
            // Make a prediction against the new project
            Console.WriteLine("Making a prediction:");
            var result = endpoint.PredictImage(project.Id, new MemoryStream(File.ReadAllBytes(filepath)));

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            if (capture == null)
            {
                capture = new Emgu.CV.VideoCapture(1);
            }
            capture.ImageGrabbed += Capture_ImageGrabbed1;
            capture.Start();
        }
    }
}
