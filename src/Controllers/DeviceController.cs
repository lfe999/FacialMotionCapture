using LFE.FacialMotionCapture.Main;
using LFE.FacialMotionCapture.Devices;
using System.Collections.Generic;

namespace LFE.FacialMotionCapture.Controllers {
    public class DeviceController {

        private Dictionary<int, BlendShapeReceivedEventArgs> _shapeEventsForFrame = new Dictionary<int, BlendShapeReceivedEventArgs>();

        public Plugin Plugin { get; private set; }
        public IFacialCaptureClient Client { get; private set; }
        public DeviceController(Plugin plugin, IFacialCaptureClient client = null)
        {
            Plugin = plugin;
            Client = client;
            Init();
        }

        private void Init() {
            if(Client != null) {
                Client.BlendShapeReceived += HandleBlendShapeReceived;
            }
        }

        private void HandleBlendShapeReceived(object sender, BlendShapeReceivedEventArgs args) {
            lock(_shapeEventsForFrame) {
                _shapeEventsForFrame[args.Shape.Id] = args;
            }
        }

        public bool IsConnected() {
            if(Client != null && Client.IsConnected()) {
                return true;
            }
            return false;
        }

        public void Connect() {
            Client?.Connect();
        }

        public void Disconnect() {
            Client?.Disconnect();
        }

        public void Destroy() {
            Disconnect();
            if(Client != null) {
                Client.BlendShapeReceived -= HandleBlendShapeReceived;
            }
            Client = null;
        }

        public Dictionary<int, BlendShapeReceivedEventArgs> GetChanges() {
            Dictionary<int, BlendShapeReceivedEventArgs> changes = new Dictionary<int, BlendShapeReceivedEventArgs>();
            lock(_shapeEventsForFrame) {
                changes = new Dictionary<int, BlendShapeReceivedEventArgs>(_shapeEventsForFrame);
                _shapeEventsForFrame.Clear();
            }
            return changes;
        }
    }
}
