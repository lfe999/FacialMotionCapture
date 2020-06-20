using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Models;
using LFE.FacialMotionCapture.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LFE.FacialMotionCapture.Devices {

	public class BannaflakFaceCapClient : IFacialCaptureClient {
        public event EventHandler<BlendShapeReceivedEventArgs> BlendShapeReceived;

        private UdpClient _udpHandler;
        private Thread _serverThread;
        private Plugin _plugin;
        private IPEndPoint _ip;


        public BannaflakFaceCapClient(IPEndPoint ip, Plugin plugin)
        {
            _plugin = plugin;
            _ip = ip;
        }

        public IFacialCaptureClient Connect() {
            _udpHandler = new UdpClient(_ip);
            _serverThread = new Thread(() => ListenForMessages(_udpHandler))
            {
                IsBackground = true
            };
            _serverThread.Start();
            return this;
        }

        public IFacialCaptureClient Disconnect() {
            try {
                _serverThread?.Abort();
                _udpHandler?.Close();
            } catch {}
            return this;
        }

        public bool IsConnected() {
            return _serverThread?.IsAlive ?? false;
        }

        private void ListenForMessages(UdpClient client)
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (IsConnected())
            {
                try
                {
                    byte[] receiveBytes = client.Receive(ref remoteIpEndPoint); // Blocks until a message returns on this socket from a remote host.
                    if(BlendShapeReceived != null)
                    {
                        // optimization (5% speed gain): blendshape commands are always a certain length
                        // this makes this server specialized and not general purpose.  Move this logic back
                        // into OnMessageReceived handler to make this general purpose again
                        if(receiveBytes != null && receiveBytes.Length == MessageParser.BLEND_SHAPE_COMMAND_LENGTH)
                        {
                            EmitBlendShapeEvents(receiveBytes);
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.Interrupted)
                    {
                        SuperController.LogError($"Socket exception while receiving data from udp client: {e.Message} code: {e.SocketErrorCode}");
                    }
                }
                catch (ThreadAbortException)
                {
                    // silence this one
                }
                catch (Exception e)
                {
                    SuperController.LogError($"Error receiving data from udp client: {e.GetType()} {e.Message}");
                    SuperController.LogError($"{e.ToString()}");
                }
            }
        }

        private void EmitBlendShapeEvents(byte[] bytes) {
            var parsed = MessageParser.ParseBlendShapeChange(bytes);
            if(!parsed.HasValue) {
                return;
            }

            var shape = ToBlendShape(parsed.Value.Key);
            var value = parsed.Value.Value;

            try {
                BlendShapeReceived.Invoke(this, new BlendShapeReceivedEventArgs(shape, value));
            }
            catch {}
        }

        private BlendShape ToBlendShape(int blendShapeId) {
            return new BlendShape(blendShapeId);
        }
    }

    internal class MessageParser
    {
        public static int BLEND_SHAPE_COMMAND_LENGTH = 16;

        // optimization: sacrifice correctness for guestimating: 1%
        //private static byte[] EXPECTED_ADDRESS = new byte[] { (byte)'/', (byte)'W', byte.MinValue, byte.MinValue };
        private static byte[] EXPECTED_ADDRESS = new byte[] { (byte)'/', (byte)'W' };

        // optimization: sacrifice correctness for guestimating: 1%
        //private static byte[] EXPECTED_TYPES = new byte[] { (byte)',', (byte)'i', (byte)'f', byte.MinValue };
        private static byte[] EXPECTED_TYPES = new byte[] { (byte)'i', (byte)'f' };
        public static KeyValuePair<int, float>? ParseBlendShapeChange(byte[] data)
        {
            // moving this check outside madethings way faster
            //// only looking for blendshape commands -- not a full OSC handler
            //// address(32), types(32), int32, float32
            //if (data.Length != BLEND_SHAPE_COMMAND_LENGTH)
            //{
            //    return null;
            //}

            if (!ByteArrayMatches(data, EXPECTED_ADDRESS, 0))
            {
                return null;
            }

            if (!ByteArrayMatches(data, EXPECTED_TYPES, 5))
            {
                return null;
            }

            // get the blendshape id
            var shapeId = ParseInt32(data, 8);
            var shapeValue = ParseFloat32(data, 12);

            return new KeyValuePair<int, float>(shapeId, shapeValue);
        }

        private static bool ByteArrayMatches(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static int ParseInt32(byte[] data, int startPos)
        {
            const int incrementBy = 4;
            int tempPos = startPos;

            if ((startPos + incrementBy) >= data.Length)
            {
                throw new Exception("Missing binary data for int32 at byte index " + startPos.ToString() + ".");
            }

            if (BitConverter.IsLittleEndian)
            {
                var littleEndianBytes = new byte[4];
                littleEndianBytes[3] = data[tempPos++];
                littleEndianBytes[2] = data[tempPos++];
                littleEndianBytes[1] = data[tempPos++];
                littleEndianBytes[0] = data[tempPos++];

                startPos = 0;
                data = littleEndianBytes;
            }

            return BitConverter.ToInt32(data, startPos);
        }

        private static float ParseFloat32(byte[] data, int startPos)
        {
            const int incrementBy = 4;
            int tempPos = startPos;

            if ((startPos + incrementBy) > data.Length)
            {
                throw new Exception("Missing binary data for float32 at byte index " + startPos.ToString() + ".");
            }

            if (BitConverter.IsLittleEndian)
            {
                var littleEndianBytes = new byte[4];
                littleEndianBytes[3] = data[tempPos++];
                littleEndianBytes[2] = data[tempPos++];
                littleEndianBytes[1] = data[tempPos++];
                littleEndianBytes[0] = data[tempPos++];

                startPos = 0;
                data = littleEndianBytes;
            }
            return BitConverter.ToSingle(data, startPos);
        }

    }
}
