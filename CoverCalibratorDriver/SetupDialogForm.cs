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
using System.Threading.Tasks;
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
                Task.Run(RefreshSwitches);
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

            //deviceComboBox.SelectedIndexChanged += new EventHandler(deviceComboBox_SelectedIndexChanged);

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();

            RefreshDevices();
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

        private void RefreshDevices(string device = "")
        {
            deviceComboBox.Items.Clear();
            deviceComboBox.BeginUpdate();

            var index = -1;

            var devices = new Profile().RegisteredDevices("Switch");
                try
                {
                for (int i = 0; i < devices.Count; i++)
                {
                    var d = new SwitchDevice((KeyValuePair)devices[i]);

                    deviceComboBox.Items.Add(d);

                    if (index == -1 && ((d.ProgID == switchDeviceName && device == string.Empty) || d.ProgID == device))
            {
                    index = i;
                }
            }
            }
            finally
            {
                deviceComboBox.EndUpdate();
            deviceComboBox.SelectedIndex = index;
        }
        }

        private void RefreshSwitches()
        {
            // Set the cursor to wait while processing
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    Cursor = Cursors.WaitCursor;
                    deviceComboBox.Enabled = false;
                    propertiesButton.Enabled = false;
                    cmdCancel.Enabled = false;
                    cmdOK.Enabled = false;
                }));
            }

            // Run the main refresh logic on a separate thread

            var switchDevice = new ASCOM.DriverAccess.Switch(SwitchDeviceName);
            if (switchDevice == null) { return; }

            try
            {
                Invoke(new Action(() =>
                {
                    brightnessSwitchComboBox.Items.Clear();
                    brightnessSwitchComboBox.BeginUpdate();
                }));

                switchDevice.Connected = true;

                short selectedIndex = -1;
                for (short i = 0; i < switchDevice.MaxSwitch; i++)
                {
                    if (switchDevice.CanWrite(i) == false) { continue; }

                    var switchItem = new SwitchComboBoxItem(i, switchDevice.GetSwitchName(i));
                    Invoke(new Action(() =>
                    {
                        brightnessSwitchComboBox.Items.Add(switchItem);
                    }));

                    if (i == switchId)
                    {
                        selectedIndex = (short)(brightnessSwitchComboBox.Items.Count - 1);
                    }
                }

                Invoke(new Action(() =>
                {
                    brightnessSwitchComboBox.SelectedIndex = selectedIndex;
                }));
            }
            catch (Exception e)
            {
                Invoke(new Action(() =>
                {
                    var errorToolTip = new System.Windows.Forms.ToolTip() { IsBalloon = true, ShowAlways = true };
                    errorToolTip.Show(string.Empty, deviceComboBox, 5000);
                    errorToolTip.Show($"Error from switch driver: {e.Message}", deviceComboBox);
                }));
            }
            finally
            {
                switchDevice.Connected = false;

                Invoke(new Action(() =>
                {
                    brightnessSwitchComboBox.EndUpdate();
                    brightnessSwitchComboBox.Enabled = brightnessSwitchComboBox.Items.Count > 0;

                    Cursor = Cursors.Default;
                    deviceComboBox.Enabled = true;
                    propertiesButton.Enabled = true;
                    cmdCancel.Enabled = true;
                    SetOkButtonState();
                }));
            }
        }

        private void BrightnessSwitchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switchId = (short) ((SwitchComboBoxItem) brightnessSwitchComboBox.SelectedItem).Item1;
            SetOkButtonState();
        }

        private void deviceComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) { return; }
            
            var c = new Chooser { DeviceType = "Switch" };
            var progId = c.Choose(SwitchDeviceName);

            RefreshDevices(progId);
        }

        private void deviceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchDeviceName = ((SwitchDevice)deviceComboBox.SelectedItem).ProgID;
            propertiesButton.Enabled = deviceComboBox.SelectedIndex != -1;
            brightnessSwitchComboBox.Enabled = false;
            SetOkButtonState();
        }

        private void propertiesButton_Click(object sender, EventArgs e)
        {
            new ASCOM.DriverAccess.Switch(SwitchDeviceName).SetupDialog();
            Task.Run(RefreshSwitches);
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

    public class SwitchComboBoxItem : Tuple<int, string>
    {
        public SwitchComboBoxItem(int item1, string item2) : base(item1, item2) {}

        public override string ToString()
        {
            return Item2;
        }
    }
}