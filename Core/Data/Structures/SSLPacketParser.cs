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
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// Class that stores the incomming vision data. 
    /// The data is parsed onto own and opponent depending on if the Rig User is playing from left or right.
    /// </summary>
    public class SSLPacketParser : IVisionData
    {
        #region SSL-Vision Packet Parsing
        protected SastaStack<List<SSL_DetectionBall>> _balls; //Who's got em?
        protected SastaStack<SSL_DetectionRobot>[] _ownRobots = new SastaStack<SSL_DetectionRobot>[12];
        protected SastaStack<SSL_DetectionRobot>[] _opponentRobots = new SastaStack<SSL_DetectionRobot>[12];
        protected SSL_GeometryData _geoData;
        protected List<SSL_DetectionRobot> _robotList = new List<SSL_DetectionRobot>();
        protected readonly object _geoLock = new object();
        protected int _stackSize;
        protected bool _isBlue = true;

        public SSLPacketParser()
        {
            _stackSize = 1;
            Initialize(_stackSize);
        }

        /// <summary>
        /// Initialize the parser with an ability to keep history of 'size' items
        /// </summary>
        /// <param name="size">The number of history items that can be persisted</param>
        public SSLPacketParser(int size)
        {
            _stackSize = size;
            Initialize(_stackSize);
        }

        public bool IsBlue
        {
            get { return _isBlue; }
            set { _isBlue = value; }
        }

        protected void Initialize(int stackSize)
        {
            if (stackSize < 1)
                throw new ArgumentOutOfRangeException(
                    "You need to initialize the SSLPacketParser with at least one packet stack size. ");
            _stackSize = stackSize;
            _balls = new SastaStack<List<SSL_DetectionBall>>(stackSize);
            for (int i = 0; i < _ownRobots.Length; i++)
            {
                _ownRobots[i] = new SastaStack<SSL_DetectionRobot>(_stackSize);
                _opponentRobots[i] = new SastaStack<SSL_DetectionRobot>(_stackSize);
            }
        }

        /// <summary>
        /// Parse the input packet so it is available on the parser's call
        /// </summary>
        /// <param name="packet">Input packet</param>
        public void ParsePacket(SSL_WrapperPacket packet)
        {
            if (packet != null)
            {
                if (_isBlue)
                {
                    foreach (SSL_DetectionRobot ownRobot in packet.detection.robots_blue)
                        _ownRobots[ownRobot.robot_id].Insert(ownRobot);
                    foreach (SSL_DetectionRobot opponentRobot in packet.detection.robots_yellow)
                        _opponentRobots[opponentRobot.robot_id].Insert(opponentRobot);
                }
                else
                {
                    foreach (SSL_DetectionRobot ownRobot in packet.detection.robots_yellow)
                        _ownRobots[ownRobot.robot_id].Insert(ownRobot);
                    foreach (SSL_DetectionRobot opponentRobot in packet.detection.robots_blue)
                        _opponentRobots[opponentRobot.robot_id].Insert(opponentRobot);
                }
                _balls.Insert(packet.detection.balls);
                if (packet.geometry != null)
                    lock (_geoLock)
                    {
                        _geoData = packet.geometry;
                    }
            }
        }

        /// <summary>
        /// Get all the latest packets of own robots
        /// </summary>
        /// <returns>All the latest packets of own robots</returns>
        public SSL_DetectionRobot[] Own()
        {
            _robotList.Clear();
            foreach (SastaStack<SSL_DetectionRobot> sasti in _ownRobots)
            {
                if (!sasti.IsEmpty())
                    _robotList.Add(sasti.GetLatest());
            }
            return _robotList.ToArray();
        }

        /// <summary>
        /// Get the packet with the specified own robot Id and received 'version' iterations before, returns the last
        /// if the version is greater than stack size
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <param name="version">History Version</param>
        /// <returns>The specified own robot's history packet</returns>
        public SSL_DetectionRobot Own(int id, int version)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            if (version >= _stackSize)
                version = _stackSize - 1;
            return _ownRobots[id][version];
        }

        /// <summary>
        /// Get the latest packet of the specified own robot Id
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <returns>The latest packet received for the specified own robot</returns>
        public SSL_DetectionRobot Own(int id)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            return _ownRobots[id].GetLatest();
        }

        /// <summary>
        /// Get all the packets in the history of specified own robot Id
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <returns>All the packets, including current and history of the specified own robot Id</returns>
        public SSL_DetectionRobot[] OwnAll(int id)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            return _ownRobots[id].GetAll();
        }

        /// <summary>
        /// Get all the latest packets of opponent robots
        /// </summary>
        /// <returns>All the latest packets of opponent robots</returns>
        public SSL_DetectionRobot[] Opponent()
        {
            _robotList.Clear();
            foreach (SastaStack<SSL_DetectionRobot> sasti in _opponentRobots)
            {
                if (!sasti.IsEmpty())
                    _robotList.Add(sasti.GetLatest());
            }
            return _robotList.ToArray();
        }

        /// <summary>
        /// Get the packet with the specified opponent robot Id and received 'version' iterations before, returns the last
        /// if the version is greater than stack size
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <param name="version">History Version</param>
        /// <returns>The specified opponent robot's history packet</returns>
        public SSL_DetectionRobot Opponent(int id, int version)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            return _opponentRobots[id][version];
        }

        /// <summary>
        /// Get the latest packet of the specified opponent robot Id
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <returns>The latest packet received for the specified opponent robot</returns>
        public SSL_DetectionRobot Opponent(int id)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            return _opponentRobots[id].GetLatest();
        }

        /// <summary>
        /// Get all the packets in the history of specified opponent robot Id
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <returns>All the packets, including current and history of the specified opponent robot Id</returns>
        public SSL_DetectionRobot[] OpponentAll(int id)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 active robots. ");
            return _opponentRobots[id].GetAll();
        }
        
        /// <summary>
        /// Get all the detection data of the ball from the letest packet
        /// </summary>
        /// <returns>An array of ball detection data</returns>
        public SSL_DetectionBall[] GetBalls()
        {
            return _balls.GetLatest().ToArray();
        }

        /// <summary>
        /// Get the packet of balls with the specified 'version' iterations before, returns the last
        /// if the version is greater than stack size
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public SSL_DetectionBall[] GetBalls(int version)
        {
            if (version >= _stackSize)
                version = _stackSize - 1;
            return _balls[version].ToArray();
        }

        /// <summary>
        /// Gets the geometery parameters received in the latest parsed packet
        /// </summary>
        /// <returns></returns>
        public SSL_GeometryData GetGeometery()
        {
            lock (_geoLock)
            {
                return _geoData;
            }
        }
        #endregion

    }
}
