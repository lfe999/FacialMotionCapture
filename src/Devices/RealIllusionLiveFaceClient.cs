using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LFE.FacialMotionCapture.Devices {

	public class RealIllusionLiveFaceClient : IFacialCaptureClient {

        public event EventHandler<BlendShapeReceivedEventArgs> BlendShapeReceived;

        private IPEndPoint _ipEndPoint;
        private Socket _socket;
        private Thread _listenerThread;
        private Plugin _plugin;

        private const int TCP_PORT = 999;
        private const int TIMEOUT_MILLISECONDS = 1000;

        public RealIllusionLiveFaceClient(string ip, Plugin plugin)
        {
            _plugin = plugin;
            var ipAddress = IPAddress.Parse(ip);
            _ipEndPoint = new IPEndPoint(ipAddress, TCP_PORT);
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        public IFacialCaptureClient Connect() {
            // This is easier
            //   _socket.Connect(_ipEndPoint)
            // but this lets us control the connection timeout (connected host has failed to respond)
            var connectionResult = _socket.BeginConnect(_ipEndPoint, null, null);
            var waited = connectionResult.AsyncWaitHandle.WaitOne(TIMEOUT_MILLISECONDS, true);
            if(_socket.Connected) {
                _socket.EndConnect(connectionResult);
            }
            else {
                Disconnect();
                throw new ApplicationException("connected host has failed to respond");
            }

            // send header (logo) 512x132 px
            string iconPath = _plugin.GetPluginPath() + "Devices/VamLogo.png";
            byte[] icon = MVR.FileManagementSecure.FileManagerSecure.ReadAllBytes(iconPath);
            byte[] padding = new byte[icon.Length % 1024];
            byte[] iconSize = BitConverter.GetBytes((int)(icon.Length + padding.Length));

            _socket.Send(iconSize);
            _socket.Send(new byte[] { 0, 0, 0, 0 } );
            _socket.Send(icon);
            _socket.Send(padding);

            // start listening for face information in a thread
            _listenerThread = new Thread(() => {
                string leftovers = "";
                byte[] header = new byte[8];
                byte[] buffer = new byte[65535];

                while(true) {
                    try {
                        if(_socket.Connected) {
                            // read header message (how long is the data message?)
                            int headerBytesRead = _socket.Receive(header, 0, 8, 0);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(header);
                            int expectedDataLength = BitConverter.ToInt32(header, 0);

                            // read data message (fully)
                            if(expectedDataLength > 0) {
                                int totalReceived = 0;
                                while(totalReceived < expectedDataLength) {
                                    int bytesToAskFor = expectedDataLength - totalReceived;
                                    int bytesReceived = _socket.Receive(buffer, 0, bytesToAskFor, 0);
                                    string got = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                                    if(string.IsNullOrEmpty(leftovers)) {
                                        leftovers = EmitBlendshapeEvents(got);
                                    }
                                    else {
                                        leftovers = EmitBlendshapeEvents(leftovers + got);
                                    }

                                    totalReceived += bytesReceived;
                                }
                            }
                        }
                    }
                    catch(ThreadAbortException) {
                        break;
                    }
                    catch(Exception e) {
                        SuperController.LogError(e.ToString(), false);
                        throw e;
                    }
                }
                SuperController.LogMessage("Not listening to facial data anymore");
            });

            try {
                _listenerThread.Start();
            }
            catch(Exception e) {
                SuperController.LogError(e.ToString(), false);
            }

            return this;
        }

        public IFacialCaptureClient Disconnect() {
            try {
                _listenerThread?.Abort();
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
            } catch {}

            return this;
        }

        public bool IsConnected() {
            return _socket != null && _socket.Connected;
        }

        private string EmitBlendshapeEvents(string rawData) {
            if(BlendShapeReceived != null && IsConnected()) {
                // ex: "shape : value, shape2 : value 3, shap"
                var items = rawData.Split(',');
                var completePairs = items.Length > 1 ? items.Take(items.Length - 1) : new List<string>();

                foreach(var item in completePairs) {
                    var parts = item.Split(':');
                    if(parts.Length == 2) {
                        try {
                            var shape = Map(parts[0].Trim()); // todo: consider normalizing shape name
                            if(shape != null) {
                                var shapeValue = float.Parse(parts[1].Trim()) / 100;
                                BlendShapeReceived.Invoke(this, new BlendShapeReceivedEventArgs(shape, shapeValue));
                            }
                        }
                        catch { }
                    }
                    else {
                        SuperController.LogError($"parse error: '{item}' does not look like k/v pair", false);
                    }
                }

                return items.LastOrDefault() ?? String.Empty;
            }

            return String.Empty;
        }

        private BlendShape Map(string shape) {
            switch(shape) {
                case "browDown_L":
                    return new BlendShape(CBlendShape.BROW_DOWN_LEFT);
                case "browDown_R":
                    return new BlendShape(CBlendShape.BROW_DOWN_RIGHT);
                case "browInnerUp":
                    return new BlendShape(CBlendShape.BROW_INNER_UP);
                case "browOuterUp_L":
                    return new BlendShape(CBlendShape.BROW_OUTER_UP_LEFT);
                case "browOuterUp_R":
                    return new BlendShape(CBlendShape.BROW_OUTER_UP_RIGHT);
                case "cheekPuff":
                    return new BlendShape(CBlendShape.CHEEK_PUFF);
                case "cheekSquint_L":
                    return new BlendShape(CBlendShape.CHEEK_SQUINT_LEFT);
                case "cheekSquint_R":
                    return new BlendShape(CBlendShape.CHEEK_SQUINT_RIGHT);
                case "eyeBlink_L":
                    return new BlendShape(CBlendShape.EYE_BLINK_LEFT);
                case "eyeBlink_R":
                    return new BlendShape(CBlendShape.EYE_BLINK_RIGHT);
                case "eyeLookDown_L":
                    return new BlendShape(CBlendShape.EYE_LOOK_DOWN_LEFT);
                case "eyeLookDown_R":
                    return new BlendShape(CBlendShape.EYE_LOOK_DOWN_RIGHT);
                case "eyeLookIn_L":
                    return new BlendShape(CBlendShape.EYE_LOOK_IN_LEFT);
                case "eyeLookIn_R":
                    return new BlendShape(CBlendShape.EYE_LOOK_IN_RIGHT);
                case "eyeLookOut_L":
                    return new BlendShape(CBlendShape.EYE_LOOK_OUT_LEFT);
                case "eyeLookOut_R":
                    return new BlendShape(CBlendShape.EYE_LOOK_OUT_RIGHT);
                case "eyeLookUp_L":
                    return new BlendShape(CBlendShape.EYE_LOOK_UP_LEFT);
                case "eyeLookUp_R":
                    return new BlendShape(CBlendShape.EYE_LOOK_UP_RIGHT);
                case "eyeSquint_L":
                    return new BlendShape(CBlendShape.EYE_SQUINT_LEFT);
                case "eyeSquint_R":
                    return new BlendShape(CBlendShape.EYE_SQUINT_RIGHT);
                case "eyeWide_L":
                    return new BlendShape(CBlendShape.EYE_WIDE_LEFT);
                case "eyeWide_R":
                    return new BlendShape(CBlendShape.EYE_WIDE_RIGHT);
                case "head_Down":
                    // not supported
                    break;
                case "head_Left":
                    // not supported
                    break;
                case "head_LeftTilt":
                    // not supported
                    break;
                case "HeadPosX":
                case "HeadPosY":
                case "HeadPosZ":
                    // not supported
                    break;
                case "head_Right":
                    // not supported
                    break;
                case "head_RightTilt":
                    // not supported
                    break;
                case "head_Up":
                    // not supported
                    break;
                case "jawForward":
                    return new BlendShape(CBlendShape.JAW_FORWARD);
                case "jawLeft":
                    return new BlendShape(CBlendShape.JAW_LEFT);
                case "jawOpen":
                    return new BlendShape(CBlendShape.JAW_OPEN);
                case "jawRight":
                    return new BlendShape(CBlendShape.JAW_RIGHT);
                case "mouthClose":
                    return new BlendShape(CBlendShape.MOUTH_CLOSE);
                case "mouthDimple_L":
                    return new BlendShape(CBlendShape.MOUTH_DIMPLE_LEFT);
                case "mouthDimple_R":
                    return new BlendShape(CBlendShape.MOUTH_DIMPLE_RIGHT);
                case "mouthFrown_L":
                    return new BlendShape(CBlendShape.MOUTH_FROWN_LEFT);
                case "mouthFrown_R":
                    return new BlendShape(CBlendShape.MOUTH_FROWN_RIGHT);
                case "mouthFunnel":
                    return new BlendShape(CBlendShape.MOUTH_FUNNEL);
                case "mouthLeft":
                    return new BlendShape(CBlendShape.MOUTH_LEFT);
                case "mouthLowerDown_L":
                    return new BlendShape(CBlendShape.MOUTH_LOWER_DOWN_LEFT);
                case "mouthLowerDown_R":
                    return new BlendShape(CBlendShape.MOUTH_LOWER_DOWN_RIGHT);
                case "mouthPress_L":
                    return new BlendShape(CBlendShape.MOUTH_PRESS_LEFT);
                case "mouthPress_R":
                    return new BlendShape(CBlendShape.MOUTH_PRESS_RIGHT);
                case "mouthPucker":
                    return new BlendShape(CBlendShape.MOUTH_PUCKER);
                case "mouthRight":
                    return new BlendShape(CBlendShape.MOUTH_RIGHT);
                case "mouthRollLower":
                    return new BlendShape(CBlendShape.MOUTH_ROLL_LOWER);
                case "mouthRollUpper":
                    return new BlendShape(CBlendShape.MOUTH_ROLL_UPPER);
                case "mouthShrugLower":
                    return new BlendShape(CBlendShape.MOUTH_SHRUG_LOWER);
                case "mouthShrugUpper":
                    return new BlendShape(CBlendShape.MOUTH_SHRUG_UPPER);
                case "mouthSmile_L":
                    return new BlendShape(CBlendShape.MOUTH_SMILE_LEFT);
                case "mouthSmile_R":
                    return new BlendShape(CBlendShape.MOUTH_SMILE_RIGHT);
                case "mouthStretch_L":
                    return new BlendShape(CBlendShape.MOUTH_STRETCH_LEFT);
                case "mouthStretch_R":
                    return new BlendShape(CBlendShape.MOUTH_STRETCH_RIGHT);
                case "mouthUpperUp_L":
                    return new BlendShape(CBlendShape.MOUTH_UPPER_LEFT);
                case "mouthUpperUp_R":
                    return new BlendShape(CBlendShape.MOUTH_UPPER_RIGHT);
                case "noseSneer_L":
                    return new BlendShape(CBlendShape.NOSE_SNEER_LEFT);
                case "noseSneer_R":
                    return new BlendShape(CBlendShape.NOSE_SNEER_RIGHT);
                case "tongueOut":
                    return new BlendShape(CBlendShape.MOUTH_TONGUE_OUT);
                default:
                    SuperController.LogMessage($"unknown shape '{shape}'", false);
                    return null;
            }
            return null;
        }
    }
}
