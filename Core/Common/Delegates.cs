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


using SSLRig.Core.Data.Packet;
using SSLRig.Core.Interface;


//These delegates are used throughout the code for different works.
namespace SSLRig.Core.Common
{

    public delegate void RefereeCommandHandler(SSL_Referee refPacket);

    public delegate void RobotInfoWriter(IRobotInfo info);

    public delegate IRobotInfo RobotInfoReader(int id);

    public delegate void UpdateString(object sender, string s);

    public delegate ITask[] GetNextTasks();

}
