

namespace OmsiVisualInterfaceNet.Managers
{
    public class ScreenManager
    {
        private readonly Panel StopScreen;
        private readonly Panel MainScreen;
        private readonly Panel LogoScreen;
        private readonly Panel FuelScreen;
        private readonly Panel CoolantTemperatureScreen;

        private double currentMode = 1.0;
        private double modeGoTo = -1;
        private bool isInitCheck = false;
        private double startupTimer = 0;
        private double backlight = 0;

        public ScreenManager(Panel stopScreen, Panel mainScreen, Panel logoScreen,
                            Panel fuelScreen, Panel coolantTempScreen)
        {
            StopScreen = stopScreen;
            MainScreen = mainScreen;
            LogoScreen = logoScreen;
            FuelScreen = fuelScreen;
            CoolantTemperatureScreen = coolantTempScreen;
        }

        public void Startup(double deltaTime)
        {
            startupTimer += deltaTime;

            // Handle startup sequence
            if (startupTimer < 3.0)
            {
                ShowScreen(LogoScreen);
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
                ShowScreen(MainScreen);
            else if (currentMode == 2.2)
                ShowScreen(FuelScreen);
            else if (currentMode == 2.3)
                ShowScreen(CoolantTemperatureScreen);
            else if (currentMode == 4.0)
                ShowScreen(StopScreen);
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
            CoolantTemperatureScreen.Hide();
        }
    }
}
