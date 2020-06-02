using System;
using System.Collections.Generic;

namespace LFE.FacialMotionCapture.Devices {
    public interface IFacialCaptureClient {
        IFacialCaptureClient Connect();
        IFacialCaptureClient Disconnect();
        bool IsConnected();
        event EventHandler<BlendShapeReceivedEventArgs> BlendShapeReceived;
    }
}
