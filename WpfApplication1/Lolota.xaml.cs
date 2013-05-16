using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.IO;
using NAudio.Wave;
using System.Media;
using System.Threading;
using System.ComponentModel;

namespace WpfApplication1
{

    public partial class Lolota : Window
    {
        KinectSensor kinect;
        ImageSource normal;
        ImageSource listening_left;
        ImageSource listening_right;
        ImageSource listening_center;
        BackgroundWorker rec_and_play;
        public Lolota(KinectSensor sensor) : this()
        {

            #region 預先載入圖片
            normal = new BitmapImage(new Uri("normal.jpg",UriKind.Relative));
            listening_left = new BitmapImage(new Uri("listening_left.jpg", UriKind.Relative));
            listening_right = new BitmapImage(new Uri("listening_right.jpg", UriKind.Relative));
            listening_center = new BitmapImage(new Uri("listening_center.jpg", UriKind.Relative));
            #endregion

            rec_and_play = new BackgroundWorker();
            rec_and_play.DoWork += _backgroundWorker_DoWork;
            rec_and_play.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;

            kinect = sensor;
            kinect.Start();
            KinectAudioSource audioSource = AudioSourceSetup(kinect.AudioSource);
            audioSource.Start();
            audioSource.SoundSourceAngleChanged += audioSource_SoundSourceAngleChanged;
        }

        bool inprocess = false;
        void audioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            if (e.ConfidenceLevel < 0.95)
                return;
            if (inprocess)
               return;
            inprocess = true;

            if(e.Angle == 0)
                lolota.Source = listening_center;
            else if(e.Angle > 0)
                lolota.Source = listening_left;
            else if (e.Angle < 0)
                lolota.Source = listening_right;

            Title = e.Angle.ToString() + ":" + e.ConfidenceLevel;

            rec_and_play.RunWorkerAsync();
        }

        void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Console.WriteLine("DoWork");
            StartRecord(kinect.AudioSource);
            StartPlayback();    
        }

        void _backgroundWorker_RunWorkerCompleted(
            object sender,
            RunWorkerCompletedEventArgs e)
        {
            inprocess = false;
            lolota.Source = normal;
            Console.WriteLine("RunWorkerCompleted");
        }

        void StartRecord(KinectAudioSource audiosource)
        {
            int bufferSize = 50000;
            byte[] soundSampleBuffer = new byte[bufferSize];

            Stream kinectAudioStream = audiosource.Start();
            kinectAudioStream.Read(soundSampleBuffer, 0, soundSampleBuffer.Length);

            SaveToWaveFile(soundSampleBuffer);

        }

        string filename = "record.wav";
        void SaveToWaveFile(byte[] sounddata)
        {
            var newFormat = new WaveFormat(16000, 16, 2);
            using (WaveFileWriter wfw = new WaveFileWriter(filename, newFormat))
            {
                wfw.Write(sounddata, 0, sounddata.Length);
            }
        }

        void StartPlayback()
        {
            FileStream fs =  new FileStream(filename, FileMode.Open, FileAccess.Read);
            SoundPlayer sp = new SoundPlayer(fs);
            sp.Play();

            fs.Close();
        }

        public Lolota()
        {
            InitializeComponent();
        }
        private KinectAudioSource AudioSourceSetup(KinectAudioSource audioSource)
        {
            audioSource.NoiseSuppression = true;
            audioSource.AutomaticGainControlEnabled = true;
            //audioSource.EchoCancellationMode = EchoCancellationMode.CancellationOnly;
            return audioSource;
        }
    }
}
