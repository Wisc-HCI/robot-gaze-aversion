using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HCI.Kinect
{
    public class KinectAudio:IKinectAudio
    {

        public KinectAudio(Choices dic):this()
        {
            dictionary = dic;
        }

        public KinectAudio()
        {
            PrepKinect();
            kinnectStatus = true;
            speechRecgonizedThread = new List<Thread>();
        }

        public KinectAudio(String XML):this()
        {
            this.pathToXML = XML;
        }

        #region Variables

        protected KinectSensor sensor;
        protected Stream audioStream;
        public bool kinnectStatus;
        protected Choices dictionary;
        protected string pathToXML;
        public SpeechRecognitionEngine speechEngine;
        protected Thread audioThread;
        protected bool audioDetect;
        protected bool soundDetect;
        protected Dictionary<string, List<Object>> _Subscribers = new Dictionary<string, List<Object>>();

        #endregion

        #region KinectCodes


        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        protected static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        protected void PrepKinect()
        {
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                try
                {
                    // Start the sensor!
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    throw new IOException();
                }
            }

            if (null == this.sensor)
            {
                throw new IOException();
            }

            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                return;
            }
            else
            {
                throw new IOException();
            }
        }

        /// <summary>
        /// Initialize Kinect with words.
        /// </summary>
        protected void KinectStart()
        {
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                //Populate the speech engine with keywords we are interested in.
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var gb = new GrammarBuilder { Culture = ri.Culture };

                //Make the path point to current directory
                string path = Directory.GetCurrentDirectory();
                path += "\\";
                path += pathToXML;


                if (pathToXML != null)
                {
                    gb.AppendRuleReference(path);
                }
                else if (dictionary != null)
                    gb.Append(dictionary);
                else
                    throw new NullReferenceException();

                var g = new Grammar(gb);

                speechEngine.LoadGrammar(g);

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);


                this.audioStream = sensor.AudioSource.Start();

                speechEngine.SetInputToAudioStream(
                   this.audioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                //speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                kinnectStatus = true;
            }
            else
            {
                //Speech Recognization not found
            }
        }

        /// <summary>
        /// Individual Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        protected virtual void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Defensive code in case when threads stack up
            if (speechEngine == null)
                return;

            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.4;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.Out.WriteLine("Speech event fired");
                var word = e.Result.Semantics.Value.ToString();
                if (_Subscribers.ContainsKey(word))
                {
                    //move the subscribers to a local value to prevent threading conflicts.
                    var handlers = _Subscribers[word];
                    foreach (Action<string> handler in handlers)
                    {
                        handler.Invoke(word);
                    }
                }
            }
            else
            {
                //nothing I guess
            }
            if (speechEngine != null)
            {
                speechEngine.SpeechRecognized += SpeechRecognizedThread;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;
            }
        }

        List<Thread> speechRecgonizedThread;
        protected virtual void SpeechRecognizedThread(object sender, SpeechRecognizedEventArgs e)
        {
            Thread thread = new Thread(() => SpeechRecognized(sender, e));
            thread.Start();
            speechRecgonizedThread.Add(thread);
            updateThread();
        }

        protected void updateThread()
        {
            //To remove previous finished tread, prevent the threads from stacking up
            for (int i = speechRecgonizedThread.Count - 1; i >= 0; i--)
            {
                if (!speechRecgonizedThread[i].IsAlive)
                {
                    speechRecgonizedThread.Remove(speechRecgonizedThread[i]);
                }
            }
        }

        protected virtual void SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Console.WriteLine("Speech Detected");
            speechEngine.SpeechDetected -= SpeechDetected;
            if (_Subscribers.ContainsKey("Interrupt"))
            {
                var handlers = _Subscribers["Interrupt"];
                foreach (Action<string> handler in handlers)
                {
                    handler.Invoke("Interrupt");
                }
            }
        }


        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        protected void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Rejected");
        }

        protected void shutdown()
        {
            if (null != this.sensor)
            {
                this.sensor.AudioSource.Stop();

                this.sensor.Stop();
                this.sensor = null;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
                this.speechEngine = null;
            }
        }
        #endregion

        #region Implementations
        public void StartKinect()
        {
            KinectStart();
            kinnectStatus = true;
        }

        public virtual void EndKinectSpeech()
        {
            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            speechEngine.RecognizeAsyncCancel();
        }

        public virtual void StartKinectSpeech()
        {
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            speechEngine.SpeechRecognized += SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected += SpeechRejected;
        }

        public void EndKinect()
        {
            shutdown();
            _Subscribers = new Dictionary<string,List<object>>();
            kinnectStatus = false;
        }

        public void SetGrammer(Choices dictionary)
        {
            shutdown();
            PrepKinect();
            this.dictionary = dictionary;
            this.KinectStart();
        }

        public void SetGrammer(String pathtoXMLFile)
        {
            shutdown();
            PrepKinect();
            this.pathToXML = pathtoXMLFile;
            this.KinectStart();

        }

        public void clearSubscribers()
        {
            _Subscribers = new Dictionary<string, List<object>>();
        }

        public void Subscribe(string words, Action<string> handler)
        {
            if (_Subscribers.ContainsKey(words))
            {
                var handlers = _Subscribers[words];
                if (!handlers.Contains(handler))
                    handlers.Add(handler);
                else
                {
                    Console.WriteLine("Already have this function");
                }
            }
            else
            {
                var handlers = new List<Object>();
                handlers.Add(handler);
                _Subscribers[words] = handlers;
            }
        }

        public void Unsubscribe(string words, Action<string> handler)
        {
            if (_Subscribers.ContainsKey(words))
            {
                _Subscribers.Remove(words);
            }
        }

        public void RestartKinect()
        {
            PrepKinect();
        }

        public KinectSensor getSensor()
        {
            return this.sensor;
        }


        #endregion

        #region Audio Implementation

        public void PrepKinectSound()
        {
            audioDetect = false;
            soundDetect = true;
            audioThread = new Thread(KinectSoundInitialize);
            audioThread.Start();
        }

        protected void KinectSoundInitialize()
        {
         /// <summary>
            /// Number of milliseconds between each read of audio data from the stream.
            /// </summary>
            const int AudioPollingInterval = 50;

            /// <summary>
            /// Number of samples captured from Kinect audio stream each millisecond.
            /// </summary>
            const int SamplesPerMillisecond = 16;

            /// <summary>
            /// Number of bytes in each Kinect audio stream sample.
            /// </summary>
            const int BytesPerSample = 2;

            /// <summary>
            /// Number of audio samples represented by each column of pixels in wave bitmap.
            /// </summary>
            const int SamplesPerColumn = 40;

            /// <summary>
            /// Buffer used to hold audio data read from audio stream.
            /// </summary>
            byte[] audioBuffer = new byte[AudioPollingInterval * SamplesPerMillisecond * BytesPerSample];

            // Bottom portion of computed energy signal that will be discarded as noise.
            // Only portion of signal above noise floor will be displayed.
            const double EnergyNoiseFloor = 0.2;

            /// <summary>
            /// Sum of squares of audio samples being accumulated to compute the next energy value.
            /// </summary>
            double accumulatedSquareSum = 0;

            /// <summary>
            /// Number of audio samples accumulated so far to compute the next energy value.
            /// </summary>
            int accumulatedSampleCount = 0;

            double refValue = 0.1;

            bool startAudioSpike = false;
            double threshold = 0.35;

            while (soundDetect)
            {
                int readCount = audioStream.Read(audioBuffer, 0, audioBuffer.Length);

                // Calculate energy corresponding to captured audio in the dispatcher
                // (UI Thread) context, so that rendering code doesn't need to
                // perform additional synchronization.
               /* Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {*/
                        for (int i = 0; i < readCount; i += 2)
                        {
                            // compute the sum of squares of audio samples that will get accumulated
                            // into a single energy value.
                            short audioSample = BitConverter.ToInt16(audioBuffer, 0);
                            accumulatedSquareSum += audioSample * audioSample;
                            ++accumulatedSampleCount;

                            if (accumulatedSampleCount < SamplesPerColumn)
                            {
                                continue;
                            }

                            // Each energy value will represent the logarithm of the mean of the
                            // sum of squares of a group of audio samples.
                            double meanSquare = accumulatedSquareSum / SamplesPerColumn;
                            double rms = Math.Sqrt(meanSquare);
                            double decibels = 20.0 * Math.Log10(rms / refValue);
                            double amplitude = Math.Log(meanSquare) / Math.Log(int.MaxValue);

                            // Renormalize signal above noise floor to [0,1] range.
                            double energy = Math.Max(0, amplitude - EnergyNoiseFloor) / (1 - EnergyNoiseFloor);
                            if (energy > threshold && !startAudioSpike)
                            {
                                if (_Subscribers.ContainsKey("Interrupt") && audioDetect)
                                {
                                    var handlers = _Subscribers["Interrupt"];
                                    foreach (Action<string> handler in handlers)
                                    {
                                        handler.Invoke("Interrupt");
                                    }
                                }
                                startAudioSpike = true;
                                
                            }
                            else if (energy <= threshold)
                            {
                                startAudioSpike = false;
                            }
                            //this.energyIndex = (this.energyIndex + 1) % this.energy.Length;
                            accumulatedSquareSum = 0;
                            accumulatedSampleCount = 0;
                         }
                   // }));
            }
        }

        public void StartKinectSound()
        {
            audioDetect = true;
        }

        public void EndKinectSound()
        {
            soundDetect = false;
            audioThread.Join();
        }

        #endregion
    }


}
