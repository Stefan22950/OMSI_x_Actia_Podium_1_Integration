using System.Diagnostics;
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
        private readonly List<Panel> allScreens;
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
            //{ "Actia_DISPLAY_ICO_AdBlue", "ms_adBlue" },
            //{ "Actia_DISPLAY_ICO_fuel", "ms_fuel" }
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

            allScreens = new List<Panel>
            {
                StopScreen,
                MainScreen,
                LogoScreen,
                FuelScreen,
                PressureScreen,
                CoolantTemperatureScreen
            };

            startupTimerTicker = new System.Windows.Forms.Timer();
            startupTimerTicker.Interval = 1000; 
            startupTimerTicker.Tick += StartupTimerTicker_Tick;

        }

        private async Task ReturnToMainAfterDelay()
        {
            Debug.WriteLine("Delay started");
            await Task.Delay(5000);
            Debug.WriteLine("Delay finished, returning to main screen");
            if (omsiManager.GetMainScreen() == 1)
                UpdateScreenVisibility(4.0);
            else
                UpdateScreenVisibility(1.0);
        }

        public void RestartStartupSequence()
        {

            startupTimerSeconds = 0;
            startupSequenceActive = true;

            HideAllScreens();

            SetAllIconsVisible(false);

            startupTimerTicker.Start();
        }

        public void UpdateMainScreen()
        {
            if (omsiManager.GetMainScreen() == 1)
            {
                UpdateScreenVisibility(4.0);
            }
            else
            {
                UpdateScreenVisibility(1.0);
                UpdateMainScreenIcons();
            }
            
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

        public async void ChangeMode(bool up)
        {
            var currentMode = allScreens.FirstOrDefault(x => x.Visible==true);

            if (up)
            {
                if (currentMode == MainScreen || currentMode == StopScreen)
                {
                    UpdateScreenVisibility(2.1);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == PressureScreen)
                {
                    UpdateScreenVisibility(2.2);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == FuelScreen)
                { 
                    UpdateScreenVisibility(2.3);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == CoolantTemperatureScreen)
                {
                    if (omsiManager.GetMainScreen() == 1)
                    {
                        UpdateScreenVisibility(4.0);
                    }
                    else
                    {
                        UpdateScreenVisibility(1.0);
                    }
                }
            }
            else
            {
                if (currentMode == MainScreen || currentMode == StopScreen)
                {
                    UpdateScreenVisibility(2.3);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == CoolantTemperatureScreen)
                {
                    UpdateScreenVisibility(2.2);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == FuelScreen)
                {
                    UpdateScreenVisibility(2.1);
                    await ReturnToMainAfterDelay();
                }
                else if (currentMode == PressureScreen)
                {
                    if (omsiManager.GetMainScreen() == 1)
                    {
                        UpdateScreenVisibility(4.0);
                    }
                    else
                    {
                        UpdateScreenVisibility(1.0);
                    }
                }

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
                MakeScreenVisible(MainScreen);
                UpdateMainScreenIcons();
            }
            else
            {
                //SetAllIconsVisible(false);
                if (currentMode == 2.1)
                    MakeScreenVisible(PressureScreen);
                else if (currentMode == 2.2)
                    MakeScreenVisible(FuelScreen);
                else if (currentMode == 2.3)
                    MakeScreenVisible(CoolantTemperatureScreen);
                else if (currentMode == 4.0)
                    MakeScreenVisible(StopScreen);
            }

        }

        public void UpdateMainScreenIcons()
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

        private void MakeScreenVisible(Panel screen)
        {
            foreach (var s in allScreens)
            {
                if (s.InvokeRequired)
                {
                    s.Invoke(new Action(() => s.Visible = false));
                }
                else
                {
                    s.Visible = false;
                }
            }

            if (screen.InvokeRequired)
            {
                screen.Invoke(new Action(() => screen.Visible = true));
            }
            else
            {
                screen.Visible = true;
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
