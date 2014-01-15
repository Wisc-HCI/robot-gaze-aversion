using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Check
{
    public class Check
    {
        private bool question1 = false;
        private bool question2 = false;
        private bool question3 = false;
        private bool question4 = false;
        private bool question5 = false;
        public bool pass = false;

        public Check()
        {
            //nothing happens yet
        }

        public void update(int type)
        {
            switch (type)
            {
                case 1:
                    question1 = true;
                    break;
                case 2:
                    question2 = true;
                    break;
                case 3:
                    question3 = true;
                    break;
                case 4:
                    question4 = true;
                    break;
                case 5:
                    question5 = true;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            update();
        }

        private void update()
        {
            if (question1 && question2 && question3 && question4 && question5)
            {
                pass = true;
            }
        }
    }
}
