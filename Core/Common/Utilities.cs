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

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SSLRig.Core.Common
{
    //Some static methods used throughout the project.
    public class Statics
    {
        /// <summary>
        ///     Creates a deep clone of the provided object though serialization. 
        ///     Use only when necessary and try to use ICloneable instead.
        /// </summary>
        /// <param name="obj"> The input object that needs to be cloned.</param>
        /// <returns>An object identical to the provided object with different reference. </returns>
        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T) formatter.Deserialize(ms);
            }
        }

        /// <summary>
        ///     Set/Clear bit in a byte.
        /// </summary>
        /// <param name="inputByte"> Byte who's bit is to be set/Cleared.</param>
        /// <param name="pos">position of bit in the inputbyte (0-7).</param>
        /// <param name="value">true for seting and false for clearing bit.</param>
        /// <returns></returns>
        public static byte SetBit(byte inputByte, int pos, bool value)
        {
            if (value)
            {
                //left-shift 1, then bitwise OR
                inputByte = (byte) (inputByte | (1 << pos));
            }
            else
            {
                //left-shift 1, then take complement, then bitwise AND
                inputByte = (byte) (inputByte & ~(1 << pos));
            }
            return inputByte;
        }
    }
}