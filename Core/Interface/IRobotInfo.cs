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

namespace SSLRig.Core.Interface
{
    public interface IRobotInfo : ICloneable
    {
        bool IsBlue { set; get; }
        int Id { set; get; }
        double TimeStamp { set; get; }
        float XVelocity { set; get; }
        float YVelocity { set; get; }
        float WVelocity { set; get; }
        float[] Wheel { set; get; }
        float[] WheelAngles { set; get; }
        string TargetAddress { set; get; }
        float KickSpeed { set; get; }
        float ChipSpeed { set; get; }
        bool WheelSpeed { set; get; }
        bool Grab { set; get; }
        float X { set; get; }
        float Y { set; get; }
        float W { set; get; }
        bool Spin { set; get; }

        void SetPoint(float x, float y, float w);
        void ToIndividualWheels();
    }
}
