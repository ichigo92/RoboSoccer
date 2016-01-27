//    SSLRig - Small Size League Robot Integration Gadget
//    Copyright (C) 2015, Ron Beyer, Usman Shahid, Umer Javaid, Musaub Shaikh

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Drawing;
using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Data.Structures;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Infrastructure.Control
{
    /// <summary>
    /// A control generator for robots.
    /// Original implementation by Ron Beyer
    /// ref: http://www.codeproject.com/Articles/49548/Industrial-NET-PID-Controllers
    /// </summary>
    public class PIDController : IController, ITask, IDataSource
    {
        #region Fields

        protected int _id;
        //Gains
        protected double kpx;
        protected double kix;
        protected double kdx;
        protected double kpy;
        protected double kiy;
        protected double kdy;
        protected double kpw;
        protected double kiw;
        protected double kdw;

        //Running Values
        protected DateTime lastUpdatex;
        protected double lastPVx;
        protected double errSumx;
        protected DateTime lastUpdatey;
        protected double lastPVy;
        protected double errSumy;
        protected DateTime lastUpdatew;
        protected double lastPVw;
        protected double errSumw;

        //Max/Min Calculation
        protected double pvMaxx;
        protected double pvMinx;
        protected double outMaxx;
        protected double outMinx;
        protected double pvMaxy;
        protected double pvMiny;
        protected double outMaxy;
        protected double outMiny;
        protected double pvMaxw;
        protected double pvMinw;
        protected double outMaxw;
        protected double outMinw;

        protected IRepository _repReference;
        #endregion

        #region Properties

        public double PGainx
        {
            get { return kpx; }
            set { kpx = value; }
        }

        public double IGainx
        {
            get { return kix; }
            set { kix = value; }
        }

        public double DGainx
        {
            get { return kdx; }
            set { kdx = value; }
        }

        public double PVMinx
        {
            get { return pvMinx; }
            set { pvMinx = value; }
        }

        public double PVMaxx
        {
            get { return pvMaxx; }
            set { pvMaxx = value; }
        }

        public double OutMinx
        {
            get { return outMinx; }
            set { outMinx = value; }
        }

        public double OutMaxx
        {
            get { return outMaxx; }
            set { outMaxx = value; }
        }

        public double PGainy
        {
            get { return kpy; }
            set { kpy = value; }
        }

        public double IGainy
        {
            get { return kiy; }
            set { kiy = value; }
        }

        public double DGainy
        {
            get { return kdy; }
            set { kdy = value; }
        }

        public double PVMiny
        {
            get { return pvMiny; }
            set { pvMiny = value; }
        }

        public double PVMaxy
        {
            get { return pvMaxy; }
            set { pvMaxy = value; }
        }

        public double OutMiny
        {
            get { return outMiny; }
            set { outMiny = value; }
        }

        public double OutMaxy
        {
            get { return outMaxy; }
            set { outMaxy = value; }
        }

        public double PGainw
        {
            get { return kpw; }
            set { kpw = value; }
        }

        public double IGainw
        {
            get { return kiw; }
            set { kiw = value; }
        }

        public double DGainw
        {
            get { return kdw; }
            set { kdw = value; }
        }

        public double PVMinw
        {
            get { return pvMinw; }
            set { pvMinw = value; }
        }

        public double PVMaxw
        {
            get { return pvMaxw; }
            set { pvMaxw = value; }
        }

        public double OutMinw
        {
            get { return outMinw; }
            set { outMinw = value; }
        }

        public double OutMaxw
        {
            get { return outMaxw; }
            set { outMaxw = value; }
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        #endregion

        #region Construction / Deconstruction
        /// <summary>
        /// Constructor to the PID Controller 
        /// </summary>
        /// <param name="pid">Id of the controller</param>
        /// <param name="pGx">Proportional Gain of PID acting on velocity in x direction</param>
        /// <param name="iGx">Integral Gain of PID acting on velocity in x direction</param>
        /// <param name="dGx">Differential Gain of PID acting on velocity in x direction</param>
        /// <param name="pMaxx">Maximum value of X coodrinate of the field</param>
        /// <param name="pMinx">Minimum value of X coordineate of the field</param>
        /// <param name="oMaxx">Maximum Value that velocity in x direction can achieve</param>
        /// <param name="oMinx">Minimum Value that velocity in x direction can achieve</param>
        /// <param name="pGy">Proportional Gain of PID acting on velocity in y direction</param>
        /// <param name="iGy">Integral Gain of PID acting on velocity in y direction</param>
        /// <param name="dGy">Differential Gain of PID acting on velocity in Y direction</param>
        /// <param name="pMaxy">Maximum value of Y coordinate of the field</param>
        /// <param name="pMiny">Minimum value of Y coordinate of the field</param>
        /// <param name="oMaxy">Maximum Value that velocity in y direction can achieve</param>
        /// <param name="oMiny">Minimum Value that velocity in y direction can achieve</param>
        /// <param name="pGw">Proportional Gain of PID acting on angular velocity</param>
        /// <param name="iGw">Integral Gain of PID acting on angular velocity</param>
        /// <param name="dGw">Differential Gain of PID acting on angular velocity</param>
        /// <param name="pMaxw">Maximum angle (in radians) around which the robot can rotate</param>
        /// <param name="pMinw">Minimum angle (in radians) around which the robot can rotate</param>
        /// <param name="oMaxw">Maximum angular velocity achieveable</param>
        /// <param name="oMinw">Minimum angular velocity achieveable</param>
        public PIDController(int pid, double pGx, double iGx, double dGx,
            double pMaxx, double pMinx, double oMaxx, double oMinx,
            double pGy, double iGy, double dGy,
            double pMaxy, double pMiny, double oMaxy, double oMiny,
            double pGw, double iGw, double dGw,
            double pMaxw, double pMinw, double oMaxw, double oMinw)
        {
            _id = pid;
            kpx = pGx;
            kix = iGx;
            kdx = dGx;
            pvMaxx = pMaxx;
            pvMinx = pMinx;
            outMaxx = oMaxx;
            outMinx = oMinx;
            kpy = pGy;
            kiy = iGy;
            kdy = dGy;
            pvMaxy = pMaxy;
            pvMiny = pMiny;
            outMaxy = oMaxy;
            outMiny = oMiny;
            kpw = pGw;
            kiw = iGw;
            kdw = dGw;
            pvMaxw = pMaxw;
            pvMinw = pMinw;
            outMaxw = oMaxw;
            outMinw = oMinw;
        }

        public PIDController(int id) : this(id, 2, 0, 0, 3025, -3025, 2, -2, 2, 0, 0, 2025, -2025, 2, -2, 5, 0, 0,
            2*Math.PI, -2*Math.PI, Math.PI, -Math.PI)
        {
            
        }

        ~PIDController()
        {
        }

        #endregion

        #region Public Methods

        public void Reset()
        {
            errSumx = 0.0f;
            lastUpdatex = DateTime.Now;
            errSumy = 0.0f;
            lastUpdatey = DateTime.Now;
            errSumw = 0.0f;
            lastUpdatew = DateTime.Now;
            RobotParameters roboparams = new RobotParameters();
            roboparams.Id = _id;
            roboparams.XVelocity = 0;
            roboparams.YVelocity = 0;
            roboparams.WVelocity = 0;
        }

        public void Compute()
        {
            if (_repReference==null)
                throw new NullReferenceException("The controller needs to read data through functions assigned to the delegates Read and Write. Assign them a method.");
            IRobotInfo Reference = _repReference.OutData[_id];
            SSL_DetectionRobot Feedback = _repReference.InData.Own(_id);
            double pvx = (double)Math.Round((decimal)Feedback.x, 0, MidpointRounding.AwayFromZero);
            double pvy = (double)Math.Round((decimal)Feedback.y, 0, MidpointRounding.AwayFromZero);
            double pvw = (double)Math.Round((decimal)Feedback.orientation, 5, MidpointRounding.AwayFromZero);
            float tempRobotAngle = Feedback.orientation;
            double spx = (double)Math.Round((decimal)Reference.X, 0, MidpointRounding.AwayFromZero);
            double spy = Reference.Y;
            double spw = Reference.W;
            #region 180 Degree Error Correction
            if ((spw - pvw) < -Math.PI)
            { spw += (2 * Math.PI); }
            else if ((spw - pvw) > Math.PI)
            { pvw += (2 * Math.PI); }
            #endregion

            pvx = Clamp(pvx, pvMinx, pvMaxx);
            pvx = ScaleValue(pvx, pvMinx, pvMaxx, -1.0f, 1.0f);
            pvy = Clamp(pvy, pvMiny, pvMaxy);
            pvy = ScaleValue(pvy, pvMiny, pvMaxy, -1.0f, 1.0f);
            pvw = Clamp(pvw, pvMinw, pvMaxw);
            pvw = ScaleValue(pvw, pvMinw, pvMaxw, -1.0f, 1.0f);

            spx = Clamp(spx, pvMinx, pvMaxx);
            spx = ScaleValue(spx, pvMinx, pvMaxx, -1.0f, 1.0f);
            spy = Clamp(spy, pvMiny, pvMaxy);
            spy = ScaleValue(spy, pvMiny, pvMaxy, -1.0f, 1.0f);
            spw = Clamp(spw, pvMinw, pvMaxw);
            spw = ScaleValue(spw, pvMinw, pvMaxw, -1.0f, 1.0f);

            double errx = spx - pvx;
            double erry = spy - pvy;
            double errw = spw - pvw;

            double pTermx = errx * kpx;
            double iTermx = 0.0f;
            double dTermx = 0.0f;
            double pTermy = erry * kpy;
            double iTermy = 0.0f;
            double dTermy = 0.0f;
            double pTermw = errw * kpw;
            double iTermw = 0.0f;
            double dTermw = 0.0f;

            double partialSumx = 0.0f;
            DateTime nowTimex = DateTime.Now;
            double partialSumy = 0.0f;
            DateTime nowTimey = DateTime.Now;
            double partialSumw = 0.0f;
            DateTime nowTimew = DateTime.Now;

            if (lastUpdatex != null)
            {
                double dTx = (nowTimex - lastUpdatex).TotalSeconds;

                if (pvx >= pvMinx && pvx <= pvMaxx)
                {
                    partialSumx = errSumx + dTx * errx;
                    iTermx = kix * partialSumx;
                }

                if (dTx != 0.0f)
                    dTermx = kdx * (pvx - lastPVx) / dTx;
            }

            if (lastUpdatey != null)
            {
                double dTy = (nowTimey - lastUpdatey).TotalSeconds;

                if (pvy >= pvMiny && pvy <= pvMaxy)
                {
                    partialSumy = errSumy + dTy * erry;
                    iTermy = kiy * partialSumy;
                }

                if (dTy != 0.0f)
                    dTermy = kdy * (pvy - lastPVy) / dTy;
            }

            if (lastUpdatew != null)
            {
                double dTw = (nowTimew - lastUpdatew).TotalSeconds;

                if (pvw >= pvMinw && pvw <= pvMaxw)
                {
                    partialSumw = errSumw + dTw * errw;
                    iTermw = kiw * partialSumw;
                }

                if (dTw != 0.0f)
                    dTermw = kdw * (pvw - lastPVw) / dTw;
            }

            lastUpdatex = nowTimex;
            errSumx = partialSumx;
            lastPVx = pvx;
            lastUpdatey = nowTimey;
            errSumy = partialSumy;
            lastPVy = pvy;
            lastUpdatew = nowTimew;
            errSumw = partialSumw;
            lastPVw = pvw;

            double outRealx = pTermx + iTermx + dTermx;
            double outRealy = pTermy + iTermy + dTermy;
            double outRealw = pTermw + iTermw + dTermw;

            #region Rotation Transform for angle Compensation
            PointF temp = RotatePoint(new PointF((float)outRealx, (float)outRealy), new PointF(0, 0), -tempRobotAngle);
            outRealx = temp.X;
            outRealy = temp.Y;
            #endregion

            outRealx = Clamp(outRealx, -1.0f, 1.0f);
            outRealx = ScaleValue(outRealx, -1.0f, 1.0f, outMinx, outMaxx);
            outRealy = Clamp(outRealy, -1.0f, 1.0f);
            outRealy = ScaleValue(outRealy, -1.0f, 1.0f, outMiny, outMaxy);
            outRealw = Clamp(outRealw, -1.0f, 1.0f);
            outRealw = ScaleValue(outRealw, -1.0f, 1.0f, outMinw, outMaxw);


            //Write it out to the world
            IRobotInfo roboparams = Reference;
            roboparams.Id = _id;
            roboparams.XVelocity = (float)outRealx;
            roboparams.YVelocity = (float)outRealy;
            roboparams.WVelocity = (float)outRealw;
            Repository.OutData.ClearToSend(roboparams.Id);
        }

        public IRobotInfo ComputeExclusive(IRobotInfo target, SSL_DetectionRobot current)
        {
            if (_repReference == null)
                throw new NullReferenceException("The controller needs to read data through functions assigned to the delegates Read and Write. Assign them a method.");
            IRobotInfo Reference = target;
            SSL_DetectionRobot Feedback = current;
            double pvx = (double)Math.Round((decimal)Feedback.x, 0, MidpointRounding.AwayFromZero);
            double pvy = (double)Math.Round((decimal)Feedback.y, 0, MidpointRounding.AwayFromZero);
            double pvw = (double)Math.Round((decimal)Feedback.orientation, 5, MidpointRounding.AwayFromZero);
            float tempRobotAngle = Feedback.orientation;
            double spx = (double)Math.Round((decimal)Reference.X, 0, MidpointRounding.AwayFromZero);
            double spy = Reference.Y;
            double spw = Reference.W;
            #region 180 Degree Error Correction
            if ((spw - pvw) < -Math.PI)
            { spw += (2 * Math.PI); }
            else if ((spw - pvw) > Math.PI)
            { pvw += (2 * Math.PI); }
            #endregion

            pvx = Clamp(pvx, pvMinx, pvMaxx);
            pvx = ScaleValue(pvx, pvMinx, pvMaxx, -1.0f, 1.0f);
            pvy = Clamp(pvy, pvMiny, pvMaxy);
            pvy = ScaleValue(pvy, pvMiny, pvMaxy, -1.0f, 1.0f);
            pvw = Clamp(pvw, pvMinw, pvMaxw);
            pvw = ScaleValue(pvw, pvMinw, pvMaxw, -1.0f, 1.0f);

            spx = Clamp(spx, pvMinx, pvMaxx);
            spx = ScaleValue(spx, pvMinx, pvMaxx, -1.0f, 1.0f);
            spy = Clamp(spy, pvMiny, pvMaxy);
            spy = ScaleValue(spy, pvMiny, pvMaxy, -1.0f, 1.0f);
            spw = Clamp(spw, pvMinw, pvMaxw);
            spw = ScaleValue(spw, pvMinw, pvMaxw, -1.0f, 1.0f);

            double errx = spx - pvx;
            double erry = spy - pvy;
            double errw = spw - pvw;

            double pTermx = errx * kpx;
            double iTermx = 0.0f;
            double dTermx = 0.0f;
            double pTermy = erry * kpy;
            double iTermy = 0.0f;
            double dTermy = 0.0f;
            double pTermw = errw * kpw;
            double iTermw = 0.0f;
            double dTermw = 0.0f;

            double partialSumx = 0.0f;
            DateTime nowTimex = DateTime.Now;
            double partialSumy = 0.0f;
            DateTime nowTimey = DateTime.Now;
            double partialSumw = 0.0f;
            DateTime nowTimew = DateTime.Now;

            if (lastUpdatex != null)
            {
                double dTx = (nowTimex - lastUpdatex).TotalSeconds;

                if (pvx >= pvMinx && pvx <= pvMaxx)
                {
                    partialSumx = errSumx + dTx * errx;
                    iTermx = kix * partialSumx;
                }

                if (dTx != 0.0f)
                    dTermx = kdx * (pvx - lastPVx) / dTx;
            }

            if (lastUpdatey != null)
            {
                double dTy = (nowTimey - lastUpdatey).TotalSeconds;

                if (pvy >= pvMiny && pvy <= pvMaxy)
                {
                    partialSumy = errSumy + dTy * erry;
                    iTermy = kiy * partialSumy;
                }

                if (dTy != 0.0f)
                    dTermy = kdy * (pvy - lastPVy) / dTy;
            }

            if (lastUpdatew != null)
            {
                double dTw = (nowTimew - lastUpdatew).TotalSeconds;

                if (pvw >= pvMinw && pvw <= pvMaxw)
                {
                    partialSumw = errSumw + dTw * errw;
                    iTermw = kiw * partialSumw;
                }

                if (dTw != 0.0f)
                    dTermw = kdw * (pvw - lastPVw) / dTw;
            }

            lastUpdatex = nowTimex;
            errSumx = partialSumx;
            lastPVx = pvx;
            lastUpdatey = nowTimey;
            errSumy = partialSumy;
            lastPVy = pvy;
            lastUpdatew = nowTimew;
            errSumw = partialSumw;
            lastPVw = pvw;

            double outRealx = pTermx + iTermx + dTermx;
            double outRealy = pTermy + iTermy + dTermy;
            double outRealw = pTermw + iTermw + dTermw;

            #region Rotation Transform for angle Compensation
            PointF temp = RotatePoint(new PointF((float)outRealx, (float)outRealy), new PointF(0, 0), -tempRobotAngle);
            outRealx = temp.X;
            outRealy = temp.Y;
            #endregion

            outRealx = Clamp(outRealx, -1.0f, 1.0f);
            outRealx = ScaleValue(outRealx, -1.0f, 1.0f, outMinx, outMaxx);
            outRealy = Clamp(outRealy, -1.0f, 1.0f);
            outRealy = ScaleValue(outRealy, -1.0f, 1.0f, outMiny, outMaxy);
            outRealw = Clamp(outRealw, -1.0f, 1.0f);
            outRealw = ScaleValue(outRealw, -1.0f, 1.0f, outMinw, outMaxw);


            //Write it out to the world
            IRobotInfo roboparams = Reference;
            roboparams.Id = _id;
            roboparams.XVelocity = (float)outRealx;
            roboparams.YVelocity = (float)outRealy;
            roboparams.WVelocity = (float)outRealw;
            return roboparams;
        }

        #endregion

        #region Protected Methods

        protected double ScaleValue(double value, double valuemin, double valuemax, double scalemin, double scalemax)
        {
            double vPerc = (value - valuemin) / (valuemax - valuemin);
            double bigSpan = vPerc * (scalemax - scalemin);

            double retVal = scalemin + bigSpan;

            return retVal;
        }

        protected double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }

        protected PointF RotatePoint(PointF pointToRotate, PointF centerPoint, float angleInRad)
        {
            double cosTheta = Math.Cos(angleInRad);
            double sinTheta = Math.Sin(angleInRad);
            PointF ans = new PointF();
            ans.X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X);
            ans.Y =
                (float)
                (sinTheta * (pointToRotate.X - centerPoint.X) +
                cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y);
            return ans;
        }



        #endregion
        
        #region ITask

        protected GetNextTasks fGetNextTask;
        public Common.GetNextTasks GetNext
        {
            get { return fGetNextTask; }
            set { fGetNextTask = value; }
        }

        public void Execute()
        {
            Compute();
        }

        #endregion

        
        public IRepository Repository
        {
            get { return _repReference; }
            set { _repReference = value; }
        }
    }
}
