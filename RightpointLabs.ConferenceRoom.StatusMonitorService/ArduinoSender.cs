using System;
using System.Drawing;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using log4net;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    /// <summary>
    /// Eventually, this will check all serial ports on the machine and do some fancy logic to figure out which one to use.
    /// For now, we'll just have it hard-coded :)
    /// </summary>
    public class ArduinoSender
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private object _lockObject = new object();
        private SerialPort _activeConnection;
        private readonly Timer _timer;
        private Color? _lastColor;

        public ArduinoSender()
        {
            _timer = new Timer(ConfirmConnection, null, 0, 15000);
        }

        private void ConfirmConnection(object state)
        {
            lock (_lockObject)
            {
                if (_activeConnection == null || !_activeConnection.IsOpen)
                {
                    try
                    {
                        try
                        {
                            if (_activeConnection != null)
                            {
                                _activeConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.WarnFormat("Exception closing connection: {0}", ex.Message);
                        }
                        _activeConnection = null;
                        var newPort = new SerialPort(Config.SerialPortNameOverride);
                        newPort.DataReceived += ReadData;
                        newPort.Open();
                        _activeConnection = newPort;
                        if (_lastColor.HasValue)
                        {
                            SetColor(_lastColor.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.InfoFormat("Exception opening connection: {0}", ex.Message);
                    }
                }
            }
        }

        private void ReadData(object sender, SerialDataReceivedEventArgs e)
        {
            log.DebugFormat("Reading line");
            var line = ((SerialPort) sender).ReadLine();
            log.DebugFormat("Read line: {0}", line);
        }

        private SerialPort GetActiveConnection()
        {
            lock (_lockObject)
            {
                return _activeConnection;
            }
        }

        public void SetColor(Color color)
        {
            _lastColor = color;
            var cn = GetActiveConnection();
            if (null != cn && cn.IsOpen)
            {
                var rValue = (byte)(color.R * Config.Brightness * Config.RedBrightness);
                var gValue = (byte)(color.G * Config.Brightness * Config.RedBrightness);
                var bValue = (byte)(color.B * Config.Brightness * Config.RedBrightness);
                var rPin = (byte) (Config.RedPin + 32);
                var gPin = (byte) (Config.GreenPin + 32);
                var bPin = (byte) (Config.BluePin + 32);
                var buffer = new byte[]
                {
                    rPin, rValue, gPin, gValue, bPin, bValue
                };
                log.DebugFormat("Writing data: {0}", BitConverter.ToString(buffer));
                cn.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
