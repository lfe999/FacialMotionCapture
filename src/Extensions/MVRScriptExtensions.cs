
using System;

namespace LFE.FacialMotionCapture.Extensions {
    public static class MVRScriptExtensions {
        public static string GetPluginPath(this MVRScript plugin) {
            string id = plugin.name.Substring(0, plugin.name.IndexOf('_'));
            string filename = plugin.manager.GetJSON()["plugins"][id].Value;
            string path = filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            return path;
        }
    }
}
