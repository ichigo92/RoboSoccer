using System;
using SlimDX.Direct3D9;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// A class to store the parameters of a command to be sent to a robot. Implements the common IRobotInfo interface
    /// </summary>
    public class RobotParameters : IRobotInfo
    {
        protected bool _isBlue, _grab, _wheelSpeed, _spin;
        protected int _id;
        protected float _xVel, _yVel, _wVel, _kickSpeed, _chipSpeed, x, y, w;
        protected float[] _wheel = new float[4];
        protected float[] _wheelAngles = new float[4];

        //TargetAddress in String so that if some one is not using xbee he can also use it with proper casting func e.g."0x0013A20040AD75C2" to int64 will be Convert.ToInt64(String add,int base); where base=16 for hex
        protected string _targetAddress; 

        protected double _timeStamp;

        public RobotParameters() { }

        public RobotParameters(float x,float y,float w)
        {
            this.x = x;
            this.y = y;
            this.w = w;
        }

        public RobotParameters(bool isblue, int id, float timestamp, float xvelocity, float yvelocity, float wvelocity, float kickspeed, float chipspeed, bool grab, float[] wheelAngles, string comDeviceAddress, bool spin)
        {
            _isBlue = isblue;
            _id = id;
            _timeStamp = timestamp;
            _xVel = xvelocity;
            _yVel = yvelocity;
            _wVel = wvelocity;
            _kickSpeed = kickspeed;
            _chipSpeed = chipspeed;
            _grab = grab;
            _wheelAngles = wheelAngles;
            _targetAddress = comDeviceAddress;
            _spin = spin;
        }

        public RobotParameters(bool isblue, int id, float timestamp, float kickspeed, float chipspeed, bool grab, float[] wheelAngles, string comDeviceAddress, bool spin)
        {
            _isBlue = isblue;
            _id = id;
            _timeStamp = timestamp;
            _kickSpeed = kickspeed;
            _chipSpeed = chipspeed;
            _grab = grab;
            _wheelAngles = wheelAngles;
            _targetAddress = comDeviceAddress;
            _spin = spin;
        }

        public bool IsBlue
        {
            get { return _isBlue; }
            set { _isBlue = value; }
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public double TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; }
        }

        public float XVelocity
        {
            get { return _xVel; }
            set { _xVel = value; }
        }

        public float YVelocity
        {
            get { return _yVel; }
            set { _yVel = value; }
        }

        public float WVelocity
        {
            get { return _wVel; }
            set { _wVel = value; }
        }

        public float KickSpeed
        {
            get { return _kickSpeed; }
            set { _kickSpeed = value; }
        }

        public float ChipSpeed
        {
            get { return _chipSpeed; }
            set { _chipSpeed = value; }
        }

        public bool WheelSpeed
        {
            set { _wheelSpeed = value; }
            get { return _wheelSpeed; }
        }

        public bool Grab
        {
            get { return _grab; }
            set { _grab = value; }
        }

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float W
        {
            get { return w; }
            set { w = value; }
        }

        public float[] Wheel
        {
            get { return _wheel; }
            set { _wheel = value; }
        }

        public float[] WheelAngles
        {
            get { return _wheelAngles; }
            set { _wheelAngles = value; }
        }

        public float WheelVelocity(int wheelNumber)
        {
            return _wheel[wheelNumber];
        }

        public string TargetAddress
        {
            get { return _targetAddress; }
            set { _targetAddress = value; }
        }

        public object Clone()
        {
            RobotParameters roboparams = new RobotParameters();
            roboparams.Id = _id;
            roboparams.ChipSpeed = _chipSpeed;
            roboparams.TargetAddress = _targetAddress;
            roboparams.Grab = _grab;
            roboparams.IsBlue = _isBlue;
            roboparams.KickSpeed = _kickSpeed;
            roboparams.TimeStamp = _timeStamp;
            roboparams.W = w;
            roboparams.WVelocity = _wVel;
            roboparams.Wheel = _wheel != null ? (float[])_wheel.Clone() : null;
            roboparams.WheelAngles = _wheelAngles != null ? (float[]) _wheelAngles.Clone() : null;
            roboparams.WheelSpeed = _wheelSpeed;
            roboparams.X = x;
            roboparams.XVelocity = _xVel;
            roboparams.Y = y;
            roboparams.YVelocity = _yVel;
            roboparams.Spin = Spin;
            return roboparams;
        }

        /// <summary>
        /// The function converts the rectangular velocities to individual wheel velocities.
        /// </summary>
        public void ToIndividualWheels()
        {
            for (int i = 0; i < _wheel.Length; i++)
            {
                _wheel[i] = (float)((-Math.Sin(_wheelAngles[i]) * _xVel) + (Math.Cos(_wheelAngles[i]) * _yVel) + (1 * _wVel));
            }
        }


        public bool Spin
        {
            get { return _spin; }
            set { _spin = value; }
        }


        public void SetPoint(float x, float y, float w)
        {
            X = x;
            Y = y;
            W = w;
        }
    }
}
