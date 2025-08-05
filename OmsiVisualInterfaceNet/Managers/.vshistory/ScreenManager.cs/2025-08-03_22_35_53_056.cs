

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
            { "Actia_DISPLAY_ICO_brake", "ms_brake" },
            { "Actia_DISPLAY_ICO_retarder", "ms_retarder" },
            { "Actia_DISPLAY_ICO_ramp", "ms_ramp" },
            { "Actia_DISPLAY_ICO_DEV", "ms_dev" },
            { "Actia_DISPLAY_ICO_haltewunsch", "ms_busStop" },
            { "Actia_DISPLAY_ICO_asr", "ms_asr" },
            { "Actia_DISPLAY_ICO_SECU", "ms_secu" },
            { "Actia_DISPLAY_ICO_Retarder", "ms_retarderOff" },
            { "Actia_DISPLAY_ICO_AdBlue", "ms_adBlue" },
            { "Actia_DISPLAY_ICO_fuel", "ms_fuel" },
            { "Actia_DISPLAY_ICO_coolant", "ms_coolant" }
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
                SetAllIconsVisible(false);
                return;
            }
            else if (startupTimerSeconds > 12.0)
            {
                ShowScreen(MainScreen);
                SetAllIconsVisible(false);
                startupSequenceActive = false;
                return;
            }
        }

        public void Update()
        {    

            if (modeGoTo >= 0)
            {
                currentMode = modeGoTo;
                modeGoTo = -1;
            }

            UpdateScreenVisibility();
        }

        public void ChangeMode(bool up)
        {
            if (up)
            {
                if (currentMode == 1.0)
                    GoToMode(2.1);
                else if (currentMode == 2.1)
                    GoToMode(2.2);
                else if (currentMode == 2.2)
                    GoToMode(2.3);
                else if (currentMode == 2.3)
                    GoToMode(1.0);
            }
            else
            {
                if (currentMode == 2.3)
                    GoToMode(2.2);
                else if (currentMode == 2.2)
                    GoToMode(2.1);
                else if (currentMode == 2.1)
                    GoToMode(1.0);
            }
        }

        private void GoToMode(double mode)
        {
            modeGoTo = mode;
        }

        private void UpdateScreenVisibility()
        {
            HideAllScreens();

            if (currentMode == 1.0)
            {
                ShowScreen(MainScreen);
                UpdateMainScreenIcons();
            }
            else
            {
                SetAllIconsVisible(false);
                if (currentMode == 2.2)
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
                    pb.Visible = isActive;
                }
            }
        }

        private void SetAllIconsVisible(bool visible)
        {
            foreach (var pb in iconPictureBoxes.Values)
                pb.Visible = visible;
        }

        private void ShowScreen(Panel screen)
        {
            screen.Show();
            screen.BringToFront();
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
