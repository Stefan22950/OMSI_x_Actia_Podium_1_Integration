

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


        private double currentMode = 1.0;
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
                            OmsiManager omsiManager)
        {
            StopScreen = stopScreen;
            MainScreen = mainScreen;
            LogoScreen = logoScreen;
            FuelScreen = fuelScreen;
            PressureScreen = pressureScreen;
            CoolantTemperatureScreen = coolantTempScreen;
            this.iconPictureBoxes = iconPictureBoxes;
            this.omsiManager = omsiManager;

        }

        public void Update(double deltaTime)
        {
            startupTimer += deltaTime;
            if(startupTimer ==1.0)
            {
                HideAllScreens();
            }
            else if (startupTimer > 1.0 && startupTimer < 3.0)
            {
                ShowScreen(LogoScreen);
                SetAllIconsVisible(false);
                return;
            }
            else if (startupTimer > 3.0 && startupTimer < 5.0)
            {
                ShowScreen(PressureScreen);
                SetAllIconsVisible(false);
                return;
            }
            else if (startupTimer > 5.0 && startupTimer < 7.0)
            {
                ShowScreen(FuelScreen);
                SetAllIconsVisible(false);
                return;
            }
            else if (startupTimer > 7.0 && startupTimer < 10.0)
            {
                ShowScreen(CoolantTemperatureScreen);
                SetAllIconsVisible(false);
                return;
            }
            else if (startupTimer > 10.0)
            {
                ShowScreen(StopScreen);
                SetAllIconsVisible(false);
                return;
            }

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

        private void HideAllScreens()
        {
            MainScreen.Hide();
            StopScreen.Hide();
            LogoScreen.Hide();
            FuelScreen.Hide();
            PressureScreen.Hide();
            CoolantTemperatureScreen.Hide();
        }
    }
}
