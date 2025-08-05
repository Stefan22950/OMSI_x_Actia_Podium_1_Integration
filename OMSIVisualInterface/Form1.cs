using System;
using System.Windows.Forms;

using OmsiHook;


namespace OMSIVisualInterface
{
    public partial class Form1 : Form
    {
        private OmsiHook.OmsiHook omsi;
        private OmsiRoadVehicleInst? playerVehicle;
        private Timer updateTimer;

        public Form1()
        {
            InitializeComponent();
            this.ClientSize = new System.Drawing.Size(320, 240);

            omsi = new OmsiHook.OmsiHook();
            omsi.AttachToOMSI().Wait();
            omsi.OnActiveVehicleChanged += (_, inst) => playerVehicle = inst;

            playerVehicle = omsi.Globals.PlayerVehicle;

            updateTimer = new Timer();
            updateTimer.Interval = 100; // ms
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            //if (playerVehicle == null) return;

            var rpm = Convert.ToInt32(playerVehicle.GetVariable("engine_n"));
            var speed = Convert.ToInt32(playerVehicle.Tacho);

            // Update your UI controls here
            labelRpm.Text = $"RPM: {rpm}";
            labelSpeed.Text = $"Speed: {speed}";

            // You can add more graphics, charts, or custom drawing here
        }
    }
}
