using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Logs
{
    public class Log
    {
        protected StreamWriter txtStream;

        public Log()
        {
            //nothing yet
        }
        public Log(string filepath)
        {
            string path = Directory.GetCurrentDirectory();
            path += "\\";
            path += filepath;
            txtStream = new StreamWriter(path);
            txtStream.Write("::LOG FILE FOR GAZE AVASION STUDY::");
        }
        public Log(string filepath, string name)
        {
            string path = Directory.GetCurrentDirectory();
            path += "\\Logs\\";
            path += filepath;
            txtStream = new StreamWriter(path);
            txtStream.Write("::LOG FILE FOR GAZE AVASION STUDY::\r\n");
            txtStream.Write(name);
            txtStream.Write("\r\n");
        }

        public void logCondition(int type)
        {
            switch (type)
            {
                case 1:
                    txtStream.WriteLine("Condition: No Gaze Motions \r\n");
                    break;
                case 2:
                    txtStream.WriteLine("Condition: Correct Gaze Motions \r\n");
                    break;
                case 3:
                    txtStream.WriteLine("Condition: Wrong Gaze Motions \r\n");
                    break;
            }
        }

        public void write(string context)
        {
            txtStream.Write(context);
            txtStream.Write("\r\n");
        }

       

        public void logTime(string action, DateTime time)
        {
            string txt = action + " : " + time.ToString("hh:mm:ss.fff");
            txt += "\r\n";
            txtStream.Write(txt);
            txtStream.Flush();
        }

        public void End()
        {
            try
            {
                txtStream.Close();
            }
            catch (Exception)
            {
                //nothing
            }
        }
    }
}
