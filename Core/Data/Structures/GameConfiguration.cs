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
using System.Configuration;
using System.Linq;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// Saves the configuration for a team in a game. The configuration also contains the information of all the robots in use
    /// and their respective wheel angles i.e. the angles between the wheels which can be used to convert the rectangular velocities
    /// to individual wheel velocities
    /// </summary>
    public class GameConfiguration
    {
        protected bool _isBlueTeam;
        protected bool _isPlayingFromLeft;
        protected Dictionary<int, float[]> _playingRobots;

        public GameConfiguration()
        {
            Initialize(true, true);
        }

        /// <summary>
        /// Initialize the configuration which stores whether you're the blue team and whether you're playing from left
        /// </summary>
        /// <param name="IsBlue">If your team is blue</param>
        /// <param name="PlayingFromLeft">If you're playing from left</param>
        public GameConfiguration(bool IsBlue, bool PlayingFromLeft)
        {
            Initialize(IsBlue, PlayingFromLeft);
        }

        public void Initialize(bool IsBlue, bool PlayingFromLeft)
        {
            _isBlueTeam = IsBlue;
            _isPlayingFromLeft = PlayingFromLeft;
            _playingRobots = new Dictionary<int, float[]>();
        }

        public bool IsBlueTeam
        {
            get { return _isBlueTeam; }
            set { _isBlueTeam = value; }
        }

        public bool IsPlayingFromLeft
        {
            get { return _isPlayingFromLeft; }
            set { _isPlayingFromLeft = value; }
        }

        /// <summary>
        /// Gets an array of all the robots initialized.
        /// </summary>
        /// <returns></returns>
        public int[] GetAllPlayingRobots()
        {
            if (_playingRobots == null)
                throw new ConfigurationException("No robots have been initialized yet. ");
            return _playingRobots.Keys.ToArray();
        }

        /// <summary>
        /// Gets the wheel angels parameters set for the specified robot. The robot needs to be initialized with AddRobot call.
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <returns>An array containing wheel angles as stored by the user</returns>
        public float[] GetWheelAngels(int id)
        {
            if (_playingRobots == null)
                throw new ConfigurationException("No robots have been initialized yet. ");
            if (!_playingRobots.ContainsKey(id))
                throw new NullReferenceException("The robot has not been initialized. ");
            return _playingRobots[id];
        }

        /// <summary>
        /// Initialize the specified robot Id with the given wheel angles
        /// </summary>
        /// <param name="id">Robot Id</param>
        /// <param name="wheelAngles">Wheel angles</param>
        public void AddRobot(int id, float[] wheelAngles)
        {
            if (id < 0 || id > 11)
                throw new ArgumentOutOfRangeException("There can only be 12 robots. ");
            _playingRobots.Add(id, wheelAngles);
        }

        public bool IsRobotInUse(int id)
        {
            if (id < 0 || id > 11)
                return false;
            if (_playingRobots.ContainsKey(id))
                return true;
            return false;
        }

        public bool IsConfigured
        {
            get
            {
                if (_playingRobots.Count < 1)
                    return false;
                else return true;
            }
        }
    }
}
