using OmsiVisualInterfaceNet.Managers;
using OmsiVisualInterfaceNet.Managers.SolarisIII;
using System.Runtime.InteropServices;
namespace OmsiVisualInterfaceNet.Forms
{
    public partial class SolarisIII12MSobol : Form
    {
        private SolarisIIIOmsiManager omsiManager;
        private SerialManager serialManager;
        private SolarisIIIDashboardManager dashboardManager;
        private SolarisIIIScreenManager screenManager;
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


        public SolarisIII12MSobol()
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
            omsiManager = new SolarisIIIOmsiManager(serialManager, true);
            constantsManager = new ConstantsManager(omsiManager);

            // Build icon picturebox map to pass to screenManager, bus configuration and dynamic manager.
            var iconPictureBoxes = new Dictionary<string, PictureBox>
            {
                { "pb_icon6", pb_icon6 },
                { "pb_icon5", pb_icon5 },
                { "pb_icon1", pb_icon1 },
                { "pb_icon14", pb_icon14 },
                { "pb_icon2", pb_icon2 },
                { "pb_icon7", pb_icon7 },
                { "pb_icon3", pb_icon3 },
                { "pb_icon9", pb_icon9 },
                { "pb_icon10", pb_icon10 },
                { "pb_icon8", pb_icon8 },
                { "pb_icon12", pb_icon12 },
                { "pb_icon13", pb_icon13 },
                { "pb_icon11", pb_icon11 },
                { "pb_icon4", pb_icon4 }
            };


            // Solaris form only has a single main panel in Designer. Pass the same panel as stop/main/logo.
            screenManager = new SolarisIIIScreenManager(
                MainScreen, // mainScreen
                LogoScreen, // logoScreen
                iconPictureBoxes,
                omsiManager,
                serialManager);

            // DashboardManager requires only mainScreen + ms_fuel for Solaris
            dashboardManager = new SolarisIIIDashboardManager(
                this,
                serialManager,
                omsiManager,
                screenManager,
                constantsManager,
                MainScreen,
                pb_fuel,
                pb_adBlue,
                pb_luft_tank1,
                pb_luft_tank2,
                pb_luft_brems1,
                pb_luft_brems2,
                pb_dpf,
                pb_temp,
                pb_door0,
                pb_door1,
                pb_door2,
                pb_door3
            );

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

            // Place the existing MainScreen at 0,0 and hide managed screens
            MainScreen.Location = new Point(0, 0);
            LogoScreen.Location = new Point(0, 0);
            pb_logo.Location = new Point(0, 0);

            pb_fuel.Parent = pb_background;
            pb_adBlue.Parent = pb_fuel;
            pb_luft_tank2.Parent = pb_adBlue;
            pb_luft_tank1.Parent = pb_luft_tank2;
            pb_luft_brems1.Parent = pb_luft_tank1;
            pb_luft_brems2.Parent = pb_luft_brems1;
            pb_dpf.Parent = pb_luft_brems2;
            pb_temp.Parent = pb_dpf;

            pb_fuel.Location = new Point(0, 0);
            pb_adBlue.Location = new Point(0, 0);
            pb_luft_tank2.Location = new Point(0, 0);
            pb_luft_tank1.Location = new Point(0, 0);
            pb_luft_brems1.Location = new Point(0, 0);
            pb_luft_brems2.Location = new Point(0, 0);
            pb_dpf.Location = new Point(0, 0);
            pb_temp.Location = new Point(0, 0);
            pb_icon4.Location = new Point(0, 0);
            pb_icon11.Location = new Point(0, 0);
            pb_icon13.Location = new Point(0, 0);
            pb_icon12.Location = new Point(0, 0);
            pb_icon8.Location = new Point(0, 0);
            pb_icon10.Location = new Point(0, 0);
            pb_icon9.Location = new Point(0, 0);
            pb_icon3.Location = new Point(0, 0);
            pb_icon7.Location = new Point(0, 0);
            pb_icon2.Location = new Point(0, 0);
            pb_icon14.Location = new Point(0, 0);
            pb_icon1.Location = new Point(0, 0);
            pb_icon5.Location = new Point(0, 0);
            pb_icon6.Location = new Point(0, 0);

            pb_icon4.Parent = pb_temp;
            pb_icon11.Parent = pb_icon4;
            pb_icon13.Parent = pb_icon11;
            pb_icon12.Parent = pb_icon13;
            pb_icon8.Parent = pb_icon12;
            pb_icon10.Parent = pb_icon8;
            pb_icon9.Parent = pb_icon10;
            pb_icon3.Parent = pb_icon9;
            pb_icon7.Parent = pb_icon3;
            pb_icon2.Parent = pb_icon7;
            pb_icon14.Parent = pb_icon2;
            pb_icon1.Parent = pb_icon14;
            pb_icon5.Parent = pb_icon1;
            pb_icon6.Parent = pb_icon5;

            //screenManager.HideAllScreens();
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

        private async void SolarisIII12MSobol_Load(object sender, EventArgs e)
        {
            await omsiManager.Initialize();
            await InitializeSerialConnection();

            ForceToForeground();
        }

    }
}