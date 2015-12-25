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
using System.Linq;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// The structure used to warehouse all the outgoing data
    /// </summary>
    public class OutputRobotData : IRobotData, IDataSource
    {
        protected IRobotInfo[] _robotParams;
        protected object _robotParamLock;
        protected int _size;
        protected IRepository _parent;
        protected HashSet<int> _clearedRobots;

        public OutputRobotData()
        {
            Initialize(12);
        }


        public OutputRobotData(int size, IRepository parent)
        {
            _parent = parent;
            Initialize(size);
        }

        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        /// <summary>
        /// Initialize the structure to store 'size' robot's outgoing data. Its advised to initialize
        /// </summary>
        /// <param name="size">Total number of robots</param>
        public void Initialize(int size)
        {
            _size = size;
            _robotParams = new IRobotInfo[size];
            _clearedRobots = new HashSet<int>();
            for (int i = 0; i < size; i++)
            {
                _robotParams[i] = new RobotParameters();
                _robotParams[i].IsBlue = _parent.Configuration.IsBlueTeam;
                float[] angles;
                if (_parent.Configuration.IsRobotInUse(i) && (angles = _parent.Configuration.GetWheelAngels(i)) != null)
                {
                    _robotParams[i].WheelAngles = angles;
                }
            }
            _robotParamLock = new object[size];
            _robotParamLock = new object();
        }

        /// <summary>
        /// Gets/Sets the object storing output robot data object that implements the IRobotInfo interface
        /// </summary>
        /// <param name="index">The Id of robot</param>
        /// <returns></returns>
        public IRobotInfo this[int index]
        {
            get
            {
                if (_robotParams == null || _robotParamLock == null)
                    throw new NullReferenceException("The collection has not been initialized. ");
                if (index < 0 || index >= _size)
                    throw new IndexOutOfRangeException("Invalid index. ");
                lock (_robotParamLock)
                {
                    return _robotParams[index];
                }
            }
            set
            {
                if (_robotParams == null || _robotParamLock == null)
                    throw new NullReferenceException("The collection has not been initialized. ");
                if (index < 0 || index >= _size)
                    throw new IndexOutOfRangeException("Invalid index. ");
                lock (_robotParamLock)
                {
                    _robotParams[index] = value;
                }
            }
        }


        public bool IsAvailable(int index)
        {
            if (_robotParams == null || _robotParamLock == null)
                return false;
            else if (index < 0 || index > _size)
                return false;
            else if (this[index] == null)
                return false;
            else return true;
        }

        public IRepository Repository
        {
            get { return _parent; }
            set { _parent = value; }
        }


        public void ClearToSend(int id)
        {
            if (!_clearedRobots.Contains(id))
                _clearedRobots.Add(id);
        }

        public void ResetClearedRobots()
        {
            _clearedRobots.Clear();
        }

        public int[] ReadyToSend
        {
            get { return _clearedRobots.ToArray(); }
        }
    }
}
