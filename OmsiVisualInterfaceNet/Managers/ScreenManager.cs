using System.Drawing.Design;

namespace OmsiVisualInterfaceNet.Managers
{
    public class ScreenManager
    {
        private readonly Panel StopScreen;
        private readonly Panel MainScreen;
        private readonly Panel LogoScreen;
        private readonly Panel FuelScreen;
        private readonly Panel PressureScreen;
        private readonly Panel CoolantTemperatureScreen;
        private readonly Dictionary<string, PictureBox> iconPictureBoxes;
        private readonly OmsiManager omsiManager;
        private readonly SerialManager serialManager;

        public System.Windows.Forms.Timer startupTimerTicker;
        private int startupTimerSeconds = 0;
        private bool startupSequenceActive = true;
        private bool buzzerActive = false;
        private bool lastBuzzerState = false;


        private double currentMode = 0.0;
        private double modeGoTo = -1;
        private bool isInitCheck = false;
        private double startupTimer = 0;
        private double backlight = 0;

        private readonly Dictionary<string, string> mainScreenIconsVariableToPictureBox = new()
        {
            //{ "Actia_DISPLAY_ICO_bremsehalte", "ms_brake" },
            { "Actia_DISPLAY_ICO_Retarder", "ms_retarder" },
            //{ "Actia_DISPLAY_ICO_ramp", "ms_ramp" },
            { "Actia_DISPLAY_ICO_DEV", "ms_dev" },
            { "Actia_DISPLAY_ICO_haltewunsch", "ms_busStop" },
            //{ "Actia_DISPLAY_ICO_asr", "ms_asr" },
            { "Actia_DISPLAY_ICO_Secu", "ms_secu" },
            { "Actia_DISPLAY_ICO_AdBlue", "ms_adBlue" },
            { "Actia_DISPLAY_ICO_fuel", "ms_fuel" }
        };

        public ScreenManager(Panel stopScreen, Panel mainScreen, Panel logoScreen,
                            Panel fuelScreen, Panel coolantTempScreen, Panel pressureScreen,
                            Dictionary<string, PictureBox> iconPictureBoxes,
                            OmsiManager omsiManager, SerialManager serialManager)
        {
            StopScreen = stopScreen;
            MainScreen = mainScreen;
            LogoScreen = logoScreen;
            FuelScreen = fuelScreen;
            PressureScreen = pressureScreen;
            CoolantTemperatureScreen = coolantTempScreen;
            this.iconPictureBoxes = iconPictureBoxes;
            this.omsiManager = omsiManager;
            this.serialManager = serialManager;
            buzzerActive = false;
            lastBuzzerState = false;

            startupTimerTicker = new System.Windows.Forms.Timer();
            startupTimerTicker.Interval = 1000; 
            startupTimerTicker.Tick += StartupTimerTicker_Tick; 

        }

        public void RestartStartupSequence()
        {

            startupTimerSeconds = 0;
            startupSequenceActive = true;

            HideAllScreens();

            SetAllIconsVisible(false);

            startupTimerTicker.Start();
        }

        private void StartupTimerTicker_Tick(object? sender, EventArgs e)
        {
            if (!startupSequenceActive)
            {
                startupTimerTicker.Stop();
                startupTimerTicker.Dispose();
                return;
            }

            startupTimerSeconds++;


            if (startupTimerSeconds >= 0.0 && startupTimerSeconds < 3.0)
            {
                HideAllScreens();
                ShowScreen(LogoScreen);
                return;
            }
            else if (startupTimerSeconds > 3.0 && startupTimerSeconds < 7.0)
            {
                ShowScreen(PressureScreen);
                return;
            }
            else if (startupTimerSeconds > 7.0 && startupTimerSeconds < 9.0)
            {
                ShowScreen(FuelScreen);
                return;
            }
            else if (startupTimerSeconds > 9.0 && startupTimerSeconds < 12.0)
            {
                ShowScreen(CoolantTemperatureScreen);
                if(buzzerActive == lastBuzzerState)
                {
                    buzzerActive = true;
                    serialManager.WriteLine($"BUZZER_STARTUP");
                    lastBuzzerState = true;
                }
                buzzerActive = false;
                return;
            }
            else if (startupTimerSeconds > 12.0)
            {
                ShowScreen(StopScreen);
                SetAllIconsVisible(false);
                startupSequenceActive = false;
                return;
            }
        }

        public void Update()
        {    

            /*if (modeGoTo >= 0)
            {
                currentMode = modeGoTo;
                modeGoTo = -1;
            }*/

            //UpdateScreenVisibility();
        }

        public void ChangeMode(bool up)
        {
            var currentMode = omsiManager.CurrentVehicle?.GetVariable("Actia_DISPLAY_mode");

            if (up)
            {
                if (currentMode == 1.0)
                    UpdateScreenVisibility(2.1);
                else if (currentMode == 2.1)
                    UpdateScreenVisibility(2.2);
                else if (currentMode == 2.2)
                    UpdateScreenVisibility(2.3);
                else if (currentMode == 2.3)
                    UpdateScreenVisibility(1.0);
            }
            else
            {
                if (currentMode == 2.3)
                    UpdateScreenVisibility(2.2);
                else if (currentMode == 2.2)
                    UpdateScreenVisibility(2.1);
                else if (currentMode == 2.1)
                    UpdateScreenVisibility(1.0);
            }
        }

        private void GoToMode(double mode)
        {
            modeGoTo = mode;
        }

        private void UpdateScreenVisibility(double currentMode)
        {
            //HideAllScreens();

            if (currentMode == 1.0)
            {
                ShowScreen(MainScreen);
                UpdateMainScreenIcons();
            }
            else
            {
                //SetAllIconsVisible(false);
                if (currentMode == 2.1)
                    ShowScreen(PressureScreen);
                else if (currentMode == 2.2)
                    ShowScreen(FuelScreen);
                else if (currentMode == 2.3)
                    ShowScreen(CoolantTemperatureScreen);
                else if (currentMode == 4.0)
                    ShowScreen(StopScreen);
            }
        }

        private void UpdateMainScreenIcons()
        {
            foreach (var kvp in mainScreenIconsVariableToPictureBox)
            {
                string gameVar = kvp.Key;
                string pbName = kvp.Value;
                if (iconPictureBoxes.TryGetValue(pbName, out var pb))
                {
                    bool isActive = omsiManager.CurrentVehicle?.GetVariable(gameVar) > 0;
                    if (pb.InvokeRequired)
                    {
                        pb.Invoke(new Action(() => pb.Visible = isActive));
                    }
                    else
                    {
                        pb.Visible = isActive;
                    }
                }
            }
        }

        public void UpdateIcon(string icon_name, bool active)
        {

            if ( iconPictureBoxes.TryGetValue(icon_name, out var pb))
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() => pb.Visible = active));
                }
                else
                {
                    pb.Visible = active;
                }
            }
        }

        private void SetAllIconsVisible(bool visible)
        {
            foreach (var pb in iconPictureBoxes.Values)
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() => pb.Visible = visible));
                }
                else
                {
                    pb.Visible = visible;
                }
            }
        }

        private void ShowScreen(Panel screen)
        {
            if (screen.InvokeRequired)
            {
                screen.Invoke(new Action(() => 
                {
                    screen.Show();
                    screen.BringToFront();
                }));
            }
            else
            {
                screen.Show();
                screen.BringToFront();
            }
        }

        public void HideAllScreens()
        {
            MainScreen.Hide();
            StopScreen.Hide();
            LogoScreen.Hide();
            FuelScreen.Hide();
            PressureScreen.Hide();
            CoolantTemperatureScreen.Hide();
            buzzerActive = false;
            lastBuzzerState = false;
        }
    }
}
