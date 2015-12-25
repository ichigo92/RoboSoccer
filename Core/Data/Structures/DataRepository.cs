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

using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// This is an implementation of the central data repository used for data throughout a Rig 
    /// and implements the IRepository interface.
    /// </summary>
    public class DataRepository : IRepository
    {
        protected IRobotData _outData;
        protected IVisionData _inData;
        protected GameConfiguration _gameConfig;

        public DataRepository()
            : this(2, true, true)
        {}

        public DataRepository(int stackSize, bool isBlue)
            : this(stackSize, isBlue, true)
        {}

        public DataRepository(int stackSize, bool isBlue, bool PlayingFromLeft)
        {
            _gameConfig = new GameConfiguration(isBlue, PlayingFromLeft);
            _inData = new SSLPacketParser(stackSize);
            _outData = new OutputRobotData(12, this);
            ((IDataSource) _outData).Repository = this;

        }

        /// <summary>
        /// Gets/Sets a reference to the Input Data
        /// </summary>
        public IVisionData InData
        {
            get { return _inData; }
            set { _inData = value; }
        }

        /// <summary>
        /// Gets/Sets a reference to Output Data
        /// </summary>
        public IRobotData OutData
        {
            get { return _outData; }
            set { _outData = value; }
        }
        

        #region SSL-Referee Packet Management
        protected SSL_Referee pRefereePacket;
        protected readonly object pRefereeLock = new object();

        /// <summary>
        /// An Event that can be assigned a method as a RefereeCommandHandler delegate. The method is called
        /// whenever the referee command is changed
        /// </summary>
        public event RefereeCommandHandler OnRefereeCommandChanged;

        public SSL_Referee RefereePacket
        {
            get
            {
                lock (pRefereeLock)
                {
                    return pRefereePacket;
                }
            }
            set
            {
                lock (pRefereeLock)
                {
                    if (value.command != pRefereePacket.command)
                    {
                        if (OnRefereeCommandChanged != null)
                            OnRefereeCommandChanged(value);
                    }
                    pRefereePacket = value;
                }
            }
        }


        #endregion

        public GameConfiguration Configuration
        {
            get { return _gameConfig; }
            set { _gameConfig = value; }
        }
    }
}
