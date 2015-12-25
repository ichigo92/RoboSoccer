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
using SSLRig.Core.Data.Structures;

namespace SSLRig.Core.Interface
{
    public interface IRepository
    {
        GameConfiguration Configuration { set; get; }
        IVisionData InData { get; set; }
        IRobotData OutData { get; set; }
        SSL_Referee RefereePacket { get; set; }
        event RefereeCommandHandler OnRefereeCommandChanged;
    }
}