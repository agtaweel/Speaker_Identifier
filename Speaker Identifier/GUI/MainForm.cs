using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;


namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;


        private string path;

        private Encoder encoder;
        private Decoder decoder;

        private bool isRecorded;

        private Sequence seq;

     

        private DTWnew dtw;


        private string record;

        public MainForm()
        {
            InitializeComponent();

            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
        }


        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
        }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                button1.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = true;
                button1.Enabled = true;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = false;
                button1.Enabled = false;
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
            btnAdd.Enabled = true;
            btnIdentify.Enabled = true;
            button1.Enabled = true;
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                
                isRecorded = false;
                path = open.FileName;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);
                seq = AudioOperations.ExtractFeatures(signal);
                updateButtons();
                btnIdentify.Enabled = true;
                button1.Enabled = true;

            }
            AudioSignal sig = new AudioSignal();
            sig = AudioOperations.OpenAudioFile(path);
            seq = AudioOperations.ExtractFeatures(sig);
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (TestCase1.Checked)
            {
                string name = "";
                List<User> t = TestcaseLoader.LoadTestcase1Training(@"C:\test\Complete SpeakerID Dataset\TrainingList.txt");
                FileStream F = new FileStream("Projectsequences1.txt", FileMode.Append);
                StreamWriter sa = new StreamWriter(F);
                for (int i = 0; i < t.Count; i++)
                {
                    name = t[i].UserName;
                    for (int j = 0; j < t[i].UserTemplates.Count; j++)
                    {
                        AudioSignal sig = new AudioSignal();
                        seq = AudioOperations.ExtractFeatures(t[i].UserTemplates[j]);
                        record = "";
                        for (int k = 0; k < seq.Frames.Length; k++)
                        {
                            for (int l = 0; l < 13; l++)
                            {
                                record += seq.Frames[k].Features[l];
                                if (l != 12)
                                {
                                    record += "@";
                                }
                            }
                            if (k != seq.Frames.Length - 1)
                            {
                                record += "#";
                            }
                        }
                        sa.WriteLine(name);
                        sa.WriteLine(record);
                    }
                }
                sa.Close();
            }

            if (TestCase2.Checked)
            {
                string name = "";
                List<User> t = TestcaseLoader.LoadTestcase2Training(@"C:\test\Complete SpeakerID Dataset\TrainingList.txt");
                FileStream F = new FileStream("Projectsequences2.txt", FileMode.Append);
                StreamWriter sa = new StreamWriter(F);
                for (int i = 0; i < t.Count; i++)
                {
                    name = t[i].UserName;

                    for (int j = 0; j < t[i].UserTemplates.Count; j++)
                    {

                        AudioSignal sig = new AudioSignal();

                        seq = AudioOperations.ExtractFeatures(t[i].UserTemplates[j]);
                        record = "";
                        for (int k = 0; k < seq.Frames.Length; k++)
                        {
                            for (int l = 0; l < 13; l++)
                            {
                                record += seq.Frames[k].Features[l];
                                if (l != 12)
                                {
                                    record += "@";
                                }
                            }
                            if (k != seq.Frames.Length - 1)
                            {
                                record += "#";
                            }
                        }
                        sa.WriteLine(name);
                        sa.WriteLine(record);

                    }
                }
                sa.Close();

            }

            if (TestCase3.Checked)
            {
                string name = "";
                List<User> t = TestcaseLoader.LoadTestcase3Training(@"C:\test\Complete SpeakerID Dataset\TrainingList.txt");
                FileStream F = new FileStream("Projectsequences3.txt", FileMode.Append);
                StreamWriter sa = new StreamWriter(F);
                for (int i = 0; i < t.Count; i++)
                {
                    name = t[i].UserName;

                    for (int j = 0; j < t[i].UserTemplates.Count; j++)
                    {

                        AudioSignal sig = new AudioSignal();

                        seq = AudioOperations.ExtractFeatures(t[i].UserTemplates[j]);
                        record = "";
                        for (int k = 0; k < seq.Frames.Length; k++)
                        {
                            for (int l = 0; l < 13; l++)
                            {
                                record += seq.Frames[k].Features[l];
                                if (l != 12)
                                {
                                    record += "@";
                                }
                            }
                            if (k != seq.Frames.Length - 1)
                            {
                                record += "#";
                            }
                        }
                        sa.WriteLine(name);
                        sa.WriteLine(record);

                    }
                }
                sa.Close();

            }
            if (Normal.Checked)
            {
             
                FileStream F = new FileStream("Projectsequences.txt", FileMode.Append);
                StreamWriter sa = new StreamWriter(F);
                Console.WriteLine("enter your name : ");
                string name = Console.ReadLine();
                        record = "";
                        for (int k = 0; k < seq.Frames.Length; k++)
                        {
                            for (int l = 0; l < 13; l++)
                            {
                                record += seq.Frames[k].Features[l];
                                if (l != 12)
                                {
                                    record += "@";
                                }
                            }
                            if (k != seq.Frames.Length - 1)
                            {
                                record += "#";
                            }
                        }
                        sa.WriteLine(name);
                        sa.WriteLine(record);
                
                sa.Close();
            }
        }





        private void btnIdentify_Click(object sender, EventArgs e)
        {
            if (TestCase1.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase1Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
               
                    FileStream fs = new FileStream("Projectsequences1.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);

                    
                   
                    seq = AudioOperations.ExtractFeatures(t[0].UserTemplates[0]);
                    double min = double.MaxValue;
                    string ans = "";
                    dtw = new DTWnew();
                    while (sr.Peek() != -1)
                    {
                        double res = 0;
                        string sequence = "";
                        string _name = sr.ReadLine();
                        sequence = sr.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');
                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res = dtw.DTWfun(seq, seq2);
                        time.Stop();
                        if (res < min)
                        {
                            min = res;
                            ans = _name;
                        }
                       
                    } 
                Console.WriteLine("OLD DTW");
                        Console.WriteLine(ans + " " + min);
                        
                        Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                  //  Console.WriteLine(ans);
                   // Console.WriteLine(min);
                    //MessageBox.Show(ans + " " + min);
                    sr.Close();
                
            }

            if (TestCase2.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase2Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
                
                    FileStream fs = new FileStream("Projectsequences2.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
             
                    seq = AudioOperations.ExtractFeatures(t[0].UserTemplates[0]);
                    double min = double.MaxValue;
                    string ans = "";
                    dtw = new DTWnew();
                    while (sr.Peek() != -1)
                    {

                        double res = 0;
                        string sequence = "";
                        string _name = sr.ReadLine();
                        sequence = sr.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res = dtw.DTWfun(seq, seq2);
                        time.Stop();
                        if (res < min)
                        {
                            min = res;
                            ans = _name;
                        }


                    }
                   /* Console.WriteLine(ans);
                    Console.WriteLine(min);
                    MessageBox.Show(ans + " " + min*/
                    Console.WriteLine("OLD DTW");
                    Console.WriteLine(ans + " " + min);

                    Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    sr.Close();
                
            }


            if (TestCase3.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase3Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
                for (int k = 0; k < t.Count; k++)
                {
                    FileStream fs = new FileStream("Projectsequences3.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    Random rd = new Random();
                    int counter = rd.Next(0, t[k].UserTemplates.Count);
                    seq = AudioOperations.ExtractFeatures(t[k].UserTemplates[counter]);
                    double min = double.MaxValue;
                    string ans = "";
                    dtw = new DTWnew();
                    while (sr.Peek() != -1)
                    {

                        double res = 0;
                        string sequence = "";
                        string _name = sr.ReadLine();
                        sequence = sr.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res = dtw.DTWfun(seq, seq2);
                        if (res < min)
                        {
                            min = res;
                            ans = _name;
                        }

                        Console.WriteLine("OLD DTW");
                        Console.WriteLine(ans + " " + min);
                        time.Stop();
                        Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    }
                    Console.WriteLine(ans);
                    Console.WriteLine(min);
                    MessageBox.Show(ans + " " + min);
                    sr.Close();
                }
            }


            if (Normal.Checked)
            {
                FileStream fs = new FileStream("Projectsequences.txt", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                Stopwatch time = new Stopwatch();
                    double min = double.MaxValue;
                    string ans = "";
                    dtw = new DTWnew();
                    while (sr.Peek() != -1)
                    {

                        double res = 0;
                        string sequence = "";
                        string _name = sr.ReadLine();
                        sequence = sr.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res = dtw.DTWfun(seq, seq2);
                        if (res < min)
                        {
                            min = res;
                            ans = _name;
                        }

                        Console.WriteLine("OLD DTW");
                        Console.WriteLine(ans + " " + min);
                        time.Stop();
                        Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    }
                    Console.WriteLine(ans);
                    Console.WriteLine(min);
                    sr.Close();

            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (TestCase1.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase1Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
                
                    FileStream f = new FileStream("Projectsequences1.txt", FileMode.Open);
                    StreamReader s = new StreamReader(f);
                    //Random rd = new Random();
                    
                    seq = AudioOperations.ExtractFeatures(t[0].UserTemplates[0]);
                    double min2 = double.MaxValue;
                    string ans2 = "";
                    //Console.Write("Enter The Window Index :");
                    // int w = Console.Read();
                    while (s.Peek() != -1)
                    {
                        double res2 = 0;
                        string sequence = "";
                        string _name = s.ReadLine();
                        sequence = s.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res2 = PRUNINGDTW.DTW2fun(seq, seq2);
                        time.Stop();
                        if (res2 < min2)
                        {
                            min2 = res2;
                            ans2 = _name;
                        }
                       
                    }
                    Console.WriteLine("NEW DTW");
                    Console.WriteLine(ans2 + " " + min2);

                    Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    s.Close();
                
            }

            if (TestCase2.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase2Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
                
                    FileStream f = new FileStream("Projectsequences2.txt", FileMode.Open);
                    StreamReader s = new StreamReader(f);

                    seq = AudioOperations.ExtractFeatures(t[0].UserTemplates[0]);
                    double min2 = double.MaxValue;
                    string ans2 = "";
                    //Console.Write("Enter The Window Index :");
                    // int w = Console.Read();
                    while (s.Peek() != -1)
                    {
                        double res2 = 0;
                        string sequence = "";
                        string _name = s.ReadLine();
                        sequence = s.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res2 = PRUNINGDTW.DTW2fun(seq, seq2);
                        time.Stop();
                        if (res2 < min2)
                        {
                            min2 = res2;
                            ans2 = _name;
                        }
    
                    }
                    Console.WriteLine("NEW DTW");
                    Console.WriteLine(ans2 + " " + min2);

                    Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    s.Close();
                
            }

            if (TestCase3.Checked)
            {
                List<User> t = TestcaseLoader.LoadTestcase3Testing(@"C:\test\Complete SpeakerID Dataset\TestingList.txt");
                Stopwatch time = new Stopwatch();
                for (int k = 0; k < t.Count; k++)
                {
                    FileStream f = new FileStream("Projectsequences3.txt", FileMode.Open);
                    StreamReader s = new StreamReader(f);
                    Random rd = new Random();
                    int counter = rd.Next(0, t[k].UserTemplates.Count);
                    seq = AudioOperations.ExtractFeatures(t[k].UserTemplates[counter]);
                    double min2 = double.MaxValue;
                    string ans2 = "";
                    //Console.Write("Enter The Window Index :");
                    // int w = Console.Read();
                    while (s.Peek() != -1)
                    {
                        double res2 = 0;
                        string sequence = "";
                        string _name = s.ReadLine();
                        sequence = s.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res2 = PRUNINGDTW.DTW2fun(seq, seq2);
                        if (res2 < min2)
                        {
                            min2 = res2;
                            ans2 = _name;
                        }
                        Console.WriteLine("NEW DTW");
                        Console.WriteLine(ans2 + " " + min2);
                        time.Stop();
                        Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    }
                    Console.WriteLine(ans2);
                    Console.WriteLine(min2);
                    MessageBox.Show(ans2 + " " + min2);
                    s.Close();
                }
            }

            if (Normal.Checked)
            {
                FileStream f = new FileStream("Projectsequences.txt", FileMode.Open);
                StreamReader s = new StreamReader(f);
                Stopwatch time = new Stopwatch();
                    double min2 = double.MaxValue;
                    string ans2 = "";
                    //Console.Write("Enter The Window Index :");
                    // int w = Console.Read();
                    while (s.Peek() != -1)
                    {
                        double res2 = 0;
                        string sequence = "";
                        string _name = s.ReadLine();
                        sequence = s.ReadLine();
                        string[] arr = sequence.Split('#');
                        string[] arr3 = new string[arr.Length];
                        Sequence seq2 = new Sequence();
                        seq2.Frames = new MFCCFrame[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            seq2.Frames[i] = new MFCCFrame();
                            for (int o = 0; o < arr.Length; o++)
                            {
                                arr3 = arr[o].Split('@');

                            }
                            for (int j = 0; j < 13; j++)
                            {
                                seq2.Frames[i].Features[j] = double.Parse(arr3[j]);
                            }
                        }
                        time.Start();
                        res2 = PRUNINGDTW.DTW2fun(seq, seq2);
                        if (res2 < min2)
                        {
                            min2 = res2;
                            ans2 = _name;
                        }
                        Console.WriteLine("NEW DTW");
                        Console.WriteLine(ans2 + " " + min2);
                        time.Stop();
                        Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", time.Elapsed);
                    }
                    Console.WriteLine(ans2);
                    Console.WriteLine(min2);
                s.Close();
            }
        }


        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
          
    
