// © Ethan Chappel
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

//tabs=4
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM CoverCalibrator driver for SwitchAsCoverCalibrator
//
// Description:	 <To be completed by driver developer>
//
// Implements:	ASCOM CoverCalibrator interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.SwitchAsCoverCalibrator.CoverCalibrator
{
    //
    // Driver's DeviceID is ASCOM.SwitchAsCoverCalibrator.CoverCalibrator
    //
    // The Guid attribute sets the CLSID for ASCOM.SwitchAsCoverCalibrator.CoverCalibrator
    // The ClassInterface/None attribute prevents an empty interface called
    // _SwitchAsCoverCalibrator from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM CoverCalibrator Driver for SwitchAsCoverCalibrator.
    /// </summary>
    [ComVisible(true)]
    [Guid("8046da18-ea4e-4cad-8c7c-dbc2513b4ce6")]
    [ProgId("ASCOM.SwitchAsCoverCalibrator.CoverCalibrator")]
    [ServedClassName("Switch as CoverCalibrator")] // Driver description that appears in the Chooser, customise as required
    [ClassInterface(ClassInterfaceType.None)]
    public class CoverCalibrator : ReferenceCountedObjectBase, ICoverCalibratorV1
    {
        // Constants used for Profile persistence
        internal const string switchDriverNameProfileName = "Switch Driver Name";
        internal const string switchIdProfileName = "Switch ID";
        internal const string switchIdDefault = "-1";
        internal const string traceStateProfileName = "Trace Level";
        internal const string traceStateDefault = "false";

        internal static string driverID; // ASCOM DeviceID (COM ProgID) for this driver, the value is retrieved from the ServedClassName attribute in the class initialiser.
        internal static string driverDescription; // The value is retrieved from the ServedClassName attribute in the class initialiser.
        internal static DriverAccess.Switch device;
        internal static short switchId;
        internal static string switchDriverName;
        internal static int brightness;
        internal static bool connectedState; // variable to hold the connected state
        internal CalibratorStatus calibratorState;
        internal static Util utilities; // Private variable to hold an ASCOM Utilities object
        internal static TraceLogger tl; // Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchAsCoverCalibrator"/> class. Must be public to successfully register for COM.
        /// </summary>
        public CoverCalibrator()
        {
            try
            {
                // Pull the ProgID from the ProgID class attribute.
                Attribute attr = Attribute.GetCustomAttribute(GetType(), typeof(ProgIdAttribute));
                driverID = ((ProgIdAttribute)attr).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

                // Pull the display name from the ServedClassName class attribute.
                attr = Attribute.GetCustomAttribute(GetType(), typeof(ServedClassNameAttribute));
                driverDescription = ((ServedClassNameAttribute)attr).DisplayName ?? "DISPLAY NAME NOT SET!";  // Get the driver description that displays in the ASCOM Chooser from the ServedClassName attribute.

                tl = new TraceLogger("", "ASCOM.SwitchAsCoverCalibrator.CoverCalibrator");
                ReadProfile(); // Read device configuration from the ASCOM Profile store, including the trace state

                tl.LogMessage("CoverCalibrator", "Starting initialisation");
                tl.LogMessage("CoverCalibrator", $"ProgID: {driverID}, Description: {driverDescription}");

                connectedState = false; // Initialise connected to false
                utilities = new Util(); //Initialise util object

                // Implement additional construction here.
                if (switchDriverName != string.Empty)
                {
                    device = new DriverAccess.Switch(switchDriverName);
                }

                tl.LogMessage("CoverCalibrator", "Completed initialisation");
            }
            catch (NotConnectedException ex)
            {
                tl.LogMessageCrLf("CoverCalibrator", $"Initialisation exception: {ex}");
                MessageBox.Show($"{ex.Message}", "Exception creating ASCOM.SwitchAsCoverCalibrator.CoverCalibrator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw ex;
            }
            catch (Exception)
            {

            }

        }

        // PUBLIC COM INTERFACE ICoverCalibratorV1 IMPLEMENTATION

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialogue form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialogue if not connected
            // or call a different dialogue if connected
            if (IsConnected)
                MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl, switchDriverName ?? string.Empty, switchId))
            {
                var result = F.ShowDialog();
                if (result == DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // TODO The optional CommandBlind method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBlind must send the supplied command to the mount and return immediately without waiting for a response

            throw new MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            // TODO The optional CommandBool method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBool must send the supplied command to the mount, wait for a response and parse this to return a True or False value

            // string retString = CommandString(command, raw); // Send the command and wait for the response
            // bool retBool = XXXXXXXXXXXXX; // Parse the returned string and create a boolean True / False value
            // return retBool; // Return the boolean value to the client

            throw new MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // TODO The optional CommandString method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandString must send the supplied command to the mount and wait for a response before returning this to the client

            throw new MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the trace logger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            device.Dispose();
        }

        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    connectedState = true;
                    LogMessage("Connected Set", "Connecting to driver {0}, switch {1}", switchDriverName, switchId);
                    // TODO connect to the device
                    device.Connected = true;
                    calibratorState = CalibratorStatus.Ready;
                }
                else
                {
                    connectedState = false;
                    LogMessage("Connected Set", "Disconnecting from driver {0}, switch {1}", switchDriverName, switchId);
                    // TODO disconnect from the device
                    device.Connected = false;
                    calibratorState = CalibratorStatus.NotReady;
                }
            }
        }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "1");
                return 1;
            }
        }

        public string Name
        {
            get
            {
                string name = "Switch as CoverCalibrator";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ICoverCalibrator Implementation

        /// <summary>
        /// Returns the state of the device cover, if present, otherwise returns "NotPresent"
        /// </summary>
        public CoverStatus CoverState
        {
            get
            {
                tl.LogMessage("CoverState Get", "Not implemented");
                return CoverStatus.NotPresent;
            }
        }

        /// <summary>
        /// Initiates cover opening if a cover is present
        /// </summary>
        public void OpenCover()
        {
            tl.LogMessage("OpenCover", "Not implemented");
            throw new MethodNotImplementedException("OpenCover");
        }

        /// <summary>
        /// Initiates cover closing if a cover is present
        /// </summary>
        public void CloseCover()
        {
            tl.LogMessage("CloseCover", "Not implemented");
            throw new MethodNotImplementedException("CloseCover");
        }

        /// <summary>
        /// Stops any cover movement that may be in progress if a cover is present and cover movement can be interrupted.
        /// </summary>
        public void HaltCover()
        {
            tl.LogMessage("HaltCover", "Not implemented");
            throw new MethodNotImplementedException("HaltCover");
        }

        /// <summary>
        /// Returns the state of the calibration device, if present, otherwise returns "NotPresent"
        /// </summary>
        public CalibratorStatus CalibratorState
        {
            get
            {
                tl.LogMessage("CalibratorState Get", "Not implemented");
                return calibratorState;
            }
        }

        /// <summary>
        /// Returns the current calibrator brightness in the range 0 (completely off) to <see cref="MaxBrightness"/> (fully on)
        /// </summary>
        public int Brightness
        {
            get
            {
                tl.LogMessage("Brightness Get", "Not implemented");
                brightness = (int)device.GetSwitchValue(switchId);
                return brightness;
            }
        }

        /// <summary>
        /// The Brightness value that makes the calibrator deliver its maximum illumination.
        /// </summary>
        public int MaxBrightness
        {
            get
            {
                tl.LogMessage("MaxBrightness Get", "Not implemented");
                return (int) Math.Floor(device.MaxSwitchValue(switchId));
            }
        }

        /// <summary>
        /// Turns the calibrator on at the specified brightness if the device has calibration capability
        /// </summary>
        /// <param name="Brightness"></param>
        public void CalibratorOn(int Brightness)
        {
            tl.LogMessage("CalibratorOn", $"Not implemented. Value set: {Brightness}");
            calibratorState = CalibratorStatus.NotReady;
            device.SetSwitchValue(switchId, Brightness);
            brightness = Brightness;
            calibratorState = CalibratorStatus.Ready;
        }

        /// <summary>
        /// Turns the calibrator off if the device has calibration capability
        /// </summary>
        public void CalibratorOff()
        {
            tl.LogMessage("CalibratorOff", "Not implemented");
            calibratorState = CalibratorStatus.NotReady;
            brightness = (int)device.GetSwitchValue(switchId);
            device.SetSwitchValue(switchId, 0);
            calibratorState = CalibratorStatus.Ready;

        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new Profile())
            {
                P.DeviceType = "CoverCalibrator";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            _ = t.Name; // Just included to remove a compiler informational message that the mandatory type parameter "t" is not used within the member
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            _ = t.Name; // Just included to remove a compiler informational message that the mandatory type parameter "t" is not used within the member
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                switchDriverName = driverProfile.GetValue(driverID, switchDriverNameProfileName, string.Empty, string.Empty);
                switchId = short.Parse(driverProfile.GetValue(driverID, switchIdProfileName, string.Empty, switchIdDefault));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, switchDriverNameProfileName, switchDriverName);
                driverProfile.WriteValue(driverID, switchIdProfileName, switchId.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}
