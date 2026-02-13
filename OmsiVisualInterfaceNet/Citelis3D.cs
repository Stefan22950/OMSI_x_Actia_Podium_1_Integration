using OmsiVisualInterfaceNet.Managers;
using OmsiVisualInterfaceNet.Managers.SolarisIII;

using System.Runtime.InteropServices;

namespace OmsiVisualInterfaceNet
{
    public partial class Citelis3D : Form
    {
        private OmsiManager omsiManager;
        private SerialManager serialManager;
        private DashboardManager dashboardManager;
        private ScreenManager screenManager;
        private ConstantsManager constantsManager;

        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.Timer criticalUpdateTimer;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Citelis3D()
        {
            InitializeComponent();
            InitializeManagers();
            SetupFormPosition();
            InitializeTimer();

            this.TopMost = true;
            ForceToForeground();
        }

        private void ForceToForeground()
        {
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }

        private void InitializeManagers()
        {
            serialManager = new SerialManager("COM3", 115200);
            omsiManager = new OmsiManager(serialManager, true);
            constantsManager = new ConstantsManager(omsiManager);

            var iconPictureBoxes = new Dictionary<string, PictureBox>
            {
                { "ms_brake", ms_brake },
                { "ms_retarder", ms_retarder },
                { "ms_ramp", ms_ramp },
                { "ms_dev", ms_dev },
                { "ms_busStop", ms_busStop },
                { "ms_asr", ms_asr },
                { "ms_secu", ms_secu },
                { "ms_retarderOff", ms_retarderOff },
                { "ms_adBlue", ms_adBlue },
                { "ms_fuel", ms_fuel },
                { "ms_coolant", ms_coolant },
                { "door1_halfClosed", door1_halfClosed },
                { "door1_halfOpened", door1_halfOpened },
                { "door1_closed", door1_closed },
                { "door1_opened", door1_opened },
                { "door2_closed", door2_closed },
                { "door2_opened", door2_opened },
                { "door3_closed", door3_closed },
                { "door3_opened", door3_opened },
                { "backWheel_block", backWheel_block },
                { "frontWheel_block", frontWheel_block }
            };

            // Use original ScreenManager constructor (panels + icon map + managers)
            screenManager = new ScreenManager(StopScreen, MainScreen, LogoScreen, FuelScreen, CoolantTemperatureScreen, PressureScreen, iconPictureBoxes, omsiManager, serialManager);

            // Use original DashboardManager constructor signature
            dashboardManager = new DashboardManager(this, serialManager, omsiManager, screenManager, constantsManager,
                                         StopScreen, MainScreen, LogoScreen, Warning, coolant_temp, fuel, adblue, pressure_1, pressure_2, ms_fuel);
        }

        private void SetupFormPosition()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ClientSize = new Size(402, 301);
            string targetScreenDeviceName = @"\\.\DISPLAY2";
            Point desiredLocationOnScreen = new Point(354, 299);

            Screen? targetScreen = Screen.AllScreens
                .FirstOrDefault(s => s.DeviceName.Equals(targetScreenDeviceName, StringComparison.OrdinalIgnoreCase));

            if (targetScreen != null)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(
                    targetScreen.Bounds.X + desiredLocationOnScreen.X,
                    targetScreen.Bounds.Y + desiredLocationOnScreen.Y
                );
            }

            // Ensure each screen panel is positioned at (0,0) and hide them
            StopScreen.Location = new Point(0, 0);
            MainScreen.Location = new Point(0, 0);
            LogoScreen.Location = new Point(0, 0);
            FuelScreen.Location = new Point(0, 0);
            PressureScreen.Location = new Point(0, 0);
            CoolantTemperatureScreen.Location = new Point(0, 0);
            screenManager.HideAllScreens();
            this.TopMost = true;
        }

        private void InitializeTimer()
        {
            criticalUpdateTimer = new System.Windows.Forms.Timer();
            criticalUpdateTimer.Interval = 16;
            criticalUpdateTimer.Tick += CriticalUpdateTimer_Tick;
            criticalUpdateTimer.Start();

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 32;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            System.Windows.Forms.Timer topMostTimer = new System.Windows.Forms.Timer();
            topMostTimer.Interval = 1000;
            topMostTimer.Tick += (s, e) =>
            {
                if (!this.TopMost)
                {
                    this.TopMost = true;
                    ForceToForeground();
                }
            };
            topMostTimer.Start();
        }

        private void CriticalUpdateTimer_Tick(object sender, EventArgs e)
        {
            dashboardManager.UpdateCritical();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            dashboardManager.UpdateNonCritical();
        }

        protected async void Form1_Load(object sender, EventArgs e)
        {
            await omsiManager.Initialize();
            await InitializeSerialConnection();

            ForceToForeground();
        }

        private async Task InitializeSerialConnection()
        {
            serialManager.WaitForArduinoReady();
            await serialManager.WaitForBootSequence();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            updateTimer.Stop();
            criticalUpdateTimer.Stop();
            serialManager.Dispose();
            omsiManager.Dispose();
            base.OnFormClosing(e);
        }
    }
}