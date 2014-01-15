using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace HCI.Math.Kalman
{
    public class KalmanFilter
    {
        public double A { get; set; } //State Transformation Matrix
        public double B { get; set; } //Control Matrix
        public double u { get; set; } //control input
        public double K { get; set; } //Kalman Factor
        public double H { get; set; }
        public double lastX { get; set; } //Previous state prediction
        public double X { get; set; } //Current State prediction
        public double lastP { get; set; } //error of estimation
        public double P { get; set; } //error of estimation
        public double lastZ { get; set; } //Previous measurement
        public double Z { get; set; } // Current measurement;
        public double Q { get; set; } // error due to process
        public double R { get; set; } // error from measurements

        public void setLast(double value)
        {
            lastX = value;
        }

        public KalmanFilter()
        {
            lastX = 0;
            lastP = 0;
            Q = 0.6;
            R = 0.3;
            A = 1;
        }

        private void predict()
        {
            X = A * lastX; //X= A(X^) + B * U
            P = lastP + Q; // P = F * B^ (F^T) + Q
        }

        private void update()
        {
            K = P / (P + R);
            X = X + K * (Z - X);
            //Do something with X here

            //update the values for the next cycle. 
            lastP = (1 - K) * P;
            lastX = X;
        }

        public double Calculate(double Measurement)
        {
            predict();
            Z = Measurement;
            update();
            return X;
        }
    }
}
