using System.Diagnostics;

namespace OmsiVisualInterfaceNet.Managers.SolarisIII
{
    public class SolarisIIIScreenManager
    {
        private readonly Panel MainScreen;
        private readonly Panel LogoScreen;

        private readonly List<Panel> allScreens;
        private readonly Dictionary<string, PictureBox> iconPictureBoxes;
        private readonly SolarisIIIOmsiManager omsiManager;
        private readonly SerialManager serialManager;

        public System.Windows.Forms.Timer startupTimerTicker;
        private int startupTimerSeconds = 0;
        private bool startupSequenceActive = true;
        private string projectRoot;
        private bool isStopModeActive = false;

        private string lastFuelImage;
        private string lastAdBlueImage;
        private string lastLuftTank1Image;
        private string lastLuftTank2Image;
        private string lastLuftBremse1Image;
        private string lastLuftBremse2Image;
        private string lastDpfImage;
        private string lastTempImage;
        private string lastDoor0Image;
        private string lastDoor1Image;
        private string lastDoor2Image;
        private string lastDoor3Image;

        private Image blankImage;

        private readonly Dictionary<string, Image> imageCache = new Dictionary<string, Image>();

        public SolarisIIIScreenManager(Panel mainScreen, Panel logoScreen,
                            Dictionary<string, PictureBox> iconPictureBoxes,
                            SolarisIIIOmsiManager omsiManager, SerialManager serialManager)
        {
            MainScreen = mainScreen;
            LogoScreen = logoScreen;
            this.iconPictureBoxes = iconPictureBoxes;
            this.omsiManager = omsiManager;
            this.serialManager = serialManager;

            allScreens = new List<Panel>
            {
                MainScreen,
                LogoScreen
            };

            startupTimerTicker = new System.Windows.Forms.Timer();
            startupTimerTicker.Interval = 1000;
            startupTimerTicker.Tick += StartupTimerTicker_Tick;

            projectRoot = FindProjectRoot();
            LoadBlankImage();
        }

        private void LoadBlankImage()
        {
            try
            {
                string blankImagePath = Path.Combine(projectRoot, "Pictures\\Solaris_III_Sobol\\3D_12M\\Actia", "blank.png");
                if (File.Exists(blankImagePath))
                {
                    blankImage = Image.FromFile(blankImagePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load blank.png: {ex.Message}");
            }
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
                return;
            }

            bool isPowerOn = omsiManager.GetElectricState();

            if (startupTimerSeconds >= 0 && startupTimerSeconds < 2)
            {
                // During initial boot, show blank if power is off, logo if power is on
                HideAllScreens();

                if (isPowerOn)
                {
                    ShowScreen(LogoScreen);
                }
            }
            else if (startupTimerSeconds >= 2 && startupTimerSeconds < 4)
            {
                // After 2 seconds, if power is on show stop mode screen with door icons
                if (isPowerOn)
                {
                    ShowStopModeScreen();
                    startupSequenceActive = false;
                }
                else
                {
                    HideAllScreens();
                }
            }

            startupTimerSeconds++;
        }

        private void ShowStopModeScreen()
        {
            if (MainScreen.InvokeRequired)
            {
                MainScreen.Invoke(new Action(ShowStopModeScreen));
            }
            else
            {
                // Load and display the stop mode background image
                HideAllScreens();
                var stopModeImage = GetCachedImage("vdo_display_stopmode");
                SetScreenImage(MainScreen, stopModeImage ?? blankImage);
                MainScreen.BringToFront();
                MainScreen.Visible = true;

                isStopModeActive = true;
                UpdateDoorIndicators();
            }
        }

        private void ShowMainModeScreen()
        {
            if (MainScreen.InvokeRequired)
            {
                MainScreen.Invoke(new Action(ShowMainModeScreen));
            }
            else
            {
                // Load and display the main screen background image
                var mainImage = GetCachedImage("Actia_Type_2");
                SetScreenImage(MainScreen, mainImage ?? blankImage);
                MainScreen.BringToFront();
                MainScreen.Visible = true;

                isStopModeActive = false;
                HideDoorIndicators();
            }
        }

        public async void ChangeMode(bool up)
        {
            var currentMode = allScreens.FirstOrDefault(x => x.Visible == true);

            if (currentMode == MainScreen)
            {
                UpdateScreenVisibility(2.1);
                await ReturnToMainAfterDelay();
            }
            else if (currentMode == LogoScreen)
            {
                UpdateScreenVisibility(1.0);
            }
        }

        private void UpdateScreenVisibility(double currentMode)
        {
            if (currentMode == 1.0)
            {
                isStopModeActive = false;
                SetScreenImage(MainScreen, GetCachedImage("Actia_Type_2") ?? blankImage);
                UpdateMainScreenIcons();
            }
            else if (currentMode == 2.1 || currentMode == 4.0)
            {
                isStopModeActive = true;
                SetScreenImage(MainScreen, GetCachedImage("vdo_display_stopmode") ?? blankImage);
                UpdateDoorIndicators();
            }
        }

        private void UpdateDoorIndicators()
        {
            if (!isStopModeActive || omsiManager.CurrentVehicle == null) return;

            UpdateDoor0Indicator();
            UpdateDoor1Indicator();
            UpdateDoor2Indicator();
            UpdateDoor3Indicator();
        }

        private void UpdateDoor0Indicator()
        {
            try
            {
                bool door0_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_0")) > 0.3f;
                bool door1_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_1")) > 0.3f;
                bool door_nothahn = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Door_01_nothahn"));
                bool door_nopress = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("door_nopress_state01"));
                bool er4 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_4"));
                bool er12 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_12"));

                string imageName = null;

                if (door_nothahn || er4 || er12 || door_nopress)
                {
                    imageName = "door0_nothahn";
                }
                else
                {
                    bool tuersperre = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("tuersperre")) < 0;
                    bool light_timer1 = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("light_timer1")) > 1;
                    bool cg_active = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("CG_active"));

                    if (door0_open || door1_open)
                    {
                        imageName = "door0_open";
                    }
                    else if (tuersperre)
                    {
                        imageName = "door0_locked";
                    }
                    else if (cg_active && light_timer1)
                    {
                        imageName = "door0_oncg";
                    }
                    else if (!door0_open && !door1_open && light_timer1)
                    {
                        imageName = "door0_closed";
                    }
                }

                if (imageName != lastDoor0Image && iconPictureBoxes.TryGetValue("pb_door0", out var pb))
                {
                    lastDoor0Image = imageName;
                    UpdateDoorImage(pb, imageName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating door 0 indicator: {ex.Message}");
            }
        }

        private void UpdateDoor1Indicator()
        {
            try
            {
                bool door0_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_0")) > 0.3f;
                bool door1_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_1")) > 0.3f;
                bool door_nothahn = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Door_01_nothahn"));
                bool door_nopress = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("door_nopress_state01"));
                bool er4 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_4"));
                bool er12 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_12"));

                string imageName = null;

                if (door_nothahn || er4 || er12 || door_nopress)
                {
                    imageName = "door1_nothahn";
                }
                else
                {
                    bool tuersperre = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("tuersperre")) < 0;
                    bool light_timer1 = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("light_timer1")) > 1;
                    bool cg_active = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("CG_active"));

                    if (door0_open || door1_open)
                    {
                        imageName = "door1_open";
                    }
                    else if (tuersperre && !door1_open)
                    {
                        imageName = "door1_locked";
                    }
                    else if (cg_active && light_timer1)
                    {
                        imageName = "door1_oncg";
                    }
                    else if (!door0_open && !door1_open && light_timer1)
                    {
                        imageName = "door1_closed";
                    }
                }

                if (imageName != lastDoor1Image && iconPictureBoxes.TryGetValue("pb_door1", out var pb))
                {
                    lastDoor1Image = imageName;
                    UpdateDoorImage(pb, imageName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating door 1 indicator: {ex.Message}");
            }
        }

        private void UpdateDoor2Indicator()
        {
            try
            {
                bool door2_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_2")) > 0.3f;
                bool door3_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_3")) > 0.3f;
                bool door_nothahn = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Door_23_nothahn"));
                bool door_nopress = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("door_nopress_state23"));
                bool er4 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_4"));
                bool er12 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_12"));

                string imageName = null;

                if (door_nothahn || er4 || er12 || door_nopress)
                {
                    imageName = "door2_nothahn";
                }
                else
                {
                    bool light_timer2 = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("light_timer2")) > 1;
                    bool cg_active = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("CG_active"));

                    if (door2_open || door3_open)
                    {
                        imageName = "door2_open";
                    }
                    else if (cg_active && light_timer2)
                    {
                        imageName = "door2_oncg";
                    }
                    else if (!door2_open && !door3_open && light_timer2)
                    {
                        imageName = "door2_closed";
                    }
                }

                if (imageName != lastDoor2Image && iconPictureBoxes.TryGetValue("pb_door2", out var pb))
                {
                    lastDoor2Image = imageName;
                    UpdateDoorImage(pb, imageName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating door 2 indicator: {ex.Message}");
            }
        }

        private void UpdateDoor3Indicator()
        {
            try
            {
                bool door4_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_4")) > 0.3f;
                bool door5_open = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("door_5")) > 0.3f;
                bool door_nothahn = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Door_45_nothahn"));
                bool door_nopress = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("door_nopress_state45"));
                bool er4 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_4"));
                bool er12 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_12"));

                string imageName = null;

                if (door_nothahn || er4 || er12 || door_nopress)
                {
                    imageName = "door3_nothahn";
                }
                else
                {
                    bool light_timer3 = Convert.ToSingle(omsiManager.CurrentVehicle.GetVariable("light_timer3")) > 1;
                    bool cg_active = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("CG_active"));

                    if (door4_open || door5_open)
                    {
                        imageName = "door3_open";
                    }
                    else if (cg_active && light_timer3)
                    {
                        imageName = "door3_oncg";
                    }
                    else if (!door4_open && !door5_open && light_timer3)
                    {
                        imageName = "door3_closed";
                    }
                }

                if (imageName != lastDoor3Image && iconPictureBoxes.TryGetValue("pb_door3", out var pb))
                {
                    lastDoor3Image = imageName;
                    UpdateDoorImage(pb, imageName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating door 3 indicator: {ex.Message}");
            }
        }

        private void HideDoorIndicators()
        {
            lastDoor0Image = null;
            lastDoor1Image = null;
            lastDoor2Image = null;
            lastDoor3Image = null;

            if (iconPictureBoxes.TryGetValue("pb_door0", out var pb0))
                UpdateDoorImage(pb0, null);
            if (iconPictureBoxes.TryGetValue("pb_door1", out var pb1))
                UpdateDoorImage(pb1, null);
            if (iconPictureBoxes.TryGetValue("pb_door2", out var pb2))
                UpdateDoorImage(pb2, null);
            if (iconPictureBoxes.TryGetValue("pb_door3", out var pb3))
                UpdateDoorImage(pb3, null);
        }

        public void UpdateDoorImage(PictureBox pb, string imageName)
        {
            if (pb == null) return;

            if (string.IsNullOrEmpty(imageName))
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() => pb.Image = blankImage));
                }
                else
                {
                    pb.Image = blankImage;
                }
                return;
            }

            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() =>
                {
                    var img = GetCachedImage(imageName);
                    pb.Image = img ?? blankImage;
                }));
            }
            else
            {
                var img = GetCachedImage(imageName);
                pb.Image = img ?? blankImage;
            }
        }

        public void UpdateMainScreenIcons()
        {
            UpdateIcon1();
            UpdateIcon2();
            UpdateIcon3();
            UpdateIcon4();
            UpdateIcon5();
            UpdateIcon6();
            UpdateIcon7();
            UpdateIcon8();
            UpdateIcon9();
            UpdateIcon10();
            UpdateIcon11();
            UpdateIcon12();
            UpdateIcon13();
            UpdateIcon14();
        }

        // Icon 1: Lights
        private void UpdateIcon1()
        {
            bool er4 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_4"));
            bool er11 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_11"));
            bool er12 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_12"));

            if (er4 || er11 || er12)
            {
                UpdateIconVariant("pb_icon1", 0);
            }
            else if (er11)
            {
                bool er33 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_33"));
                bool er55 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_55"));
                bool er56 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er56"));

                if (er33 || er55 || er56)
                {
                    UpdateIconVariant("pb_icon1", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon1"], "", false);
                }
            }
            else
            {
                SetIconImage(iconPictureBoxes["pb_icon1"], "", false);
            }
        }

        // Icon 2: HVAC / Heater
        private void UpdateIcon2()
        {
            bool er52 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_52"));
            bool er53 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_53"));

            if (er52 || er53)
            {
                UpdateIconVariant("pb_icon2", 0);
            }
            else
            {
                bool er7 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_7"));
                bool er34 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_34"));

                if (er7 || er34)
                {
                    UpdateIconVariant("pb_icon2", 1);
                }
                else
                {
                    bool standheizung_running = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("standheizung_running"));

                    if (standheizung_running)
                    {
                        UpdateIconVariant("pb_icon2", 2);
                    }
                    else
                    {
                        bool hvac_ac_cooling = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("HVAC_AC_cooling"));
                        bool hvac_ac_heizung = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("HVAC_AC_heizung"));
                        bool hvac_fhr_ac_cooling = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("HVAC_FhrAC_cooling"));

                        if (hvac_ac_cooling || hvac_ac_heizung || hvac_fhr_ac_cooling)
                        {
                            UpdateIconVariant("pb_icon2", 3);
                        }
                        else
                        {
                            SetIconImage(iconPictureBoxes["pb_icon2"], "", false);
                        }
                    }
                }
            }
        }

        // Icon 3: Engine
        private void UpdateIcon3()
        {
            bool er63 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_63"));
            bool er64 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_64"));

            if (er63 || er64)
            {
                UpdateIconVariant("pb_icon3", 0);
            }
            else
            {
                bool er14 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_14"));
                bool er61 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_61"));

                if (er14 || er61)
                {
                    UpdateIconVariant("pb_icon3", 1);
                }
                else
                {
                    bool er36 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_36"));

                    if (er36)
                    {
                        UpdateIconVariant("pb_icon3", 2);
                    }
                    else
                    {
                        SetIconImage(iconPictureBoxes["pb_icon3"], "", false);
                    }
                }
            }
        }

        // Icon 4: AC
        private void UpdateIcon4()
        {
            bool er47 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_47"));
            bool er48 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_48"));

            if (er47 || er48)
            {
                UpdateIconVariant("pb_icon4", 0);
            }
            else
            {
                bool er6 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_6"));

                if (er6)
                {
                    UpdateIconVariant("pb_icon4", 1);
                }
                else
                {
                    bool weather_temp_low = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Weather_Temperature"));

                    if (weather_temp_low)
                    {
                        UpdateIconVariant("pb_icon4", 2);
                    }
                    else
                    {
                        SetIconImage(iconPictureBoxes["pb_icon4"], "", false);
                    }
                }
            }
        }

        // Icon 5: Fuel
        private void UpdateIcon5()
        {
            bool er46 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_46"));
            bool er61 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_61"));
            bool er74 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_74"));
            bool er79 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_79"));

            if (er46 || er61 || er74 || er79)
            {
                UpdateIconVariant("pb_icon5", 0);
            }
            else
            {
                bool er54 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_54"));
                bool er55 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_55"));
                bool er60 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_60"));
                bool er59 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_59"));
                bool er73 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_73"));

                if (er54 || er55 || er60 || er59 || er73)
                {
                    UpdateIconVariant("pb_icon5", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon5"], "", false);
                }
            }
        }

        // Icon 6: Retarder
        private void UpdateIcon6()
        {
            bool er75 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_75"));
            bool er79 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_79"));

            if (er75 || er79)
            {
                UpdateIconVariant("pb_icon6", 0);
            }
            else
            {
                bool er9 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_9"));

                if (er9)
                {
                    UpdateIconVariant("pb_icon6", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon6"], "", false);
                }
            }
        }

        // Icon 7: Not Hahn
        private void UpdateIcon7()
        {
            bool er25 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_25"));
            bool er54 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_54"));
            bool er86 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_86"));

            if (er25 || er54 || er86)
            {
                UpdateIconVariant("pb_icon7", 0);
            }
            else
            {
                bool er70 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_70"));
                bool er71 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_71"));
                bool er72 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_72"));
                bool er73 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_73"));

                if (er70 || er71 || er72 || er73)
                {
                    UpdateIconVariant("pb_icon7", 1);
                }
                else
                {
                    bool er57 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_57"));
                    bool er58 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_58"));

                    if (er57 || er58)
                    {
                        UpdateIconVariant("pb_icon7", 2);
                    }
                    else
                    {
                        SetIconImage(iconPictureBoxes["pb_icon7"], "", false);
                    }
                }
            }
        }

        // Icon 8: EBS
        private void UpdateIcon8()
        {
            bool er79 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_79"));
            bool er80 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_80"));
            bool er81 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_81"));
            bool er82 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_82"));
            bool er83 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_83"));

            if (er79 || er80 || er81 || er82 || er83)
            {
                UpdateIconVariant("pb_icon8", 0);
            }
            else
            {
                bool er66 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_66"));

                if (er66)
                {
                    UpdateIconVariant("pb_icon8", 1);
                }
                else
                {
                    bool er20 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_20"));

                    if (er20)
                    {
                        UpdateIconVariant("pb_icon8", 2);
                    }
                    else
                    {
                        SetIconImage(iconPictureBoxes["pb_icon8"], "", false);
                    }
                }
            }
        }

        // Icon 9: EDC
        private void UpdateIcon9()
        {
            bool er1 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_1"));
            bool er59 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_59"));
            bool er62 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_62"));
            bool er83 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_83"));
            bool er84 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_84"));

            if (er1 || er59 || er62 || er83 || er84)
            {
                UpdateIconVariant("pb_icon9", 0);
            }
            else
            {
                bool er13 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_13"));

                if (er13)
                {
                    UpdateIconVariant("pb_icon9", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon9"], "", false);
                }
            }
        }

        // Icon 10: ECAS
        private void UpdateIcon10()
        {
            bool er5 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_5"));
            bool er39 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_39"));
            bool er43 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_43"));
            bool er57 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_57"));
            bool er73 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_73"));
            bool er77 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_77"));
            bool er82 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_82"));
            bool er84 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_84"));
            bool er85 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_85"));

            if (er5 || er39 || er43 || er57 || er73 || er77 || er82 || er84 || er85)
            {
                UpdateIconVariant("pb_icon10", 0);
            }
            else
            {
                bool er3 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_3"));
                bool er60 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_60"));
                bool er62 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_62"));

                if (er3 || er60 || er62)
                {
                    UpdateIconVariant("pb_icon10", 1);
                }
                else
                {
                    bool er19 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_19"));
                    bool er24 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_24"));
                    bool er26 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_26"));
                    bool er37 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_37"));
                    bool er38 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_38"));
                    bool er67 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_67"));
                    bool er69 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_69"));
                    bool er68 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_68"));

                    if (er19 || er24 || er26 || er37 || er38 || er67 || er69 || er68)
                    {
                        UpdateIconVariant("pb_icon10", 2);
                    }
                    else
                    {
                        SetIconImage(iconPictureBoxes["pb_icon10"], "", false);
                    }
                }
            }
        }

        // Icon 11: ASR
        private void UpdateIcon11()
        {
            bool er70 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_70"));
            bool er72 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_72"));
            bool er73 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_73"));

            if (er70 || er72 || er73)
            {
                UpdateIconVariant("pb_icon11", 0);
            }
            else
            {
                bool er67 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_67"));
                bool er69 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_69"));
                bool er68 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_68"));
                bool er71 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_71"));

                if (er67 || er69 || er68 || er71)
                {
                    UpdateIconVariant("pb_icon11", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon11"], "", false);
                }
            }
        }

        // Icon 12: DPF
        private void UpdateIcon12()
        {
            bool er8 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_8"));

            if (er8)
            {
                UpdateIconVariant("pb_icon12", 0);
            }
            else
            {
                bool er15 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_15"));

                if (er15)
                {
                    UpdateIconVariant("pb_icon12", 1);
                }
                else
                {
                    SetIconImage(iconPictureBoxes["pb_icon12"], "", false);
                }
            }
        }

        // Icon 13: Brakes
        private void UpdateIcon13()
        {
            bool er21 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_21"));
            bool er78 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_78"));
            bool er79 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_79"));
            bool er82 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_82"));

            if (er21 || er78 || er79 || er82)
            {
                UpdateIconVariant("pb_icon13", 0);
            }
            else
            {
                bool er2 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_2"));

                if (er2)
                {
                    UpdateIconVariant("pb_icon13", 1);
                }
                else
                {
                    bool er17 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_17"));
                    bool er28 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_28"));
                    bool er77 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_77"));

                    if (er17 || er28 || er77)
                    {
                        UpdateIconVariant("pb_icon13", 2);
                    }
                    else
                    {
                        bool antrieb_retarder = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("antrieb_retarder"));

                        if (antrieb_retarder)
                        {
                            UpdateIconVariant("pb_icon13", 3);
                        }
                        else
                        {
                            SetIconImage(iconPictureBoxes["pb_icon13"], "", false);
                        }
                    }
                }
            }
        }

        // Icon 14: HVAC
        private void UpdateIcon14()
        {
            bool er10 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_10"));
            bool er40 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_40"));
            bool er41 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_41"));
            bool er51 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_51"));
            bool er63 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_63"));
            bool er64 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_64"));
            bool er65 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_65"));

            if (er10 || er40 || er41 || er51 || er63 || er64 || er65)
            {
                UpdateIconVariant("pb_icon14", 0);
            }
            else
            {
                bool er16 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_16"));
                bool er49 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_49"));

                if (er16 || er49)
                {
                    UpdateIconVariant("pb_icon14", 1);
                }
                else
                {
                    bool er45 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_45"));
                    bool er50 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_50"));

                    if (er45 || er50)
                    {
                        UpdateIconVariant("pb_icon14", 2);
                    }
                    else
                    {
                        bool er32 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_32"));

                        if (er32)
                        {
                            UpdateIconVariant("pb_icon14", 3);
                        }
                        else
                        {
                            bool er18 = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("Er_18"));

                            if (er18)
                            {
                                UpdateIconVariant("pb_icon14", 4);
                            }
                            else
                            {
                                bool engine_asr = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("engine_ASR_eingriff"));
                                bool bremse_abs = Convert.ToBoolean(omsiManager.CurrentVehicle.GetVariable("bremse_ABS_eingriff"));

                                if (engine_asr || bremse_abs)
                                {
                                    UpdateIconVariant("pb_icon14", 5);
                                }
                                else
                                {
                                    SetIconImage(iconPictureBoxes["pb_icon14"], "", false);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateIconVariant(string pb, int variant)
        {
            if (variant < 0)
            {
                UpdateIcon(pb, false);
            }
            else
            {
                string iconNumber = pb.Replace("pb_icon", "");
                string iconImageName = $"{iconNumber}-{variant}";

                if (iconPictureBoxes.TryGetValue(pb, out var pictureBox))
                {
                    if (pictureBox.InvokeRequired)
                    {
                        pictureBox.Invoke(new Action(() =>
                        {
                            var img = GetCachedImage(iconImageName);
                            pictureBox.Image = img ?? blankImage;
                        }));
                    }
                    else
                    {
                        var img = GetCachedImage(iconImageName);
                        pictureBox.Image = img ?? blankImage;
                    }
                }
            }
        }

        public void UpdateIcon(string icon_name, bool active)
        {
            if (iconPictureBoxes.TryGetValue(icon_name, out var pb))
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() => SetIconImage(pb, icon_name, active)));
                }
                else
                {
                    SetIconImage(pb, icon_name, active);
                }
            }
        }

        private void SetIconImage(PictureBox pb, string iconName, bool active)
        {
            if (active)
            {
                pb.Image = GetCachedImage(iconName);
            }
            else
            {
                pb.Image = blankImage;
            }
        }

        private Image GetCachedImage(string iconName)
        {
            if (imageCache.TryGetValue(iconName, out var cachedImage))
            {
                return cachedImage;
            }

            try
            {
                string actiaPath = Path.Combine(
                    projectRoot,
                    "Pictures",
                    "Solaris_III_Sobol",
                    "3D_12M",
                    "Actia");

                if (!Directory.Exists(actiaPath))
                    return null;

                string[] files = Directory.GetFiles(
                    actiaPath,
                    $"{iconName}.png",
                    SearchOption.AllDirectories);

                if (files.Length == 0)
                    return null;

                var image = Image.FromFile(files[0]);
                imageCache[iconName] = image;

                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load image {iconName}: {ex.Message}");
            }
            return null;
        }

        private string GetFuelImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Fuel_{rounded}";
        }

        public void UpdateFuelImage(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetFuelImageNameFromPercent(percent, step);

            if (imageName == lastFuelImage)
                return;

            lastFuelImage = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetAdBlueImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"AdBlue_{rounded}";
        }

        public void UpdateAdBlueImage(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetAdBlueImageNameFromPercent(percent, step);

            if (imageName == lastAdBlueImage)
                return;

            lastAdBlueImage = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetLuftTank1ImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Luft_Tank1_{rounded}";
        }

        public void UpdateLuftTank1Image(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetLuftTank1ImageNameFromPercent(percent, step);

            if (imageName == lastLuftTank1Image)
                return;

            lastLuftTank1Image = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetLuftTank2ImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Luft_Tank2_{rounded}";
        }

        public void UpdateLuftTank2Image(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetLuftTank2ImageNameFromPercent(percent, step);

            if (imageName == lastLuftTank2Image)
                return;

            lastLuftTank2Image = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetLuftBremse1ImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Luft_Brems1_{rounded}";
        }

        public void UpdateLuftBremse1Image(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetLuftBremse1ImageNameFromPercent(percent, step);

            if (imageName == lastLuftBremse1Image)
                return;

            lastLuftBremse1Image = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetLuftBremse2ImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Luft_Brems2_{rounded}";
        }

        public void UpdateLuftBremse2Image(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetLuftBremse2ImageNameFromPercent(percent, step);

            if (imageName == lastLuftBremse2Image)
                return;

            lastLuftBremse2Image = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetDpfImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"DPF_{rounded}";
        }

        public void UpdateDpfImage(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetDpfImageNameFromPercent(percent, step);

            if (imageName == lastDpfImage)
                return;

            lastDpfImage = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private string GetTempImageNameFromPercent(double percent, int step = 1)
        {
            percent = Math.Clamp(percent, 0.0, 100.0);
            int rounded = (int)(Math.Round(percent / step) * step);
            return $"Temp_{rounded}";
        }

        public void UpdateTempImage(PictureBox pb, double percent, int step = 1)
        {
            if (pb == null) return;
            string imageName = GetTempImageNameFromPercent(percent, step);

            if (imageName == lastTempImage)
                return;

            lastTempImage = imageName;
            var img = GetCachedImage(imageName);
            if (pb.InvokeRequired)
            {
                pb.Invoke(new Action(() => pb.Image = img));
            }
            else
            {
                pb.Image = img;
            }
        }

        private void SetAllIconsVisible(bool visible)
        {
            foreach (var pb in iconPictureBoxes.Values)
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() => pb.Image = visible ? pb.Image : blankImage));
                }
                else
                {
                    pb.Image = visible ? pb.Image : blankImage;
                }
            }
        }

        private void ShowScreen(Panel screen)
        {
            if (screen.InvokeRequired)
            {
                screen.Invoke(new Action(() =>
                {
                    screen.BringToFront();
                    screen.Visible = true;
                }));
            }
            else
            {
                screen.BringToFront();
                screen.Visible = true;
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

        private void SetScreenImage(Panel panel, Image image)
        {
            panel.BackgroundImage = image;
            panel.BackgroundImageLayout = ImageLayout.Stretch;
        }

        private string FindProjectRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (dir != null)
            {
                bool hasPictures = Directory.Exists(Path.Combine(dir.FullName, "Pictures"));
                bool hasCsproj = dir.GetFiles("*.csproj").Any();

                if (hasPictures || hasCsproj)
                    return dir.FullName;

                dir = dir.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public void HideAllScreens()
        {
            SetScreenImage(MainScreen, blankImage);
            SetScreenImage(LogoScreen, blankImage);
        }
    }
}