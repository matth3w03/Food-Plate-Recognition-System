using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace plateDetection
{
    static class Program
    {


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        
        private static List<string> hemlockImages;

        private static List<string> japaneseCherryImages;

        private static MemoryStream testImage;

        static void Main()
        {
            using (var consoleWriter = new ConsoleWriter())
            {
                consoleWriter.WriteEvent += consoleWriter_WriteEvent;
                consoleWriter.WriteLineEvent += consoleWriter_WriteLineEvent;

                Console.SetOut(consoleWriter);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }

        private static void LoadImagesFromDisk()
        {
            // this loads the images to be uploaded from disk into memory
            hemlockImages = Directory.GetFiles(@"..\..\..\Images\Hemlock").ToList();
            japaneseCherryImages = Directory.GetFiles(@"..\..\..\Images\Japanese Cherry").ToList();
            testImage = new MemoryStream(File.ReadAllBytes(@"..\..\..\Images\SampleData\Test\test_image.jpg"));
        }

        static void consoleWriter_WriteLineEvent(object sender, Program.ConsoleWriterEventArgs e)
        {
            MessageBox.Show(e.Value, "WriteLine");
        }

        static void consoleWriter_WriteEvent(object sender, Program.ConsoleWriterEventArgs e)
        {
            MessageBox.Show(e.Value, "Write");
        }

        public class ConsoleWriterEventArgs : EventArgs
        {
            public string Value { get; private set; }
            public ConsoleWriterEventArgs(string value)
            {
                Value = value;
            }
        }

        public class ConsoleWriter : TextWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }

            public override void Write(string value)
            {
                if (WriteEvent != null) WriteEvent(this, new ConsoleWriterEventArgs(value));
                base.Write(value);
            }

            public override void WriteLine(string value)
            {
                if (WriteLineEvent != null) WriteLineEvent(this, new ConsoleWriterEventArgs(value));
                base.WriteLine(value);
            }

            public event EventHandler<ConsoleWriterEventArgs> WriteEvent;
            public event EventHandler<ConsoleWriterEventArgs> WriteLineEvent;
        }
    }
}
