using System;
using System.Net;

namespace LFE.FacialMotionCapture.Extensions {
    public static class StringExtensions {

        public static IPAddress ToIPAddress(this string val) {
            IPAddress ipAddress;
            if(IPAddress.TryParse(val, out ipAddress)) {
                return ipAddress;
            }
            return null;
        }

        public static IPEndPoint ToIPEndPoint(this string val) {
            if(val == null)  {
                return null;
            }

            var parts = val.Split(':');
            if(parts.Length != 2) {
                return null;
            }

            IPAddress ipAddress = parts[0].ToIPAddress();
            if(ipAddress == null) {
                return null;
            }

            int port;
            if(!int.TryParse(parts[1], out port)) {
                return null;
            }

            return new IPEndPoint(ipAddress, port);
        }
    }
}
