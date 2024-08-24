// © Ethan Chappel
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ASCOM.SwitchAsCoverCalibrator.CoverCalibrator
{
    [ComVisible(false)] // Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        private string switchDeviceName;

        private string SwitchDeviceName
        {
            get => switchDeviceName;
            set
            {
                switchDeviceName = value;
                RefreshSwitches();
            }
        }

        private short switchId { get; set; }

        public SetupDialogForm(TraceLogger tlDriver, string switchDeviceName, short switchId)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            this.switchDeviceName = switchDeviceName;
            this.switchId = switchId;

            deviceComboBox.MouseDown += new MouseEventHandler(deviceComboBox_MouseDown);

            deviceComboBox.SelectedIndexChanged += new EventHandler(deviceComboBox_SelectedIndexChanged);

            RefreshDevices();

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void CmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here and update the state variables with results from the dialogue
            tl.Enabled = chkTrace.Checked;

            // Update the COM port variable if one has been selected
            if (brightnessSwitchComboBox.SelectedItem is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Trace: {chkTrace.Checked}, Brightness Switch: Not selected");
            }
            else // A COM port has been selected
            {
                CoverCalibrator.switchDriverName = SwitchDeviceName;
                CoverCalibrator.switchId = switchId;
                tl.LogMessage("Setup OK", $"New configuration values - Trace: {chkTrace.Checked}, Switch Device: {SwitchDeviceName}");
            }
        }

        private void CmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {

            // Set the trace checkbox
            chkTrace.Checked = tl.Enabled;

            tl.LogMessage("InitUI", $"Set UI controls to Trace: {chkTrace.Checked}"); //, COM Port: {comboBoxComPort.SelectedItem}");
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {
            // Bring the setup dialogue to the front of the screen
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            else
            {
                TopMost = true;
                Focus();
                BringToFront();
                TopMost = false;
            }
        }

        private void RefreshDevices()
        {
            deviceComboBox.Items.Clear();
            deviceComboBox.BeginUpdate();

            var devices = new Profile().RegisteredDevices("Switch");
            foreach (KeyValuePair device in devices)
            {
                try
                {
                    deviceComboBox.Items.Add(new SwitchDevice(device));
                }
                catch (Exception)
                {

                }
            }
            deviceComboBox.EndUpdate();

            int index = -1;
            for (int i = 0; i < deviceComboBox.Items.Count; i++)
            {
                if (((SwitchDevice)deviceComboBox.Items[i]).ProgID == switchDeviceName)
                {
                    index = i;
                    break;
                }
            }

            deviceComboBox.SelectedIndex = index;
        }

        private void RefreshSwitches()
        {
            
            var switchDevice = new ASCOM.DriverAccess.Switch(SwitchDeviceName);
            if (switchDevice != null)
            {
                try
                {
                    brightnessSwitchComboBox.Items.Clear();
                    brightnessSwitchComboBox.BeginUpdate();

                    switchDevice.Connected = true;

                    for (short i = 0; i < switchDevice.MaxSwitch; i += 1)
                    {
                        if (switchDevice.CanWrite(i))
                        {
                            brightnessSwitchComboBox.Items.Add(switchDevice.GetSwitchName(i));
                        }
                    }

                    brightnessSwitchComboBox.SelectedIndex = switchId;
                }
                catch (Exception e)
                {
                    var errorToolTip = new System.Windows.Forms.ToolTip() { IsBalloon = true, ShowAlways = true };
                    errorToolTip.Show(string.Empty, deviceComboBox, 0);
                    errorToolTip.Show($"Error from switch driver: {e.Message} {brightnessSwitchComboBox.Items.Count}", deviceComboBox);
                }
                finally
                {
                    switchDevice.Connected = false;
                    brightnessSwitchComboBox.EndUpdate();

                    var enable = brightnessSwitchComboBox.Items.Count > 0;
                    brightnessSwitchComboBox.Enabled = enable;
                }
            }
        }

        private void brightnessSwitchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switchId = (short)brightnessSwitchComboBox.SelectedIndex;
            SetOkButtonState();
        }

        private void deviceComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Alt) { return; }
            
            var c = new ASCOM.Utilities.Chooser { DeviceType = "Switch" };

            var progId = c.Choose(SwitchDeviceName);

            if (progId == string.Empty) { return; }

            RefreshDevices();
            deviceComboBox.SelectedItem = deviceComboBox.Items.Cast<SwitchDevice>().Where(i => i.ProgID == progId).FirstOrDefault();
        }

        private void deviceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchDeviceName = ((SwitchDevice)deviceComboBox.SelectedItem).ProgID;
            propertiesButton.Enabled = deviceComboBox.SelectedIndex != -1;
            SetOkButtonState();
        }

        private void propertiesButton_Click(object sender, EventArgs e)
        {
            new ASCOM.DriverAccess.Switch(SwitchDeviceName).SetupDialog();
            RefreshSwitches();
        }

        private void SetOkButtonState()
        {
            cmdOK.Enabled = deviceComboBox.SelectedIndex != -1 && brightnessSwitchComboBox.SelectedIndex != -1;
        }
    }

    class SwitchDevice
    {
        private KeyValuePair Device;
        public string ProgID => Device.Key;
        public string Name => Device.Value;

        public SwitchDevice(KeyValuePair device)
        {
            Device = device;
        }

        public override string ToString() { return Name; }
    }
}