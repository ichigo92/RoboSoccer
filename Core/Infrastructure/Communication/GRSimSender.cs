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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SSLRig.Core.Common;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Infrastructure.Communication
{
    /// <summary>
    /// A command sender client for GRSim Simulator. 
    /// </summary>
    public class GRSimSender : IPacketSender, ITask, IDataSource
    {
        protected UdpClient _client;
        protected IPAddress _ip;
        protected uint _port;
        protected IPEndPoint _ipEndpoint;
        protected bool _isConnected = false, _isLocked = false;
        protected readonly object _resLock;
        protected IRepository repReference;


        public IPAddress Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }

        public uint Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public GRSimSender()
        {
            _resLock = new object();
        }

        public GRSimSender(IPAddress ip, uint port)
        {
            _resLock = new object();
            _ip = ip;
            _port = port;
            Connect();
        }

        public void Connect()
        {
            if (!_isConnected)
            {
                if (_ip == null)
                    throw new NullReferenceException("Dude, give an IP Address to connect on, its NULL at the moment.");
                if (_port < 1 || Port > 65535)
                    throw new Exception(
                        "Seriously? Read info on ports. Available ports to assign is between 1-65535 and you can't use port 80 or any other reserved port.");
                try
                {
                    _ipEndpoint = new IPEndPoint(_ip, (int) _port);
                    Monitor.TryEnter(_resLock, ref _isLocked);
                    _client = new UdpClient((int) _port);
                    _isConnected = true;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new ArgumentOutOfRangeException(
                        "Seriously? Read info on ports. Available ports to assign is between 1-65535 and you can't use port 80 or any other reserved port.",
                        ex);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (_isLocked)
                    {
                        Monitor.Exit(_resLock);
                        _isLocked = false;
                    }
                }
            }
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        public void Send(IRobotInfo Packet)
        {
            if (Packet != null && _isConnected)
            {
                if (_isConnected)
                {
                    grSim_Robot_Command s1 =
                        grSim_Robot_Command.CreateBuilder()
                            .SetId((uint) Packet.Id)
                            .SetWheel1(Packet.Wheel[0])
                            .SetWheel2(Packet.Wheel[1])
                            .SetWheel3(Packet.Wheel[2])
                            .SetWheel4(Packet.Wheel[3])
                            .SetKickspeedx(Packet.KickSpeed)
                            .SetKickspeedz(Packet.ChipSpeed)
                            .SetVeltangent(Packet.XVelocity)
                            .SetVelnormal(Packet.YVelocity)
                            .SetVelangular(Packet.WVelocity)
                            .SetWheelsspeed(Packet.WheelSpeed)
                            .SetSpinner(Packet.Spin)
                            .Build();
                    grSim_Commands s2 =
                        grSim_Commands.CreateBuilder()
                            .SetTimestamp(Packet.TimeStamp)
                            .SetIsteamyellow(!Packet.IsBlue)
                            .AddRobotCommands(s1)
                            .Build();
                    grSim_Packet finalPacket = grSim_Packet.CreateBuilder().SetCommands(s2).Build();
                    string strBuffer = finalPacket.ToString();
                    byte[] byteBuffer = finalPacket.ToByteArray();
                    try
                    {
                        Monitor.Enter(_resLock, ref _isLocked);
                        _client.Send(byteBuffer, byteBuffer.Length, _ipEndpoint);
                        return;
                    }
                    finally
                    {
                        if (_isLocked)
                        {
                            Monitor.Exit(_resLock);
                            _isLocked = false;
                        }
                    }
                }
                else
                    throw new Exception("Man connect the darn thing first. ");
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                try
                {
                    Monitor.Enter(_resLock, ref _isLocked);
                    _client.Close();
                    _isConnected = false;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (_isLocked)
                    {
                        Monitor.Exit(_resLock);
                        _isLocked = false;
                    }
                }
            }
        }

        #region ITask

        protected GetNextTasks fGetNextTask;
        public GetNextTasks GetNext
        {
            get { return fGetNextTask; }
            set { fGetNextTask = value; }
        }

        public void Execute()
        {
            if (repReference != null)
            {
                IRobotInfo packettosend;
                foreach (var id in repReference.OutData.ReadyToSend)
                {
                    packettosend = repReference.OutData[id];
                    Send(packettosend);
                }
                repReference.OutData.ResetClearedRobots();
            }
            else
            {
                throw new NotImplementedException("Reference to repository not set. ");
            }
        }

        #endregion

        public IRepository Repository
        {
            get { return repReference; }
            set { repReference = value; }
        }
    }
}
