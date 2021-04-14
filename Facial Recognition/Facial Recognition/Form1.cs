using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Emgu.Util;

namespace Facial_Recognition
{
    public partial class Form1 : Form
    {
        #region Variables
        private Capture videoCapture = null;
        private Image<Bgr, Byte> currentFrame = null;
        Mat frame = new Mat();
        private bool facesDetectionEnabled = false;
        CascadeClassifier faceCascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
      /*  List<Image<Gray, Byte>> TrainedFaces = new List<Image<Gray, byte>>();
        List<int> PersonLabes = new List<int>(); */
        bool EnableSaveImage = false;
        Saving save;
        private static bool isTrained = false;

        Image<Gray, Byte>[] trainingImages;
        Image<Gray, Byte>[] trainingSizedImages;
        int imagesCount;
        int[] labels;
        EigenFaceRecognizer recognizer;
        string[] PersonName;

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            videoCapture = new Capture();
            videoCapture.ImageGrabbed += ProcessFrame;
            videoCapture.Start();
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            // Step 1: Video Capture
            videoCapture.Retrieve(frame, 0);
            currentFrame = frame.ToImage<Bgr, Byte>().Resize(picCapture.Width, picCapture.Height, Inter.Cubic);

            // Step 2: Face Detection
            if (facesDetectionEnabled)
            {
                // Convert from Bgr to Gray Image
                Mat grayImage = new Mat();
                CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                // Enhance the image to get better result
                CvInvoke.EqualizeHist(grayImage, grayImage);

                Rectangle[] faces = faceCascadeClassifier.DetectMultiScale(grayImage, 1.1, 3, Size.Empty, Size.Empty);
                // If Faces detected
                if (faces.Length > 0)
                {
                    // Draw a square around the face
                    foreach (var face in faces)
                    {
                        CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Red).MCvScalar, 2);

                        // Step 3: Add Person
                        // Assign the face to the picture Box face picDetected
                        Image<Bgr, byte>  resultImage = currentFrame.Convert<Bgr, byte>();
                        resultImage.ROI = face;
                        picDetected.SizeMode = PictureBoxSizeMode.StretchImage;
                        picDetected.Image = resultImage.Bitmap;

                        if (EnableSaveImage)
                        {
                            save.setResultImage(resultImage);                                            
                        }

                        // Show the results for the trained images
                        if (isTrained)
                        {
                            
                            Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, byte>().Resize(200,200, Inter.Cubic);
                            //var result = recognizer.Predict(grayFaceResult);
                            EigenFaceRecognizer.PredictionResult res = recognizer.Predict(grayFaceResult);
                            pictureBox3.Image = trainingSizedImages[res.Label].Bitmap;
                            Console.WriteLine(res.Label);
                            Console.WriteLine(res.Distance);
                            if (res.Distance < 100000 / 10)
                            {
                                CvInvoke.PutText(currentFrame, PersonName[res.Label], new Point(face.X - 2, face.Y - 2),
                                    FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            }
                            else
                            {
                                CvInvoke.PutText(currentFrame, "Unknown", new Point(face.X - 2, face.Y - 2),
                                    FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            }
                        }
                    }
                }
            }
            // Render the video capture into the Picturebox Capture
            picCapture.Image = currentFrame.Bitmap;
        }

        private void btnDetectFaces_Click(object sender, EventArgs e)
        {
            facesDetectionEnabled = true;

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
          //  btnSave.Enabled = false;
           // btnAddPerson.Enabled = false;
            EnableSaveImage = false;
            save.setSaveImageEnabled(EnableSaveImage);
        }

        private void btnAddPerson_Click(object sender, EventArgs e)
        {
          //  btnSave.Enabled = true;
          //  btnAddPerson.Enabled = false;
            
            // The only way i could get saving to work the way i wanted it to was by creating another object
            // then using the thread with the object
            save = new Saving();
            save.setPersonName(txtPersonName.Text);
            Thread saving = new Thread(new ThreadStart(save.Save));
            saving.Start();
            save.setSaveImageEnabled(true);
            EnableSaveImage = true;
            
            // Thread saveThread = new Thread( new ThreadStart(saveImage));
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            TrainImagesFromDir();
        }

        private void TrainImagesFromDir()
        {
            // http://www.emgu.com/wiki/files/3.1.0/document/html/c9c13b1e-4f15-31ea-9d40-4a76d5bee079.htm

            
            Size imageSize = new Size(200, 200);
            double Threshold = 100000;
           // TrainedFaces.Clear();
          //  PersonLabes.Clear();
           try
           {
                string path = Directory.GetCurrentDirectory() + @"\TrainedImages";
                string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
                imagesCount = files.Length;
                trainingImages = new Image<Gray, byte>[imagesCount];
                trainingSizedImages = new Image<Gray, byte>[imagesCount];
                labels = new int[imagesCount];
                PersonName = new string[imagesCount];
                // Adds all of the images to the array and resizes them all to the same size
                for (int i = 0; i < files.Length; i++)
                {
                    trainingImages[i] = new Image<Gray, byte>(files[i]);
                    trainingSizedImages[i] = new Image<Gray, byte>(files[i]);
                    CvInvoke.Resize(trainingImages[i],trainingSizedImages[i], imageSize);
                    labels[i] = i+1;
                    PersonName[i] = "Michael";
                }
                recognizer = new EigenFaceRecognizer(imagesCount, Threshold);
                // recognizer.Train(TrainedFaces.ToArray(), PersonLabes.ToArray());
                recognizer.Train(trainingSizedImages, labels);
                isTrained = true;
                Debug.WriteLine(imagesCount);
                Debug.WriteLine(isTrained);
               
           }
           catch(Exception ex)
           {             
                MessageBox.Show("Error in Trained Images: " + ex.Message);
                
            }
        }
    }
}
