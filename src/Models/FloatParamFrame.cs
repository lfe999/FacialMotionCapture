using System.Collections.Generic;
using System.Linq;

namespace LFE.FacialMotionCapture.Models
{
    public class FloatParamFrame : ITimelineFrame {
        public int Number { get; set; }
        public string StorableName { get; set; }
        public string Name { get; set; }
        public float Value { get; set; }

        public string GetGroupName() {
            return $"{StorableName}_{Name}";
        }
    }
}
