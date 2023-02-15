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

namespace LFE.FacialMotionCapture.Devices
{

    public class LiveLinkFaceClient : IFacialCaptureClient
    {
        public event EventHandler<BlendShapeReceivedEventArgs> BlendShapeReceived;

        private UdpClient _udpHandler;
        private Thread _serverThread;
        private Plugin _plugin;
        private IPEndPoint _ip;
        public float[] values;

        private string port = "11111";

        // mapping from ARKit to CBlendShape
        // https://developer.apple.com/documentation/arkit/arfaceanchor/blendshapelocation
        int[] mappings = new int[] {13, 7, 9, 11, 5, 15, 17, 14, 8, 10, 12, 6, 16, 18,
         25, 26, 27, 24,
         36, 28, 29, 30, 31, 37, 38, 39, 40, 41, 42, 49, 50, 33, 32, 35, 34, 47, 48, 45, 46,43, 44,
         1, 2, 0, 3, 4,
         19, 20, 21, 22, 23, 51
         };

        public LiveLinkFaceClient(string livelinkport, Plugin plugin)
        {
            _plugin = plugin;
            values = new float[61];
            port = livelinkport;
        }

        public IFacialCaptureClient Connect()
        {
            _udpHandler = new UdpClient(int.Parse(port));
            _serverThread = new Thread(() => ListenForMessages(_udpHandler))
            {
                IsBackground = true
            };
            _serverThread.Start();
            return this;
        }

        public IFacialCaptureClient Disconnect()
        {
            try
            {
                _serverThread?.Abort();
                _udpHandler?.Close();
            }
            catch { }
            return this;
        }

        public bool IsConnected()
        {
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
                    if (receiveBytes.Length < 244)
                    {
                        continue;
                    }

                    IEnumerable<Byte> trimmedBytes = receiveBytes.Skip(Math.Max(0, receiveBytes.Count() - 244));
                    List<List<Byte>> chunkedBytes = trimmedBytes
                        .Select((x, i) => new { Index = i, Value = x })
                        .GroupBy(x => x.Index / 4)
                        .Select(x => x.Select(v => v.Value).ToList())
                        .ToList();

                    foreach (var item in chunkedBytes.Select((value, i) => new { i, value }))
                    {
                        item.value.Reverse();
                        values[item.i] = BitConverter.ToSingle(item.value.ToArray(), 0);
                    }
                    try
                    {
                        for (int i = 0; i < mappings.Length; i++)
                        {
                            if (mappings[i] >= 0)
                            {
                                BlendShapeReceived.Invoke(this, new BlendShapeReceivedEventArgs(ToBlendShape(mappings[i]), values[i]));
                            }
                        }
                    }

                    catch { }
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

        private BlendShape ToBlendShape(int blendShapeId)
        {
            return new BlendShape(blendShapeId);
        }
    }
}
