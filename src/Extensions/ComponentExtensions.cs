using UnityEngine;
using UnityEngine.UI;

namespace LFE.FacialMotionCapture.Extensions{
    public static class ComponentExtensions {
        /// <summary>
        /// (extension) Forces the height of the UI element
        /// </summary>
        public static void SetLayoutHeight(this Component component, float height)
        {
            var layoutElement = component.GetComponent<LayoutElement>();
            if(layoutElement != null) {
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = height;
            }
        }
    }
}
