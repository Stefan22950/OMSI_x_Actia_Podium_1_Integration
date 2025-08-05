using RJCP.IO.Ports;
using System;
using System.Threading;

namespace OmsiVisualInterfaceNet
{
    public class SerialManager : IDisposable
    {
        private readonly SerialPortStream port;
        private Thread serialReadThread;
        private volatile bool running;

        public event Action<string> OnDataReceived;

        public SerialManager(string portName, int baudRate)
        {
            port = new SerialPortStream(portName, baudRate);
            port.Close();
            port.Open();
            FlushAndResetArduino();
            WaitForArduinoReady();            
            StartReading();

        }

        public void FlushAndResetArduino()
        {
            // Flush input/output buffers
            port.DiscardInBuffer();
            port.DiscardOutBuffer();

            // Toggle DTR to reset Arduino
            port.DtrEnable = false;
            Thread.Sleep(250);
            port.DtrEnable = true;
            Thread.Sleep(250);
        }

        public void WaitForArduinoReady(string readyMessage = "=== SYSTEM BOOT ===", int timeoutMs = 3000)
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                if (port.BytesToRead > 0)
                {
                    string line = port.ReadLine().Trim();
                    if (line == readyMessage)
                        break;
                }
                Thread.Sleep(10);
            }
        }

        private void StartReading()
        {
            running = true;
            serialReadThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "SerialPort Read Thread"
            };
            serialReadThread.Start();
        }

        private void ReadLoop()
        {
            while (running)
            {
                try
                {
                    if (port.BytesToRead > 0)
                    {
                        string input = port.ReadLine().Trim();
                        OnDataReceived?.Invoke(input);
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Serial read error: {ex.Message}");
                }
            }
        }

        public void WriteLine(string message)
        {
            try
            {
                port.WriteLine(message);
                port.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial write error: {ex.Message}");
            }
        }

        public void Stop()
        {
            running = false;
            serialReadThread?.Join(1000);
            port?.Close();
        }

        public void Dispose()
        {
            Stop();
            port?.Dispose();
        }
    }
}