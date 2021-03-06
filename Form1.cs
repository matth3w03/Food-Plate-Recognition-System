﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace plateDetection
{
    public partial class Form1 : Form
    {
        //Create variables
        private VideoCapture capture = null;
        private string json;
        int meatCount = 0;
        int vegCount = 0;
        double price = 0.0;
        int plateCount = 0;
        bool empty = false;

        public Form1()
        {
            InitializeComponent();
            this.pictureBox2.BackColor= TransparencyKey;
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

                CircleF[] circles = CvInvoke.HoughCircles(blur, HoughType.Gradient, 2, gray.Rows/16, 20, 150, 80, 100);

                if (capture != null)
                {
                    capture.ImageGrabbed -= Capture_ImageGrabbed1;
                    capture.Stop();
                    capture = null;
                }

                if (circles.Count() >= 1){
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
                    

                }
                if (empty == true)
                {
                    Console.WriteLine("The tray contains an empty plate.");
                }
                m.Dispose();
                
                Invoke(new Action(() =>
                {
                    pictureBox2.Visible = false;
                    label5.Refresh();
                }));

            }
            catch (Exception)
            {

            }
        }

        async Task MakePredictionRequest(string imageFilePath)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid subscription key.
            client.DefaultRequestHeaders.Add("Prediction-Key", "22bdf38731ec4be3b5890968bbdff4e1");

            // Prediction URL - replace this example URL with your valid prediction URL.
            string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/df31870b-2fe1-482e-a6d5-be21c13f6c7b/image?iterationId=8562d487-c25c-491c-bcfa-ca0dab597f79";

            HttpResponseMessage response;

            // Request body. Try this sample with a locally stored image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            
            Invoke(new Action(() =>
            {
                pictureBox2.Visible = true;
            }));

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                System.Threading.Thread.Sleep(1000);
                response = await client.PostAsync(url, content);

                //Retrieve JSON result
                string jsona = new JavaScriptSerializer().Serialize(await response.Content.ReadAsStringAsync());
                int jFrom = jsona.IndexOf("[");
                int jTo = jsona.LastIndexOf("]");
                string json = jsona.Substring(jFrom, jTo - jFrom);
                var charsToRemove = new string[] { @"\"};
                foreach (var c in charsToRemove)
                {
                    json = json.Replace(c, string.Empty);
                }

                //Remove all unnecessary characters & words
                json = RemoveWord(json, "tagName");
                json = RemoveWord(json, "probability");
                json = json.Replace("\"", "");
                json = json.Replace("]", "");
                json = json.Replace("}", "");
                json = json.Replace("[", "");
                json = json.Replace("{", "");
                json = json.Replace(":", "");
                json = json.Replace(" ", "");
                string[] jsons = json.Split(new char[] { ',' });
                //Console.WriteLine(json); Debug to check probabilities and tags

                //Creates a list of the results
                List<string> results = new List<string>();
                List<string> tags = new List<string>();
                List<float> prob = new List<float>();

                //TagId is not needed for results, so everything is added except for the line with TagId
                for(int i = 0; i < jsons.Length; i++)
                {
                    if(!jsons[i].Contains("Id"))
                    {
                        results.Add(jsons[i]);
                    }
                }
                string plateName = "";
                string chicken = "ChickenNuggets";
                string fries = "Fries";
                for(int i = 0; i < results.Count; i++)
                {
                    if (i % 2 != 0) // check odd - Tag Name
                    {
                        if(prob[prob.Count - 1] < 0.2)
                        {
                        }
                        else if(tags.Count < 1) //only get top result AKA HIGHEST PROBABILITY
                        {
                            tags.Add(results[i]);
                        }
                    }
                    else // check even - Probability
                    {
                        float a = float.Parse(results[i]);
                        prob.Add(a);              
                    }
                }

                foreach (string x in tags.ToList())
                {
                    //In each tags, if the tags contains veggies or meat, it will remove the tag and show the plate
                    if (x.Contains(fries))
                    {
                        vegCount++;
                        plateCount++;
                        price += 0.5;
                        tags.Remove(x);
                        plateName = "French Fries";

                        Invoke(new Action(() =>
                        {
                            RowStyle temp = tableLayoutPanel2.RowStyles[tableLayoutPanel2.RowCount - 1];
                        //increase panel rows count by one
                        tableLayoutPanel2.RowCount++;
                        //add a new RowStyle as a copy of the previous one
                        tableLayoutPanel2.RowStyles.Add(new RowStyle(temp.SizeType, 50));
                        //add your three controls
                        tableLayoutPanel2.Controls.Add(new Label() { Text = plateName, AutoSize = true}, 0, tableLayoutPanel2.RowCount - 1);
                        tableLayoutPanel2.Controls.Add(new Label() { Text = "$0.50", AutoSize = true }, 1, tableLayoutPanel2.RowCount - 1);
                       
                            label5.Text = "$" + price.ToString("0.##");
                        }));
                    }
                    else if (x.Contains(chicken))
                    {
                        meatCount++;
                        plateCount++;
                        price += 1.00;
                        tags.Remove(x);
                        plateName = "Chicken Nuggets";

                        Invoke(new Action(() =>
                        {
                            RowStyle temp = tableLayoutPanel2.RowStyles[tableLayoutPanel2.RowCount - 1];
                            //increase panel rows count by one
                            tableLayoutPanel2.RowCount++;
                            //add a new RowStyle as a copy of the previous one
                            tableLayoutPanel2.RowStyles.Add(new RowStyle(temp.SizeType, 50));
                            //add your three controls
                            tableLayoutPanel2.Controls.Add(new Label() { Text = plateName, AutoSize = true }, 0, tableLayoutPanel2.RowCount - 1);
                            tableLayoutPanel2.Controls.Add(new Label() { Text = "$1.00", AutoSize = true }, 1, tableLayoutPanel2.RowCount - 1);

                            label5.Text = "$" + price.ToString("0.##");
                        }));
                    }
                    else if (x.Contains("emptyplate"))
                    {
                            Console.WriteLine("The tray contains an empty plate.");
                        
                    }
                }
                
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("A staff will be coming shortly to assist you.");
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Controls.Add(pictureBox2);
            pictureBox2.BackColor = Color.Transparent;
            label5.Text = "$" + price;
            label5.Refresh();
            if (capture == null)
            {
                capture = new Emgu.CV.VideoCapture(1);
            }
            capture.ImageGrabbed += Capture_ImageGrabbed1;
            capture.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }
        private static string RemoveWord(string text, string word)
        {
            string result = string.Empty;

            string possibleMatch = string.Empty;
            for (int i = 0, j = 0; i < text.Length; i++)
            {
                if (text[i] == word[j])
                {
                    if (j == word.Length - 1) // match!
                    {
                        possibleMatch = string.Empty; // discard word!
                        j = 0; // restart!
                    }
                    else
                    {
                        // don't discard! It could match just partially!
                        possibleMatch += text[i];
                        j++;
                    }
                }
                else // don't match!
                {
                    // save possibleMatch
                    result += possibleMatch;
                    possibleMatch = string.Empty;

                    if (j == 0) // There is no way that current char can match anything...
                    {
                        result += text[i];
                    }
                    else // if it was in the middle of a search...
                    {
                        // current char doesn't match a char in the middle of 'word'...
                        // but it could match the beginning of 'word'!
                        // so...let's re-test!
                        j = 0;
                        i--;
                    }
                }
            }

            return result;
        }

        public void resetRows(TableLayoutPanel panel)
        {
            panel.Controls.Clear();
            panel.RowStyles.Clear();

            panel.RowCount = 1;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            panel.Controls.Add(new Label() { Text = "Plate Contents", Font = new Font("Arial", 18, FontStyle.Bold), ForeColor = Color.Black, AutoSize = true, Height = 50 }, 0, panel.RowCount - 1);
            panel.Controls.Add(new Label() { Text = "Price", Font = new Font("Arial", 18, FontStyle.Bold), ForeColor = Color.Black, AutoSize = true, Height = 50 }, 1, panel.RowCount - 1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            resetRows(tableLayoutPanel2);
            meatCount = 0;
            vegCount = 0;
            price = 0.0;
            plateCount = 0;
            empty = false;
            label5.Text = "$" + price;
            if (capture == null)
            {
                capture = new Emgu.CV.VideoCapture(1);
            }
            capture.ImageGrabbed += Capture_ImageGrabbed1;
            capture.Start();
        }
    }

    

}
