using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFE.FacialMotionCapture.Models
{
    public class ControllerFrame : ITimelineFrame
    {
        public int Number { get; set; }
        public string ControllerName { get; set; }
        public Vector3? Position { get; set; }
        public Quaternion? Rotation { get; set; }

        public string GetGroupName() {
            return ControllerName;
        }
    }
}
