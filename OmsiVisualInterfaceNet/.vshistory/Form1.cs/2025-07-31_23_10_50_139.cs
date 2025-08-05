using OmsiVisualInterfaceNet.Managers;

using System.Drawing.Drawing2D;

namespace OmsiVisualInterfaceNet
{
    public partial class Form1 : Form
    {
        private OmsiManager omsiManager;
        private SerialManager serialManager;
        private DashboardManager dashboardManager;
        private ScreenManager screenManager;

        private System.Windows.Forms.Timer updateTimer;
        private Panel dimOverlay;

        public Form1()
        {
            InitializeComponent();
            InitializeManagers();
            SetupFormPosition();
            InitializeTimer();
            this.Paint += Form1_Paint;
        }

        private void InitializeManagers()
        {
            serialManager = new SerialManager("COM3", 115200);
            omsiManager = new OmsiManager(serialManager, true);
            dashboardManager = new DashboardManager(serialManager, omsiManager,
                                         StopScreen, MainScreen, LogoScreen, Warning, coolant_temp,fuel,adblue,ms_fuel);
            screenManager = new ScreenManager(StopScreen, MainScreen, LogoScreen, FuelScreen, CoolantTemperatureScreen);

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
            this.ClientSize = new Size(803, 450);
            string targetScreenDeviceName = @"\\.\DISPLAY2";
            Point desiredLocationOnScreen = new Point(710, 449);

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
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }

            StopScreen.Location = new Point(0, 0);
            MainScreen.Location = new Point(0, 0);
            LogoScreen.Location = new Point(0, 0);
            FuelScreen.Location = new Point(0, 0);
            CoolantTemperatureScreen.Location = new Point(0, 0);
            StopScreen.Hide();
            LogoScreen.Hide();
            MainScreen.Hide();
            FuelScreen.Hide();
            CoolantTemperatureScreen.Hide();
            /*dimOverlay.Visible = true;
            SetDimmingLevel(1);*/
        }

        private void InitializeTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 16; // ms
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
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

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            dashboardManager.Update();
            //lightManager.Update();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Flip the form upside down
            e.Graphics.TranslateTransform(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
            e.Graphics.RotateTransform(180);
            e.Graphics.TranslateTransform(-this.ClientSize.Width / 2, -this.ClientSize.Height / 2);
        }

        protected async void Form1_Load(object sender, EventArgs e)
        {
            await omsiManager.Initialize();
            await InitializeSerialConnection();
        }

        private async Task InitializeSerialConnection()
        {
            serialManager.WaitForArduinoReady();
            await serialManager.WaitForBootSequence();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            updateTimer.Stop();
            serialManager.Dispose();
            omsiManager.Dispose();
            base.OnFormClosing(e);
        }
    }
}