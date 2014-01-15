using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HCI.Kinect;
using Aldebaran.Proxies;
using Microsoft.Speech;
using Microsoft.Speech.Recognition;
using System.Threading;
using System.Xml;

namespace HCI.GAS.Kinect
{
    public class KinectAudio : HCI.Kinect.KinectAudio, HCI.Kinect.IKinectAudio
    {
        #region variables

        private string ip;
        private int port;
        private LedsProxy led;
        //True if the kinect speech would continue to listen for input.
        public bool auto;

        #endregion

        public KinectAudio(String XML,string ip, int port):base(XML)
        {
            auto = true;
            this.ip = ip;
            this.port = port;
            led = new LedsProxy(ip, port);
            readyXML(XML);
            SpeechHypoThread = new List<Thread>();
        }

        private List<string> wordlist;
        private List<string> keylist;
        public void readyXML(String XML)
        {
            wordlist = new List<string>();
            keylist = new List<string>();
            using (XmlReader reader = XmlReader.Create(XML))
            {
                reader.ReadStartElement("grammar");
                reader.ReadStartElement("rule");
                reader.ReadStartElement("one-of");
                for (var i = 0; i < 5; i++)
                {
                    reader.ReadStartElement("item");
                    wordlist.Add(reader.ReadContentAsString());
                    reader.ReadStartElement("tag");
                    keylist.Add(reader.ReadContentAsString());
                    reader.ReadEndElement();
                    reader.ReadEndElement();
                }
                reader.Close();
            }

            for (int i = 0; i < wordlist.Count; i++ )
            {
                var word = wordlist[i];
                word.Trim();
                var lists = word.Split(' ');
                string newword = "";
                for (int j = 0; j < lists.Length-2; j++)
                {
                    newword += lists[j] += " ";
                }
                newword = newword.Substring(1);
                newword = newword.Substring(0,newword.Length-1);
                wordlist[i] = newword;
            }

        }

        //protected override void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        //{
        //    //Defensive code in case when threads stack up
        //    if (speechEngine == null)
        //        return;

        //    EndKinectSpeech();
        //    // Speech utterance confidence below which we treat speech as if it hadn't been heard
        //    const double ConfidenceThreshold = 0.4;

        //    if (e.Result.Confidence >= ConfidenceThreshold)
        //    {
        //        Console.Out.Write("Speech event fired");
        //        var word = e.Result.Semantics.Value.ToString();
        //        if (_Subscribers.ContainsKey(word))
        //        {
        //            var handlers = _Subscribers[word];
        //            foreach (Action<string> handler in handlers)
        //            {
        //                handler.Invoke(word);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        //nothing I guess
        //    }
        //    if (auto)
        //        StartKinectSpeech();
        //

        private List<Thread> SpeechHypoThread;
        protected void SpeechHypothesisThread(object sender, SpeechHypothesizedEventArgs e)
        {
            Thread thread = new Thread(() => SpeechHypo(sender, e));
            thread.Start();
            SpeechHypoThread.Add(thread);
            updateThread();
        }

        protected void updateHypoThread()
        {
            //To remove previous finished tread, prevent the threads from stacking up
            for (int i = SpeechHypoThread.Count - 1; i >= 0; i--)
            {
                if (!SpeechHypoThread[i].IsAlive)
                {
                    SpeechHypoThread.Remove(SpeechHypoThread[i]);
                }
            }
        }


        //Debug only... to see speech hypothsis
        public void SpeechHypo(object sender,SpeechHypothesizedEventArgs e)
        {
            Console.WriteLine(e.Result.Text);
            if(wordlist.Contains(e.Result.Text))
            {
                int index = wordlist.IndexOf(e.Result.Text);
                index++;
                string word = "Question" + index;
                if (_Subscribers.ContainsKey(word))
                {
                    var handlers = _Subscribers[word];
                    foreach (Action<string> handler in handlers)
                    {
                        try
                        {
                            handler.Invoke(word);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An exception has happen when involving an action");
                            Console.WriteLine("We are not going to take care of them in this study");
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }


        public void StartSpeechDetected()
        {
            try
            {
                speechEngine.RecognizeAsync(Microsoft.Speech.Recognition.RecognizeMode.Multiple);
            }
            catch (Exception)
            {
                //nothing
            }
            speechEngine.SpeechDetected += SpeechDetected;
        }

        public void EndSpeechDetected()
        {
            speechEngine.RecognizeAsyncCancel();
        }

        protected override void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Defensive code in case when threads stack up
            if (speechEngine == null)
                return;

            //EndKinectSpeech();
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.4;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.Out.WriteLine("Speech event fired");
                var word = e.Result.Semantics.Value.ToString();
                if (_Subscribers.ContainsKey(word))
                {
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
            //if (auto)
               //StartKinectSpeech();
        }

        public override void StartKinectSpeech()
        {
            led.off("ChestLeds");
            led.on("ChestLedsBlue");
            speechEngine.RecognizeAsync(Microsoft.Speech.Recognition.RecognizeMode.Multiple);
            speechEngine.SpeechRecognized += SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected += SpeechRejected;
            speechEngine.SpeechHypothesized += SpeechHypothesisThread;
        }

        public override void EndKinectSpeech()
        {
            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            speechEngine.SpeechHypothesized -= SpeechHypothesisThread;
            speechEngine.RecognizeAsyncCancel();
            led.off("ChestLeds");
        }

        public void StartKinectSpeechHidden()
        {
            speechEngine.RecognizeAsync(Microsoft.Speech.Recognition.RecognizeMode.Multiple);
            speechEngine.SpeechRecognized += SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected += SpeechRejected;
            speechEngine.SpeechHypothesized += SpeechHypothesisThread;
        }

        public void EndKinectSpeechHidden()
        {
            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            speechEngine.SpeechHypothesized -= SpeechHypothesisThread;
            speechEngine.RecognizeAsyncCancel();
        }

        
    }
}
