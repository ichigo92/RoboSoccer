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
using System.IO;
using System.Net;
using System.Net.Sockets;
using SSLRig.Core.Common;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Infrastructure.Communication
{
    /// <summary>
    /// A generic UDP Multicast Receiver. Used for both Vision and Referee data.
    /// </summary>
    public class MulticastReceiver : IPacketReceiver, ITask, IDataSource
    {
        protected uint _port;
        protected IPEndPoint _ipEndPoint;
        protected UdpClient _client;
        protected bool _isConnected = false;
        protected IPAddress _ipAddress;
        protected IRepository repReference;

        public uint Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public IPAddress IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; }
        }

        public MulticastReceiver() { }

        public MulticastReceiver(IPAddress ipaddress, uint port)
        {
            _ipAddress = ipaddress;
            _port = port;
            Connect();
        }

        public void Connect()
        {
            if (!_isConnected)
            {
                if (_port < 1 || _port > 65535)
                    throw new ArgumentOutOfRangeException(
                        "You can only assign a port from 1-65535 and not any of the reserved ports like 80. Get some basic knowledge on ports. ");
                if (_ipAddress == null)
                    throw new NullReferenceException(
                        "Give it an IP man, and it also needs to be one of the multicast IPs for this module to work.");
                _ipEndPoint = new IPEndPoint(IPAddress.Any, (int) _port);
                _client = new UdpClient();
                _client.Client.Bind(_ipEndPoint);
                _client.JoinMulticastGroup(_ipAddress);
                _isConnected = true;
            }

        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        public virtual object Receive()
        {
            return _client.Receive(ref _ipEndPoint);
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _client.DropMulticastGroup(_ipAddress);
                _client.Close();
                _client = null;
                _isConnected = false;
            }
        }

        #region ITask

        protected GetNextTasks fGetNextTask;
        public GetNextTasks GetNext
        {
            get { return fGetNextTask; }
            set { fGetNextTask = value; }
        }

        public virtual void Execute()
        { } 

        #endregion

        
        public IRepository Repository
        {
            get { return repReference; }
            set { repReference = value; }
        }
    }
}
