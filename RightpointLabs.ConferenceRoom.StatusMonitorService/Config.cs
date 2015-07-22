using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static string RoomAddress
        {
            get { return ConfigurationManager.AppSettings["RoomAddress"]; }
        }

        public static string RedPin
        {
            get { return ConfigurationManager.AppSettings["RedPin"]; }
        }

        public static string GreenPin
        {
            get { return ConfigurationManager.AppSettings["GreenPin"]; }
        }

        public static string BluePin
        {
            get { return ConfigurationManager.AppSettings["BluePin"]; }
        }

    }
}
