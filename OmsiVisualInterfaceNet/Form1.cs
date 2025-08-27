using OmsiVisualInterfaceNet.Managers;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace OmsiVisualInterfaceNet
{
    public partial class Form1 : Form
    {
        private OmsiManager omsiManager;
        private SerialManager serialManager;
        private DashboardManager dashboardManager;
        private ScreenManager screenManager;
        private ConstantsManager constantsManager;

        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.Timer criticalUpdateTimer;
        private Panel dimOverlay;

        // Add these Win32 imports at the top of your class
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Form1()
        {
            InitializeComponent();
            InitializeManagers();
            SetupFormPosition();
            InitializeTimer();

            // Set extended window style to be always on top
            this.TopMost = true;
            ForceToForeground();
        }

        private void ForceToForeground()
        {
            // Force window to top-most position
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

            screenManager = new ScreenManager(StopScreen, MainScreen, LogoScreen, FuelScreen, CoolantTemperatureScreen, PressureScreen, iconPictureBoxes, omsiManager, serialManager);

            dashboardManager = new DashboardManager(this, serialManager, omsiManager, screenManager, constantsManager,
                                         StopScreen, MainScreen, LogoScreen, Warning, coolant_temp, fuel, adblue, pressure_1, pressure_2, ms_fuel);


        }

        public void SetDimmingLevel(int alpha)
        {
            dimOverlay.BackColor = Color.FromArgb(alpha, 0, 0, 0);
            dimOverlay.Visible = alpha > 0;
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

            StopScreen.Location = new Point(0, 0);
            MainScreen.Location = new Point(0, 0);
            LogoScreen.Location = new Point(0, 0);
            FuelScreen.Location = new Point(0, 0);
            PressureScreen.Location = new Point(0, 0);
            CoolantTemperatureScreen.Location = new Point(0, 0);
            screenManager.HideAllScreens();
            this.TopMost = true;
            //screenManager.Update();
            /*dimOverlay.Visible = true;
            SetDimmingLevel(1);*/
        }

        private void InitializeTimer()
        {
            // Critical updates (speed/RPM) at 60Hz
            criticalUpdateTimer = new System.Windows.Forms.Timer();
            criticalUpdateTimer.Interval = 16; // ~60Hz
            criticalUpdateTimer.Tick += CriticalUpdateTimer_Tick;
            criticalUpdateTimer.Start();

            // Non-critical updates at 30Hz
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 32; // ~30Hz
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // Add a new timer specifically for ensuring top-most status
            System.Windows.Forms.Timer topMostTimer = new System.Windows.Forms.Timer();
            topMostTimer.Interval = 1000; // Check every second
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

        /*private void AddDimOverlay()
        {
            dimOverlay = new Panel();
            dimOverlay.BackColor = Color.FromArgb(128, 0, 0, 0); // 128 = 50% opacity, adjust as needed
            dimOverlay.Dock = DockStyle.Fill;
            dimOverlay.Visible = false; // Start hidden
            dimOverlay.BringToFront();
            this.Controls.Add(dimOverlay);
            dimOverlay.BringToFront();
        }*/

        private void CriticalUpdateTimer_Tick(object sender, EventArgs e)
        {
            dashboardManager.UpdateCritical();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            dashboardManager.UpdateNonCritical();
        }

        // Override ShowInTaskbar to prevent the taskbar from showing the window
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }

        protected async void Form1_Load(object sender, EventArgs e)
        {
            await omsiManager.Initialize();
            await InitializeSerialConnection();

            // Add this
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