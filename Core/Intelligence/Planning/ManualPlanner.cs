//    SSLRig - Small Size League Robot Integration Gadget
//    Copyright (C) 2015, Usman Shahid, Umer Javaid, Musaub Shaikh

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
using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;
using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Data.Structures;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Intelligence.Planning
{
    /// <summary>
    /// A class to generate manual commands through a joystick or game controller.
    /// </summary>
    internal class ManualPlanner : IPlanner, ITask, IDataSource
    {
        protected Joystick[] _gamePads;
        protected JoystickState[] _states;
        protected int _joyStickMin = -1000;
        protected int _joyStickMax = 1000;
        protected SSL_Referee _refereeCommand;
        protected IRepository _repReference;

        public void Initialize()
        {
            DirectInput dInput = new DirectInput();
            IList<DeviceInstance> deviceList = dInput.GetDevices(DeviceClass.GameController,
                DeviceEnumerationFlags.AttachedOnly);
            if (deviceList.Count == 0)
            {
                throw new Exception("No Device is connected to the system.");
            }
            else
            {
                _gamePads = new Joystick[deviceList.Count];
                _states = new JoystickState[deviceList.Count];

                //Initialize all GamePads Connected to the System
                for (int i = 0; i < deviceList.Count; i++)
                {
                    _gamePads[i] = new Joystick(dInput, deviceList[i].InstanceGuid);
                    _states[i] = new JoystickState();
                    //DONT know what to sned in this .. its a handle to the window .. so the gui handle will be send i guess .. maybe
                    //pGamePads[i].SetCooperativeLevel(this, CooperativeLevel.Exclusive | CooperativeLevel.Foreground); 
                }
                for (int i = 0; i < _gamePads.Length; i++)
                {
                    foreach (DeviceObjectInstance deviceObject in _gamePads[i].GetObjects())
                    {
                        if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                        {
                            _gamePads[i].GetObjectPropertiesById((int)deviceObject.ObjectType)
                                .SetRange(_joyStickMin, _joyStickMax);
                        }
                    }
                    //Get Access to the Input Device
                    _gamePads[i].Acquire();
                }

            }
        }

        public void Plan()
        {
            for (int i = 0; i < _gamePads.Length; i++)
            {
                if (_gamePads[i].Acquire().IsFailure)
                    continue;
                if (_gamePads[i].Poll().IsFailure)
                    continue;
                _states[i] = _gamePads[i].GetCurrentState();
                if (Result.Last.IsFailure)
                    continue;
                else
                {
                    if (_repReference != null)
                    {
                        //Get Current Robot Position
                        //Set Next Robot Position By adding the GamePad Values in Current Positon
                        // example 
                        //currVAlX += _states[i].X;
                        //currVALY += _states[i].Y;
                        //currVALZ += _states[i].Z;
                        //Call GrabBall Function from AI AIPlanner if you want (To Rotate and Go to that point)
                        //Call GotoBall Function from AI AIPlanner if you want (TO Goto that point with Same direction)
                        //Send Value to PID;
                        //Write values to _repReference.OutData
                    }
                    else throw new NotImplementedException("Data writer not provided. ");
                }
            }
        }
        
        public IRobotInfo[] PlanExclusive(SSL_WrapperPacket mainPacket)
        {
            for (int i = 0; i < _gamePads.Length; i++)
            {
                if (_gamePads[i].Acquire().IsFailure)
                    continue;
                if (_gamePads[i].Poll().IsFailure)
                    continue;
                _states[i] = _gamePads[i].GetCurrentState();
                if (Result.Last.IsFailure)
                    continue;
                else
                {
                    IRobotInfo robotInfo = new RobotParameters();
                    robotInfo.Id = 1;
                    //Get Current Robot Position
                    //Set Next Robot Position By adding the GamePad Values in Current Positon
                    // example 
                    //currVAlX += _states[i].X;
                    //currVALY += _states[i].Y;
                    //currVALZ += _states[i].Z;
                    //Call GrabBall Function from AI AIPlanner if you want (To Rotate and Go to that point)
                    //Call GotoBall Function from AI AIPlanner if you want (TO Goto that point with Same direction)
                    //Send Value to PID;
                    //Write values to _repReference.OutData
                    return new[] {robotInfo};
                }
            }
            return null;
        }
        
        public void Release()
        {
            if (_gamePads.Length != 0)
            {
                for (int i = 0; i < _gamePads.Length; i++)
                {
                    _gamePads[i].Unacquire();
                    _gamePads[i].Dispose();
                }
            }
            _gamePads = null;
            _states = null;
        }

        #region ITask

        protected GetNextTasks fGetNextTask;
        public Common.GetNextTasks GetNext
        {
            get { return fGetNextTask; }
            set { fGetNextTask = value; }
        }

        public void Execute()
        {
            Plan();
        }


        #endregion

        public IRepository Repository
        {
            get { return _repReference; }
            set { _repReference = value; }
        }

        public void OnRefereeCommandChanged(SSL_Referee command)
        {
            this._refereeCommand = command;
        }
    }
}
