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
using System.IO.Ports;
using System.Text;
using SSLRig.Core.Common;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Infrastructure.Communication
{
    /// <summary>
    /// An implementation of command sender for XBee modules. 
    /// The module utilizes a PacketMaker objects that implements the IXbeePacketMaker interface. This object defines the packet format.
    /// </summary>
    public class XBeeSender : IPacketSender,ITask,IDataSource
    {
        protected SerialPort _serialPort;
        protected string _comPortName;
        protected IXBeePacketMaker _packetMaker;
        protected bool _isConnected = false;
        protected IRepository repReference;
        protected GetNextTasks _getNext;

        public string COMPort
        {
            get { return _comPortName; }
            set { _comPortName = value; }
        }

        public XBeeSender()
        {

        }


        public XBeeSender(string ComPort)
        {
            _comPortName = ComPort;
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        public void Disconnect()
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            _isConnected = false;
        }

        public void Connect()
        {
            _serialPort = new SerialPort(_comPortName);
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = System.IO.Ports.Parity.None;
            _serialPort.DataBits = 8;
            _packetMaker = new XBeePacketMaker(5);
            _serialPort.Open();
            _isConnected = _serialPort.IsOpen;
        }

        public void Send(IRobotInfo Packet)
        {
            byte[] data = CreateDataBytes(Packet);
            Int64 DestinationAddress = Convert.ToInt64(Packet.TargetAddress, 16);
            byte[] XbeePacket = _packetMaker.MakePacket(DestinationAddress, data);
            string show = ByteArrayToString(XbeePacket);
            _serialPort.Write(XbeePacket, 0, XbeePacket.Length);
        }

        private string ByteArrayToString(byte[] byteArr)
        {
            StringBuilder hex = new StringBuilder(byteArr.Length * 3);
            foreach (byte b in byteArr)
            {
                hex.AppendFormat("{0:x2}", b);
                hex.Append(' ');
            }
            return hex.ToString();
        }

        private byte[] CreateDataBytes(IRobotInfo packet)
        {
            byte[] data = new byte[5];
            packet.ToIndividualWheels();
            byte[] PWMs = Quantize(packet);
            for (int i = 0; i < PWMs.Length; i++)
            {
                data[i] = PWMs[i];
            }
            byte lastByte = 0x00;
            if (packet.KickSpeed > 0 && packet.KickSpeed <= 10)
            {
                lastByte = (byte)packet.KickSpeed;
            }
            //SET DRIBBLER
            lastByte = Statics.SetBit(lastByte, 7, packet.Grab);
            //SET FLAT KICK
            if (packet.KickSpeed > 0)
            { lastByte = Statics.SetBit(lastByte, 6, true); }
            if (packet.ChipSpeed > 0)
            { lastByte = Statics.SetBit(lastByte, 5, true); }
            data[4] = lastByte;
            return data;
        }

        protected byte[] Quantize(IRobotInfo packet)
        {
            float multiplier = (127f / 2f);
            byte[] PWMs = new byte[4];
            for (int i = 0; i < PWMs.Length; i++)
            {
                byte temp = 0x00;
                if (packet.Wheel[i] < 0)  //if negative
                {
                    float tempvel = Math.Abs(packet.Wheel[i]);
                    temp = (byte)(tempvel * multiplier);
                    temp = Statics.SetBit(temp, 7, true);
                }
                else                //if zero or positive
                {
                    temp = (byte)(packet.Wheel[i] * multiplier);
                }
                PWMs[i] = temp;
            }
            return PWMs;
        }

        public GetNextTasks GetNext
        {
            get { return _getNext; }
            set { _getNext = value; }
        }

        public void Execute()
        {
            if (repReference != null)
            {
                foreach (var id in repReference.OutData.ReadyToSend)
                {
                    IRobotInfo packet = repReference.OutData[id];
                    Send(packet);
                }
                repReference.OutData.ResetClearedRobots();
            }
            else throw new NullReferenceException("XBeeSender's repository not set. ");
        }

        public IRepository Repository
        {
            get { return repReference; }
            set { repReference = value; }
        }
    }

    public interface IXBeePacketMaker
    {
        /// <summary>
        /// Makes a Byte Array with all the required Parameters and Checksum with the given 64-bit Destination Address and given data
        /// </summary>
        /// <param name="destinationAddress">64-Bit End Device Destination Address</param>
        /// <param name="data">Byte Array of Data to Be Transmit</param>
        /// <returns>Packet with all the required bytes ready to write on the COM Port on which XBee is Connected</returns>
        byte[] MakePacket(Int64 destinationAddress, byte[] data);
    }

    public class XBeePacketMaker : IXBeePacketMaker
    {
        protected byte[] pPacket;
        protected const int pHeaderLength = 18;
        protected byte broadcastRadius;

        public byte BroadcastRadius
        {
            get { return broadcastRadius; }
            set { broadcastRadius = value; }
        }

        /// <summary>
        /// Initializes an instance of PacketMaker Class for XBee Packet Making 
        /// </summary>
        /// <param name="dataLength">Length of the Databyte[] in the Packet. It Must be equal to the length of byte[]Data being passed in MakePacket Function</param>
        public XBeePacketMaker(int dataLength)
        {
            pPacket = new byte[pHeaderLength + dataLength];
            //Start Delimiter
            pPacket[0] = 0x7E;
            //2 Bytes For Length (2 Bytes => Max Length = 65536 Bytes). Length= Allbytes Except Start Delimiter, Length bytes and Checksum
            byte[] tempSizeBytes = BitConverter.GetBytes((Int16)pHeaderLength + dataLength - 4);
            //Length ByteH 
            pPacket[1] = tempSizeBytes[1];
            //Length ByteL
            pPacket[2] = tempSizeBytes[0];
            //Frame Type (10=Transmit Request)
            pPacket[3] = (byte)XBeeAPICommandId.TRANSMIT_DATA_REQUEST;
            //API FrameID   (0= No Acknowledgment of data received, if set to 1 then Destination Sends and Acknowledgment to the Source)
            pPacket[4] = (byte)XBeeFrameID.DoNotRequestResponse;
            //pPacket[5]->pPacket[12] bytes = 8Byte Destination Address
            //2Bytes Destination Address if known. set to 0xFFFE if unknown or broadcasting
            byte[] DestinationNetworkAdd = BitConverter.GetBytes((int)XBeeDestinationNetworkAddress.UnknownOrBroadcast);
            //Destination Address ByteH
            pPacket[13] = DestinationNetworkAdd[1];
            //Destination Address ByteL
            pPacket[14] = DestinationNetworkAdd[0];
            //BroadCast Radius Maximum Number of Hops in Broadcast (set to 0x00 for Maximum Hops in a Broadcast)
            pPacket[15] = (byte)broadcastRadius;
            //Supported Transmission Options (0x01=Disable ACK, 0x20=Enable APS Encryption, 0x40= use extended transimssion timeout for destination)
            pPacket[16] = (byte)XBeeOptionValues.None;
            //pPacket[17]->pPacket[SecondLastByte] = Data To Be Sent
            //pPacket[Last Byte] = Checksum
        }

        /// <summary>
        /// Makes a Byte Array with all the required Parameters and Checksum with the given 64-bit Destination Address and given data
        /// </summary>
        /// <param name="destinationAddress">64-Bit End Device Destination Address</param>
        /// <param name="data">Byte Array of Data to Be Transmit</param>
        /// <returns>Packet with all the required bytes ready to write on the COM Port on which XBee is Connected</returns>
        public byte[] MakePacket(Int64 destinationAddress, byte[] data)
        {
            byte[] tempByteArr = BitConverter.GetBytes(destinationAddress);
            //Insert Destination in the packet
            for (int i = 0; i < tempByteArr.Length; i++)
            {
                pPacket[5 + i] = tempByteArr[(tempByteArr.Length - 1 - i)];
            }
            //Insert DataBytes in the packet
            for (int i = 0; i < data.Length; i++)
            {
                pPacket[17 + i] = data[i];
            }
            //Calculate CheckSum and Append it to the packet
            pPacket[(pPacket.Length - 1)] = CalculateCheksum();
            return pPacket;
        }

        /// <summary>
        /// Function To Calculate Checksum of the Entire Packet
        /// </summary>
        /// <returns>1-Byte Checksum of the Packet to be transmitted</returns>
        private byte CalculateCheksum()
        {
            int ans = 0x00;
            for (int i = 3; i < (pPacket.Length - 1); i++)    //Sum till Second Last packet
            {
                ans += pPacket[i];
            }
            //Discard Upper Bits
            ans = 0xff & ans;
            //Perform 2's Complement
            ans = 0xff - ans;
            return (byte)ans;
        }
    }

    public enum XBeeAPICommandId
    {
        REQUEST_64 = 0x00,
        REQUEST_16 = 0x01,
        AT_COMMAND_REQUEST = 0x08,
        AT_COMMAND_QUEUE_REQUEST = 0x09,
        TRANSMIT_DATA_REQUEST = 0x10,
        EXPLICIT_ADDR_REQUEST = 0x11,
        REMOTE_AT_COMMAND_REQUEST = 0x17,
        CREATE_SOURCE_ROUTE = 0x21,
        RECEIVE_64_RESPONSE = 0x80,
        RECEIVE_16_RESPONSE = 0x81,
        RECEIVE_64_IO_RESPONSE = 0x82,
        RECEIVE_16_IO_RESPONSE = 0x83,
        AT_COMMAND_RESPONSE = 0x88,
        TX_STATUS_RESPONSE = 0x89,
        MODEM_STATUS_RESPONSE = 0x8A,
        TRANSMIT_STATUS_RESPONSE = 0x8B,
        RECEIVE_PACKET_RESPONSE = 0x90,
        EXPLICIT_RX_INDICATOR_RESPONSE = 0x91,
        IO_SAMPLE_RESPONSE = 0x92,
        SENSOR_READ_INDICATOR = 0x94,
        NODE_IDENTIFIER_RESPONSE = 0x95,
        REMOTE_AT_COMMAND_RESPONSE = 0x97,
        FIRMWARE_UPDATE_STATUS = 0xA0,
        ROUTE_RECORD_INDICATOR = 0xA1,
        MANYTOONE_ROUTE_REQUEST_INDICATOR = 0xA3,
        UNKNOWN = 0xFF,
    }

    public enum XBeeOptionValues : byte
    {
        None = 0x00,
        DisableAck = 0x01,
        EnableApsEncryption = 0x20,
        ExtendedTimeout = 0x40
    }

    public enum XBeeFrameID
    {
        DoNotRequestResponse = 0x00,
        RequestResponse = 0x01
    }

    public enum XBeeDestinationNetworkAddress
    {
        UnknownOrBroadcast = 0xFFFE,
        BroadcastToAllRouters = 0xFFFC,
        BroadcastToNonSleepy = 0xFFFD,
        BroadcastToAllIncludingSleepy = 0xFFFF
    }
}
