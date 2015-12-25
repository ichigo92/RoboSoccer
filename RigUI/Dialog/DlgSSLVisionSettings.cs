using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SSLRig.Core;
using System.Net;
using SSLRig.RigUI.Common;
using SSLRig.Core.Infrastructure.Communication;
using SSLRig.Core.Data.Packet;

namespace SSLRig.RigUI.Dialog
{
    public partial class DlgSSLVisionSettings : Form
    {
        private Rig rigReference;
        IPAddress address;
        uint port;

        public DlgSSLVisionSettings(ref Rig rig)
        {
            InitializeComponent();
            rigReference = rig;
            if (rigReference != null)
            {
                if (rigReference.VisionReceiver != null && rigReference.VisionReceiver is SSLVisionReceiver)
                {
                    SSLVisionReceiver reveiverref = (SSLVisionReceiver)rigReference.VisionReceiver;
                    txtIp.Text = reveiverref.IpAddress.ToString();
                    txtPort.Text = reveiverref.Port.ToString();
                }
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (!Apply())
                return;
            try
            {
                SSL_WrapperPacket pack = (SSL_WrapperPacket)rigReference.VisionReceiver.Receive();
                MessageBoxes.ShowInfo("Packet received. " + pack.ToString(), "Success");
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.ToString());
            }
        }

        private bool Apply()
        {
            if (!IPAddress.TryParse(txtIp.Text, out address))
            {
                MessageBoxes.ShowError("Unable to cast IP Address, check if the specified text in the Server IP box is a valid IP Address. ");
                return false;
            }
            if (!uint.TryParse(txtPort.Text, out port))
            {
                MessageBoxes.ShowError("Unable to cast Port, check if the specified text in the Multicast Port box is a valid Port. ");
                return false;
            }
            SSLVisionReceiver receiver = new SSLVisionReceiver();
            receiver.IpAddress = address;
            receiver.Port = port;
            rigReference.VisionReceiver = receiver;
            try
            {
                rigReference.VisionReceiver.Connect();
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.Message);
                return false;
            }
            return true;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (Apply())
            {
                MessageBoxes.ShowInfo("SSL Vision Receiver set. ", "Success");
                this.Close();
            }
        }

    }
}
