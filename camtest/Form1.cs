using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using System.Windows.Forms.VisualStyles;
using System.Collections.Generic;
using MassTransit.Util;

namespace camtest
{



    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        static List<Task> bitmapTasks = new List<Task>();
        static int CountFrame = 0;
        static int[,] CurrentCoord;
        static double H, S, V;
        static Dictionary<Color, int> histogram;
        static Dictionary<int, int[]> coordRectanles;
        static List<int[]> ROI = new List<int[]>();
        static int width;
        static int height;
        static double expH;
        static double expS;
        static double expV;
        static double expR;
        static double expG;
        static double expB;
        static Graphics g;
        static Pen Red = new Pen(Color.Red, 1);
        static double DiamtrHands;
        static double HeightHands;
        static int CenterX = 0;
        static int CenterY = 0;
        static int LeftX = 0;
        static int LeftY = 0;
        static int[] histR = new int[255];
        static int[] histG = new int[255];
        static int[] histB = new int[255];
        static int[] histH = new int[360];
        static int[] histS = new int[100];
        static int[] histV = new int[100];
        static int secX = 0;
        static int secY = 0;
        static bool isDetect = false;
        static int deltaX;
        static int deltaY;
        static int currentSecX;
        static int currentSecY;
        Graphics gPanel;
        TaskFactory factory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(2));
        List<Task> tasks = new List<Task>();
        float currentPointX = 0;
        float currentPointY = 0;
        float prevPointX = 0;
        float prevPointY = 0;
        public static void initcoordRectanles(int width, int height)
        {
            if (coordRectanles == null)
            {
                coordRectanles = new Dictionary<int, int[]>();
                coordRectanles.Add(0, new int[2] { width / 2 - 50, height / 2 - 50 });
                coordRectanles.Add(1, new int[2] { width / 2 - 30, height / 2 - 50 });
                coordRectanles.Add(2, new int[2] { width / 2 - 10, height / 2 - 50 });
                coordRectanles.Add(3, new int[2] { width / 2 - 50, height / 2 - 30 });
                coordRectanles.Add(4, new int[2] { width / 2 - 30, height / 2 - 30 });
                coordRectanles.Add(5, new int[2] { width / 2 - 10, height / 2 - 30 });
                coordRectanles.Add(6, new int[2] { width / 2 - 50, height / 2 - 10 });
                coordRectanles.Add(7, new int[2] { width / 2 - 30, height / 2 - 10 });
                coordRectanles.Add(8, new int[2] { width / 2 - 10, height / 2 - 10 });
                coordRectanles.Add(9, new int[2] { width / 2 - 50, height / 2 + 10 });
                coordRectanles.Add(10, new int[2] { width / 2 - 30, height / 2 + 10 });
                coordRectanles.Add(11, new int[2] { width / 2 - 10, height / 2 + 10 });
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            gPanel = panel1.CreateGraphics();
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo filterInfo in filterInfoCollection)
            {
                cboCamera.Items.Add(filterInfo.Name);
            }
            cboCamera.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice();

            videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cboCamera.SelectedIndex].MonikerString);
            videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
            videoCaptureDevice.Start();
            panel1.Width = pic.Width;
            panel1.Height = pic.Height;
        }


        public static void expected_valueRGB()
        {
            int lenVal = ROI.Count;
            double p = (double)1 / lenVal;
            expR = 0;
            expG = 0;
            expB = 0;


            for (int i = 0; i < lenVal; i++)
            {
                expR += ROI[i][0] * p;
            }

            for (int i = 0; i < lenVal; i++)
            {
                expG += ROI[i][1] * p;
            }

            for (int i = 0; i < lenVal; i++)
            {
                expB += ROI[i][2] * p;
            }
        }

        //Мат ожидания для моих каналов
        public static void expected_value()
        {
            int lenVal = ROI.Count;
            double p = (double) 1 / lenVal;
            expH = 0;
            expS = 0;
            expV = 0;
       
            for (int i = 0; i < lenVal; i++)
            {
                expH += ROI[i][0] * p;
            }

            for (int i = 0; i < lenVal; i++)
            {
                expS += ROI[i][1] * p;
            }

            for (int i = 0; i < lenVal; i++)
            {
                expV += ROI[i][2] * p;
            }

        }

        public static void toHSV(RGB rgb)
        {
            double R = (double)rgb.Red/(rgb.Red+rgb.Green+rgb.Blue);
            double G = (double)rgb.Green/(rgb.Red + rgb.Green + rgb.Blue);
            double B = (double)rgb.Blue/(rgb.Red + rgb.Green + rgb.Blue);
            double max = Math.Max(R, Math.Max(G, B));
            double min = Math.Min(R, Math.Min(G, B));
            
            if (min == max) H = 0;
            else if (max == R && G >= B) H = 60*(G - B) / (max - min);
            else if (max == R && G < B) H =  60*(G - B) / (max - min) + 360;
            else if (max == G) H =  60*(B - R) / (max - min) + 120;
            else if (max == B) H =  60*(R - G) / (max - min) + 240;

            if (max == 0) S = 0;
            else
                S = (1 - min / max)*100;

            V = max*100;

        }



        public static void MakeHistogram()
        {
            for(int i = 0; i < ROI.Count(); i++)
            {
                histR[(int)ROI[i][0]]++;
                histG[(int)ROI[i][1]]++;
                histB[(int)ROI[i][2]]++;
            }

            for (int i = 0; i < ROI.Count(); i++)
            {
                toHSV(new RGB((byte)ROI[i][0], (byte)ROI[i][1], (byte)ROI[i][2]));
                histH[(int)H]++;
                histG[(int)S]++;
                histB[(int)V]++;
            }
        }

        public static void convertB(Bitmap bitmap)
        {
            List<Rectangle> rects = new List<Rectangle>();

            g = Graphics.FromImage(bitmap); 
            
            int sum = 0;
            double[,,] HSV = new double[bitmap.Width, bitmap.Height, 3];
            var isDet = false;
            int distance = 0;
            double max = 0;
            double min = 1000;
            if (CountFrame == 0 )
             {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
               
                      var R = bitmap.GetPixel(x, y).R;
                      var G = bitmap.GetPixel(x, y).G;
                      var B = bitmap.GetPixel(x, y).B;
                      var A = bitmap.GetPixel(x, y).A;
  
                      toHSV(new RGB(R,G,B));
                      HSV[x, y, 0] = H;
                      HSV[x, y, 1] = S;
                      HSV[x, y, 2] = V;

                        distance = (int)Math.Sqrt(
                                      (R - expR) * (R - expR) +
                                      (G - expG) * (G - expG) +
                                      (B - expB) * (B - expB));
                        if (distance > max) max = distance;
                        if (distance < min) min = distance;


                        if (distance <= 15)
                        {
                            CurrentCoord[x, y] = 1;
                        }
                        else
                        {
                            CurrentCoord[x, y] = 0;
                        }

                        if (!isDetect || ROI.Count == 0)
                        {

                            foreach (var key in coordRectanles.Keys)
                            {
                                if (x > coordRectanles[key][0] &&
                                    x < coordRectanles[key][0] + 10 &&
                                    y < coordRectanles[key][1] &&
                                    y > coordRectanles[key][1] - 10)
                                {

                                    int[] temparr = new int[3];
                                    temparr[0] = R;
                                    temparr[1] = G;
                                    temparr[2] = B;
                                    ROI.Add(temparr);
                                }

                            }
                        }
                    }
                }
          
                var max_pixels = 1;
                var max_diametr = 0;
                var max_zeros = 0;
                var startCoordY = 0;
                var startCoordX = 0;
                var endCoordY = 0;
                var endCoordX = 0;
                var maxXi = 0;
                List<int> max_Lists = new List<int>();
                List<int> max_Xi = new List<int>();
                List<int> max_Yi = new List<int>();
                List<int> startYi = new List<int>();
                List<int> startXi = new List<int>();
                int sumX = 0;
                int sumY = 0;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {

                        if (CurrentCoord[x, y] == 1)
                        {
                            max_pixels++;
                            sumX += x;
                            sumY += y;
                        }

                        if (CurrentCoord[x, y] == 1)
                        {
                            max_diametr++;
                        }
                        else
                        {
                            max_zeros++;
                        }

                        if (max_zeros >= 20)
                        {
                            if (max_diametr != 0)
                            {
                                max_Xi.Add(x);
                                max_Yi.Add(y);
                                max_Lists.Add(max_diametr);
                          
                            }
                            max_diametr = 0;
                            max_zeros = 0;
                            continue;
                            
                        }
                    }
                }
                var maxl = 0;
                var maxX = 0;
                var maxY = 0;
                for (int l = 0; l < max_Lists.Count; l++)
                {
                    if (maxl < max_Lists[l]) maxl = max_Lists[l];
                    if (maxX < max_Xi[l])
                    {
                        maxX = max_Xi[l];
                     
                    }
                    if (maxY < max_Yi[l])
                    {
                        maxY = max_Yi[l];
                    
                    }
                }


                DiamtrHands = maxl;
                HeightHands = max_pixels/ DiamtrHands*Math.PI;
                CenterX =  (int) DiamtrHands;
                CenterY = (int) HeightHands;


                
                secX = sumX / max_pixels;
                secY = sumY / max_pixels;
                deltaX = secX - currentSecX;
                deltaY = secX - currentSecX;
                Console.WriteLine("delta X: " + deltaX);
                Console.WriteLine("delta Y: " + deltaY);
                currentSecX = secX;
                currentSecY = secY;
                max_pixels = 0;
                max_diametr = 0;

                expected_valueRGB();
                if (!isDetect)
                {  
                    ROI.Clear();
                }
                CountFrame = 0;
            }
            else
            {
                CountFrame++;
            }

            return;
            
        }
       
        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap tempImg = (Bitmap)eventArgs.Frame.Clone();
            width = tempImg.Width;
            height = tempImg.Height;   
            initcoordRectanles(width, height);
             CurrentCoord = new int[tempImg.Width, tempImg.Height];
            convertB(tempImg);
            Pen Green = new Pen(Color.Green, 1);
            g = Graphics.FromImage(tempImg);
            if (isDetect)
            {
                prevPointX = currentPointX;
                prevPointY = currentPointY;
                //currentPointX = currentPointX + deltaX*0.5f;
                //currentPointY =  currentPointY + deltaY*0.5f;
                currentPointX = secX;
                currentPointY = secY;
                //if (currentPointX < 0) currentPointX = 0;
                //if (currentPointY < 0) currentPointY = 0;
                //if (panel1.Width < currentPointX) currentPointX = panel1.Width;
                //if (panel1.Height < currentPointY) currentPointY = panel1.Height;


                gPanel.DrawLine(Red, currentPointX, currentPointY, prevPointX, prevPointY);
            }

            if (!isDetect) g.DrawEllipse(Red, 80, 10, (float) DiamtrHands, (float) HeightHands);
            Rectangle[] rects = new Rectangle[coordRectanles.Count];
            foreach (var key in coordRectanles.Keys)
            {
                rects[key] = new Rectangle(coordRectanles[key][0], coordRectanles[key][1], 10, 10);
            }

           
            g.DrawRectangles(Green,  rects);
         
            pic.Image = tempImg;
                      
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            isDetect = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoCaptureDevice.IsRunning == true)
            {
                videoCaptureDevice.Stop();
            }
        }

      
    }
}
