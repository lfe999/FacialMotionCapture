using LFE.FacialMotionCapture.Models;
using System;
using System.Diagnostics;

namespace LFE.FacialMotionCapture.Devices {
    public class BlendShapeReceivedEventArgs : EventArgs {
        public BlendShape Shape { get; private set; }
        public float Value { get; private set; }

        public BlendShapeReceivedEventArgs(BlendShape shape, float value) {
            Shape = shape;
            Value = value;
        }
    }
}
