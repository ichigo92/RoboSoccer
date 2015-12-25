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
    public partial class DlgSSLRefereeSettings : Form
    {
        private Rig rigReference;
        IPAddress address;
        uint port;

        public DlgSSLRefereeSettings(ref Rig rig)
        {
            InitializeComponent();
            rigReference = rig;
            if (rigReference != null)
            {
                if (rigReference.RefereeReceiver != null && rigReference.RefereeReceiver is SSLRefereeReceiver)
                {
                    SSLRefereeReceiver reveiverref = (SSLRefereeReceiver)rigReference.RefereeReceiver;
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
                SSL_Referee pack = (SSL_Referee)rigReference.RefereeReceiver.Receive();
                MessageBoxes.ShowInfo("Packet received. " + pack.ToString(), "Success");
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.ToString());
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (Apply())
            {
                MessageBoxes.ShowInfo("SSL Referee Receiver set. ", "Success");
                this.Close();
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
            SSLRefereeReceiver receiver = new SSLRefereeReceiver();
            receiver.IpAddress = address;
            receiver.Port = port;
            rigReference.RefereeReceiver = receiver;
            try
            {
                rigReference.RefereeReceiver.Connect();
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.Message);
                return false;
            }
            return true;
        }
    }
}
