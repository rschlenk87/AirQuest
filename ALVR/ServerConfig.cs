﻿using Codeplex.Data;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AirQuest
{
    class ServerConfig
    {
        private static readonly string APP_FILEMAPPING_NAME = "ALVR_DRIVER_FILEMAPPING_0B124897-7730-4B84-AA32-088E9B92851F";

        public static readonly int DEFAULT_SCALE_INDEX = 3; // 100%
        public static readonly int[] supportedScales = { 25, 50, 75, 100, 125, 150, 175, 200 };

        public static readonly int DEFAULT_REFRESHRATE = 72;
        public static readonly int DEFAULT_WIDTH = 2432;
        public static readonly int DEFAULT_HEIGHT = 1344;

        public class ComboBoxCustomItem
        {
            public ComboBoxCustomItem(string s, int val)
            {
                text = s;
                value = val;
            }
            private readonly string text;
            public int value { get; private set; }

            public override string ToString()
            {
                return text;
            }
        }

        // From OpenVR EVRButtonId
        public static readonly ComboBoxCustomItem[] supportedButtons = {
            new ComboBoxCustomItem("None", -1)
            ,new ComboBoxCustomItem("System", 0)
            ,new ComboBoxCustomItem("ApplicationMenu", 1)
            ,new ComboBoxCustomItem("Grip", 2)
            ,new ComboBoxCustomItem("DPad_Left", 5)
            ,new ComboBoxCustomItem("DPad_Up", 6)
            ,new ComboBoxCustomItem("DPad_Right", 7)
            ,new ComboBoxCustomItem("DPad_Down", 8)
            ,new ComboBoxCustomItem("A Button", 9)
            ,new ComboBoxCustomItem("B Button", 11)
            ,new ComboBoxCustomItem("X Button", 13)
            ,new ComboBoxCustomItem("Y Button", 15)
            ,new ComboBoxCustomItem("Trackpad", 39)
            ,new ComboBoxCustomItem("Trigger", 34)
            ,new ComboBoxCustomItem("Shoulder Left", 19)
            ,new ComboBoxCustomItem("Shoulder Right", 20)
            ,new ComboBoxCustomItem("Joystick Left", 21)
            ,new ComboBoxCustomItem("Joystick Right", 24)
            ,new ComboBoxCustomItem("Back", 31)
            ,new ComboBoxCustomItem("Guide", 32)
            ,new ComboBoxCustomItem("Start", 33)
        };
        public static readonly string[] supportedRecenterButton = new string[] {
            "None", "Trigger", "Trackpad click", "Trackpad touch", "Back"
        };
        public static readonly int[] recenterButtonIndex = new int[] {
            -1, 34, 39, 40, 31
        };

        public static readonly ComboBoxCustomItem[] supportedCodecs = {
            new ComboBoxCustomItem("H.264 AVC", 0),
            new ComboBoxCustomItem("H.265 HEVC", 1)
        };

        MemoryMappedFile memoryMappedFile;

        public ServerConfig()
        {
        }

        public static int FindButton(int button)
        {
            for (var i = 0; i < supportedButtons.Length; i++)
            {
                if (supportedButtons[i].value == button)
                {
                    return i;
                }
            }
            return 0;
        }

        public int GetBufferSizeKB()
        {
            int buffer = Properties.Settings.Default.bitrate * 2 + Properties.Settings.Default.bufferOffset;
            if(buffer < 0)
            {
                buffer = 0;
            }
            return buffer;
        }

        public int GetFrameQueueSize(bool suppressFrameDrop)
        {
            return suppressFrameDrop ? 5 : 1;
        }

        public bool Save(DeviceDescriptor device)
        {
            try
            {
                var c = Properties.Settings.Default;
                dynamic driverConfig = new DynamicJson();

                driverConfig.trackingSystemName = "oculus";
                driverConfig.serialNumber = "1WMGH000XX0000";
                driverConfig.modelNumber = "Oculus Rift S";
                driverConfig.manufacturerName = "Oculus";
                driverConfig.renderModelName = "generic_hmd";
                driverConfig.registeredDeviceType = "oculus/1WMGH000XX0000";
                driverConfig.driverVersion = "1.42.0";

                driverConfig.adapterIndex = 0;
                driverConfig.IPD = 0.063;
                driverConfig.secondsFromVsyncToPhotons = 0.005;
                driverConfig.listenPort = 9944;
                driverConfig.listenHost = "0.0.0.0";
                driverConfig.sendingTimeslotUs = 500;
                driverConfig.limitTimeslotPackets = 0;
                driverConfig.controlListenPort = 9944;
                driverConfig.controlListenHost = "127.0.0.1";
                driverConfig.useKeyedMutex = true;
                driverConfig.codec = c.codec; // 0: H264, 1: H265
                driverConfig.encodeBitrateInMBits = c.bitrate;

                if (device == null)
                {
                    driverConfig.refreshRate = DEFAULT_REFRESHRATE;
                    driverConfig.renderWidth = DEFAULT_WIDTH;
                    driverConfig.renderHeight = DEFAULT_HEIGHT;
                    driverConfig.recomendedRenderWidth = DEFAULT_WIDTH;
                    driverConfig.recomendedRenderHeight = DEFAULT_HEIGHT;

                    driverConfig.autoConnectHost = "";
                    driverConfig.autoConnectPort = 0;

                    driverConfig.eyeFov = new double[] { 45, 45, 45, 45, 45, 45, 45, 45 };
                }
                else
                {

                    driverConfig.refreshRate = device.RefreshRates[0] == 0 ? DEFAULT_REFRESHRATE : device.RefreshRates[0];
                    if(c.force60Hz)
                    {
                        driverConfig.refreshRate = 60;
                    }
                    driverConfig.renderWidth = device.DefaultWidth * c.resolutionScale / 100;
                    driverConfig.renderHeight = device.DefaultHeight * c.resolutionScale / 100;
                    driverConfig.recommendedRenderWidth = device.DefaultWidth;
                    driverConfig.recommendedRenderHeight = device.DefaultHeight;

                    driverConfig.autoConnectHost = device.ClientHost;
                    driverConfig.autoConnectPort = device.ClientPort;

                    driverConfig.eyeFov = device.EyeFov;
                }
                driverConfig.disableThrottling = c.disableThrottling;

                driverConfig.enableSound = c.enableSound && c.soundDevice != "";
                driverConfig.soundDevice = c.soundDevice;
                driverConfig.streamMic = c.streamMic;

                driverConfig.debugOutputDir = Utils.GetOutputPath();
                driverConfig.debugLog = c.debugLog;
                driverConfig.debugFrameIndex = false;
                driverConfig.debugFrameOutput = false;
                driverConfig.debugCaptureOutput = c.debugCaptureOutput;
                driverConfig.useKeyedMutex = true;

                driverConfig.clientRecvBufferSize = GetBufferSizeKB() * 1000;
                driverConfig.frameQueueSize = GetFrameQueueSize(c.suppressFrameDrop);

                driverConfig.force60HZ = c.force60Hz;
                driverConfig.force3DOF = c.force3DOF;
                driverConfig.aggressiveKeyframeResend = c.aggressiveKeyframeResend;
                driverConfig.nv12 = c.nv12;

                driverConfig.disableController = c.disableController;



                driverConfig.controllerTrackingSystemName = "oculus";
                driverConfig.controllerSerialNumber = "1WMGH000XX0000_Controller"; //requires _Left & _Right
                driverConfig.controllerModelNumber = "Oculus Rift S"; //requires (Left Controller) & (Right Controller)
                driverConfig.controllerManufacturerName = "Oculus";
                driverConfig.controllerRenderModelNameLeft = "oculus_rifts_controller_left";
                driverConfig.controllerRenderModelNameRight = "oculus_rifts_controller_right";
                driverConfig.controllerRegisteredDeviceType = "oculus/1WMGH000XX0000_Controller"; //requires _Left & _Right
                driverConfig.controllerInputProfilePath = "{oculus}/input/touch_profile.json";
                driverConfig.controllerType = "oculus_touch";


                driverConfig.controllerTriggerMode = c.controllerTriggerMode;
                driverConfig.controllerTrackpadClickMode = c.controllerTrackpadClickMode;
                driverConfig.controllerTrackpadTouchMode = c.controllerTrackpadTouchMode;
                driverConfig.controllerBackMode = c.controllerBackMode;

                // -1=Disabled, other=AirQuest Input id
                driverConfig.controllerRecenterButton = recenterButtonIndex[c.controllerRecenterButton];
                driverConfig.useTrackingReference = c.useTrackingReference;

                driverConfig.enableOffsetPos = c.useOffsetPos;
                driverConfig.offsetPosX = Utils.ParseFloat(c.offsetPosX);
                driverConfig.offsetPosY = Utils.ParseFloat(c.offsetPosY);
                driverConfig.offsetPosZ = Utils.ParseFloat(c.offsetPosZ);

                driverConfig.trackingFrameOffset = Utils.ParseInt(c.trackingFrameOffset);
                driverConfig.controllerPoseOffset = Utils.ParseFloat(c.controllerPoseOffset);

                driverConfig.foveationMode = c.foveationMode;
                driverConfig.foveationStrength = c.foveationStrength / 100f;
                driverConfig.foveationShape = 1.5f;
                driverConfig.foveationVerticalOffset = c.foveationVerticalOffset / 100f;

                driverConfig.enableColorCorrection = c.enableColorCorrection;
                driverConfig.brightness = (float)c.brightness;
                driverConfig.contrast = (float)c.contrast;
                driverConfig.saturation = (float)c.saturation;
                driverConfig.gamma = (float)c.gamma;


                byte[] bytes = Encoding.UTF8.GetBytes(driverConfig.ToString());
                memoryMappedFile = MemoryMappedFile.CreateOrOpen(APP_FILEMAPPING_NAME, sizeof(int) + bytes.Length);

                using (var mappedStream = memoryMappedFile.CreateViewStream())
                {
                    mappedStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
                    mappedStream.Write(bytes, 0, bytes.Length);
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Error on creating filemapping.\r\nPlease check the status of vrserver.exe and retry.");
                return false;
            }
            return true;
        }
    }
}
