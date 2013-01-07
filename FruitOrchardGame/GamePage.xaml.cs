using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.Windows.Threading;
using System.Windows.Media.Animation;


namespace FruitOrchardGame
{
    /// <summary>
    /// Interaction logic for GamePage.xaml
    /// </summary>
    public partial class GamePage : System.Windows.Window
    {
        #region 設定檔參數

        private float confidenceValue = 0.5f;
        private int pictureSize = 100;
        private string language = "en-US";
        private int createMaxTime = 6;
        private int createMinTime = 3;
        private float maxDownSpeed = 3;
        private float minDownSpeed = 1;
        private string SuccessSoundName = string.Empty;
        private string FailSoundName = string.Empty;

        #endregion

        #region Private field
        private int totalScore = 0;
        private int failTotal = 0;
        private FileManager filemanager;
        #endregion

        #region Window Setting (Event)
        public GamePage()
        {
            InitializeComponent();
        }

        //當畫面解析度改變，將UI位置改變
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(this.SpeechStatus, (e.NewSize.Width - this.SpeechStatus.Width) / 2);
            Canvas.SetTop(this.SpeechStatus, (e.NewSize.Height - this.SpeechStatus.Height) / 2);

            Canvas.SetLeft(this.ScoreText, 1065 * (e.NewSize.Width / 1280));
            Canvas.SetTop(this.ScoreText, 55 * (e.NewSize.Height / 720));

            //Canvas.SetLeft(this.FailText, e.NewSize.Width - 250);
            //Canvas.SetTop(this.FailText, 100);

            this.BackgroundImage.Width = e.NewSize.Width;
            this.BackgroundImage.Height = e.NewSize.Height;

            Canvas.SetLeft(this.BackgroundImage, 0);
            Canvas.SetTop(this.BackgroundImage, 0);

            Canvas.SetLeft(this.ConfidenceText, 45 * (e.NewSize.Width / 1280));
            Canvas.SetTop(this.ConfidenceText, 30 * (e.NewSize.Height / 720));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.GetConfigSetting();
            this.GetPicturePath();
            this.SettingSoundData();
            this.CreateSpeechRecongnition();
            //this.KinectSensorIni();
            if (this.speechEngine != null)
            //if (this.sensor != null && this.speechEngine != null)
            {
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);//等待4秒
                this.readyTimer.Start();
            }
        }

        //當畫面Unload，執行此函式
        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.speechEngine != null)
            {
                this.speechEngine.RecognizeAsyncCancel();
                this.speechEngine.RecognizeAsyncStop();
            }

            if (this.readyTimer != null)
            {
                this.readyTimer.Stop();
                this.readyTimer = null;
            }
        }
        #endregion        

        #region GAME
        //Game Start (only call once)
        private void Start()
        {
            this.speechEngine.SetInputToDefaultAudioDevice();
            this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        // 仿遊戲迴圈 Update ,  FPS = 60
        private void Update(object sender, EventArgs e)
        {
            this.CreatePictureTimer();
            foreach (var dir in imageDictionary)
            {
                for (int i = 0; i < imageDictionary[dir.Key].Count; i++)
                {
                    imageDictionary[dir.Key][i].ImageDown();
                    if (imageDictionary[dir.Key][i].position.Y > this.ActualHeight - this.pictureSize)
                    {
                        this.DeleteImage(imageDictionary[dir.Key][i].pictureName);
                        this.failTotal++;
                        //FailText.Text = "        Fail：" + this.failTotal.ToString();
                        this.PlaySound(this.failSoundPlayer);
                    }
                }
            }
        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            CompositionTarget.Rendering += Update;

            this.Start();           //讀取使用者語音
            //this.label1.Content = "語音識別裝置已就緒";
            this.SpeechStatus.Text = "";
            this.readyTimer.Stop();
            this.readyTimer = null;
        }
        #endregion

        #region Kinect相關設定 語音辨識

        //private KinectSensor sensor;
        private SpeechRecognitionEngine speechEngine;  //語音辨識引擎
        private DispatcherTimer readyTimer;

        //private void KinectSensorIni()
        //{
        //    this.sensor = (from sensorToCheck in KinectSensor.KinectSensors
        //                   where sensorToCheck.Status == KinectStatus.Connected
        //                   select sensorToCheck).FirstOrDefault();

        //    if (this.sensor != null)
        //    {
        //        // Turn on the color stream to receive color frames
        //        this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

        //        // Allocate space to put the pixels we'll receive
        //        this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

        //        // This is the bitmap we'll display on-screen
        //        this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

        //        // Set the image we display to point to the bitmap where we'll put the image data
        //        this.BackgroundImage.Source = this.colorBitmap;

        //        // Add an event handler to be called whenever there is new color frame data
        //        this.sensor.ColorFrameReady += this.sensor_ColorFrameReady;

        //        try
        //        {
        //            this.sensor.Start();
        //        }
        //        catch (Exception)
        //        {
        //            this.sensor = null;
        //        }
        //    }
        //}

        //private WriteableBitmap colorBitmap;
        //private byte[] colorPixels;

        //void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        //{
        //    using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
        //    {
        //        if (colorFrame != null)
        //        {
        //            // Copy the pixel data from the image to a temporary array

        //            colorFrame.CopyPixelDataTo(this.colorPixels);

        //            // Write the pixel data into our bitmap
        //            this.colorBitmap.WritePixels(
        //                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
        //                this.colorPixels,
        //                this.colorBitmap.PixelWidth * sizeof(int),
        //                0);

        //            this.BackgroundImage.Source = this.colorBitmap;
        //        }
        //    }
        //}

        private void CreateSpeechRecongnition()
        {
            //Initialize speech recognition            
            var recognizerInfo = (from a in SpeechRecognitionEngine.InstalledRecognizers()
                                  where a.Culture.Name == this.language
                                  select a).FirstOrDefault();

            if (recognizerInfo != null)
            {
                this.speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);
                Choices recognizerString = new Choices();

                foreach (var name in imageNameList)
                    recognizerString.Add(name);

                GrammarBuilder grammarBuilder = new GrammarBuilder();

                //Specify the culture to match the recognizer in case we are running in a different culture.                                 
                grammarBuilder.Culture = recognizerInfo.Culture;
                grammarBuilder.Append(recognizerString);

                // Create the actual Grammar instance, and then load it into the speech recognizer.
                var grammar = new Grammar(grammarBuilder);

                //載入辨識字串
                this.speechEngine.LoadGrammarAsync(grammar);
                this.speechEngine.SpeechRecognized += SreSpeechRecognized;
            }
        }

        void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            this.ConfidenceText.Text = "準確度：" + e.Result.Confidence.ToString("0.00");
            if (e.Result.Confidence < this.confidenceValue)//肯定度低於0.75，判為錯誤語句
            {
                //this.textBlock1.Text = e.Result.Text.ToString();
                return;
            }

            foreach (var name in imageNameList)
            {
                if (name.Equals(e.Result.Text))
                {
                    if (this.DeleteImage(name))
                    {
                        this.totalScore++;
                        ScoreText.Text = this.totalScore.ToString();
                        this.PlaySound(this.successSoundPlayer);
                    }
                }
            }
        }

        //初始化語音訊號
        //private void Start()
        //{
        //    var audioSource = sensor.AudioSource;
        //    audioSource.EchoCancellationMode = EchoCancellationMode.None; // No AEC for this sample
        //    audioSource.AutomaticGainControlEnabled = true; // Important to turn this off for speech recognition

        //    Stream stream = audioSource.Start();//開啟Kinect語音串流 

        //    this.speechEngine.SetInputToAudioStream(
        //                stream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

        //    this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        //}



        #endregion

        #region Picture Controller
        private List<PictureData> imageList = new List<PictureData>();
        private Dictionary<string, List<PictureData>> imageDictionary = new Dictionary<string, List<PictureData>>();
        private List<string> imageNameList = new List<string>();

        void CreateImage()
        {
            Random random = new Random();
            int pictureNumber = random.Next(0, this.imageNameList.Count);

            string background_path = this.PicturePathList[pictureNumber];
            Stream imageStreamSource = new FileStream(background_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            PngBitmapDecoder decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];

            Image newImage = new Image();
            newImage.Source = bitmapSource;
            newImage.MaxWidth = this.pictureSize;
            newImage.MaxHeight = this.pictureSize;

            Canvas root = this.Content as Canvas;
            Point point = new Point(random.Next(0, (int)root.ActualWidth - this.pictureSize), -this.pictureSize);
            float speed = (random.Next((int)(this.minDownSpeed * 1000), (int)(this.maxDownSpeed * 1000))) / 1000.0f;

            PictureData pictureData = new PictureData(
                newImage,
                speed,
                point,
                this.imageNameList[pictureNumber]
                );

            root.Children.Add(newImage);

            imageDictionary[this.imageNameList[pictureNumber]].Add(pictureData);
        }

        bool DeleteImage(string name)
        {
            Canvas root = this.Content as Canvas;

            List<PictureData> imageList = imageDictionary[name];

            if (imageList.Count != 0)
            {
                root.Children.Remove(imageList[0].imageControl);
                imageList.RemoveAt(0);
                return true;
            }
            return false;
        }
        #endregion

        #region Get config value (讀取設定檔資訊)
        StreamReader objReader;
        string readStr;
        /// <summary>
        /// Get setting (Read notepad [\config.txt])
        /// </summary>
        void GetConfigSetting()
        {
            SettingData settingData = new SettingData();
            this.filemanager = new FileManager();
            this.filemanager.ConfigReader(GameDefinition.SettingFilePath);
            settingData = this.filemanager.GetSettingData();

            this.confidenceValue = float.Parse(settingData.ConfidenceValue);  //get current Series name
            this.pictureSize = Int32.Parse(settingData.PictureSize);  //get current Series name
            this.language = settingData.Language;  //get SR language
            this.createMaxTime = Int32.Parse(settingData.CreateMaxTime);
            this.createMinTime = Int32.Parse(settingData.CreateMinTime);
            this.maxDownSpeed = Int32.Parse(settingData.MaxDownSpeed);
            this.minDownSpeed = Int32.Parse(settingData.MinDownSpeed);
            this.SuccessSoundName = settingData.SuccessSound;
            this.FailSoundName = settingData.FailSound;


            //    string sFolderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), configPath);
            //    objReader = new StreamReader(sFolderPath);
            //    string[] tempStr;

            //    // read confidence value
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "confidenceValue")
            //    {
            //        this.confidenceValue = float.Parse(tempStr[1]);  //get current Series name
            //    }

            //    // read picture size
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "pictureSize")
            //    {
            //        this.pictureSize = Int32.Parse(tempStr[1]);  //get current Series name
            //    }

            //    // read SR language
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "language")
            //    {
            //        this.language = tempStr[1];  //get SR language
            //    }

            //    // read createMaxTime
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "createMaxTime")
            //    {
            //        this.createMaxTime = Int32.Parse(tempStr[1]);
            //    }

            //    // read createMinTime
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "createMinTime")
            //    {
            //        this.createMinTime = Int32.Parse(tempStr[1]);
            //    }

            //    // read maxDownSpeed
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "maxDownSpeed")
            //    {
            //        this.maxDownSpeed = Int32.Parse(tempStr[1]);
            //    }

            //    // read minDownSpeed
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "minDownSpeed")
            //    {
            //        this.minDownSpeed = Int32.Parse(tempStr[1]);
            //    }

            //    // read SuccessSound name
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "SuccessSound")
            //    {
            //        this.SuccessSoundName = tempStr[1];
            //    }

            //    // read FailSound name
            //    ReadLine(ref readStr);
            //    tempStr = readStr.Split(new string[] { "=", " " }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tempStr[0] == "FailSound")
            //    {
            //        this.FailSoundName = tempStr[1];
            //    }

            //    objReader.Close();          // have to close reader (don't forget)
            //}

            ///// <summary>
            ///// 讀取一行文字，假如讀到空白，繼續往下一行讀取
            ///// </summary>
            ///// <param name="readString">存放的字串</param>
            //void ReadLine(ref string readString)
            //{
            //    do
            //    {
            //        readString = objReader.ReadLine();
            //    } while (readString == "");
        }

        #endregion

        #region 設定音效檔

        private MediaPlayer successSoundPlayer = new MediaPlayer();
        private MediaPlayer failSoundPlayer = new MediaPlayer();

        private void SettingSoundData()
        {
            // Get Sound resource path   folder : (\bin\debug\Sound)
            string sFolderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Sound");

            if (Directory.Exists(sFolderPath))          // 確認是否有此資料夾
            {
                string soundPath = string.Empty;

                soundPath = System.IO.Path.Combine(sFolderPath, this.SuccessSoundName);
                if (File.Exists(soundPath))
                {
                    this.successSoundPlayer.Open(new Uri(soundPath));
                }
                soundPath = System.IO.Path.Combine(sFolderPath, this.FailSoundName);
                if (File.Exists(soundPath))
                {
                    this.failSoundPlayer.Open(new Uri(soundPath));
                }
            }
        }

        private void PlaySound(MediaPlayer player)
        {
            player.Position = TimeSpan.Zero;
            player.Play();
        }

        #endregion

        #region Get Picture Path (得到圖片路徑)

        List<string> PicturePathList = new List<string>();    // save Picture's path 

        /// <summary>
        /// Get Picture resource folder: (\bin\debug\Picture)
        /// </summary>
        void GetPicturePath()
        {
            // Get media resource path   folder : (\bin\debug\Picture)
            string sFolderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Picture");

            string[] tempFile;
            FileInfo info;

            if (Directory.Exists(sFolderPath))          // 確認是否有此資料夾
            {
                tempFile = Directory.GetFiles(sFolderPath);//取得資料夾下所有檔案

                foreach (string item in tempFile)       //讀取資料夾下的每個檔案(排除desktop.ini)
                {
                    info = new FileInfo(item);

                    if (Equals(info.Name, "desktop.ini")) //不加入此文件
                        continue;

                    string pictureName = info.Name.Split('.')[0];
                    this.imageDictionary.Add(pictureName, new List<PictureData>());
                    this.imageNameList.Add(pictureName);

                    this.PicturePathList.Add(info.ToString());                 //Add to file path string in PicturePathList                    
                }
            }
        }
        #endregion

        #region Create Picture Timer (一定時間生成新圖片)

        private float totalTime = 0;
        private float createTime = 0;
        private DateTime currentDateTime = new DateTime();
        private DateTime oldDateTime = DateTime.Now;
        private TimeSpan deltaTime = new TimeSpan();

        private void CreatePictureTimer()
        {
            if (totalTime > createTime)
            {
                Random random = new Random();
                this.createTime = (random.Next(this.createMinTime * 1000, this.createMaxTime * 1000)) / 1000.0f;
                this.totalTime = 0;
                this.CreateImage();
            }
            else
            {
                this.currentDateTime = DateTime.Now;
                this.deltaTime = this.currentDateTime - this.oldDateTime;
                this.oldDateTime = this.currentDateTime;
                this.totalTime += (float)this.deltaTime.TotalSeconds;
            }
        }

        #endregion
    }

    #region PictureData Class
    public class PictureData
    {
        public Image imageControl { get; private set; }
        public float downSpeed { get; private set; }
        public string pictureName { get; private set; }
        public Point position;

        public PictureData(Image image, float speed, Point pos, string name)
        {
            this.imageControl = image;
            this.downSpeed = speed;
            this.position = pos;
            Canvas.SetLeft(this.imageControl, this.position.X);
            Canvas.SetTop(this.imageControl, this.position.Y);
            this.pictureName = name;
        }

        public void ImageDown()
        {
            Canvas.SetLeft(this.imageControl, this.position.X);
            Canvas.SetTop(this.imageControl, this.position.Y + this.downSpeed);
            this.position.X = Canvas.GetLeft(this.imageControl);
            this.position.Y = Canvas.GetTop(this.imageControl);
        }
    }
    #endregion
    
}