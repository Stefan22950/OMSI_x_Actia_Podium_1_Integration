using RJCP.IO.Ports;

using System;
using System.Diagnostics;
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
            //FlushAndResetArduino();
            WaitForArduinoReady();            
            StartReading();

        }

        public async Task WaitForBootSequence()
        {
            // Wait for boot sequence to complete
            await Task.Delay(1500); // Adjust timing as needed
            FlushAndResetArduino();
        }

        public void FlushAndResetArduino()
        {
            // Clear any pending messages
            if (port != null && port.IsOpen)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }
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
                        Debug.WriteLine($"[RX] {input}"); // Log incoming message
                        OnDataReceived?.Invoke(input);
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Serial read error: {ex.Message}");
                }
            }
        }

        public void WriteLine(string message)
        {
            try
            {
                Debug.WriteLine($"[TX] {message}"); // Log outgoing message
                port.WriteLine(message);
                port.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Serial write error: {ex.Message}");
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