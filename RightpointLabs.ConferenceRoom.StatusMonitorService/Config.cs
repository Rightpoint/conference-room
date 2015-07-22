using System;
using System.Configuration;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    public static class Config
    {
        public static string SerialPortNameOverride
        {
            get { return ConfigurationManager.AppSettings["SerialPortNameOverride"]; }
        }

        public static string ApiServer
        {
            get { return ConfigurationManager.AppSettings["ApiServer"]; }
        }

        public static string SignalRServer
        {
            get { return ConfigurationManager.AppSettings["SignalRServer"]; }
        }

        public static string RoomAddress
        {
            get { return ConfigurationManager.AppSettings["RoomAddress"]; }
        }

        public static TimeSpan StatusInterval
        {
            get { return TimeSpan.Parse(ConfigurationManager.AppSettings["StatusInterval"] ?? "00:05:00"); }
        }

        public static int RedPin
        {
            get { return int.Parse(ConfigurationManager.AppSettings["RedPin"]); }
        }

        public static int GreenPin
        {
            get { return int.Parse(ConfigurationManager.AppSettings["GreenPin"]); }
        }

        public static int BluePin
        {
            get { return int.Parse(ConfigurationManager.AppSettings["BluePin"]); }
        }

        public static double Brightness
        {
            get { return double.Parse(ConfigurationManager.AppSettings["Brightness"] ?? "1"); }
        }

        public static double RedBrightness
        {
            get { return double.Parse(ConfigurationManager.AppSettings["RedBrightness"] ?? "1"); }
        }

        public static double GreenBrightness
        {
            get { return double.Parse(ConfigurationManager.AppSettings["GreenBrightness"] ?? "1"); }
        }

        public static double BlueBrightness
        {
            get { return double.Parse(ConfigurationManager.AppSettings["BlueBrightness"] ?? "1"); }
        }
    }
}
