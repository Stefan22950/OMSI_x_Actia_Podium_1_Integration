using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using OmsiHook;

namespace OmsiVisualInterfaceNet
{
    public class DashboardManager
    {
        private readonly SerialManager serialManager;
        private readonly OmsiManager omsiManager;

        private bool dashboardOn = false;
        private bool logoSequenceRunning = false;
        private int lastBlinkerState = 0;
        private int lastArduinoBlinkerState = 0;

        private readonly Panel stopScreen;
        private readonly Panel mainScreen;
        private readonly Panel logoScreen;
        private readonly Label warningLabel;
        private readonly Panel coolantTempPanel;
        private readonly Panel fuelPanel;
        private readonly Panel adbluePanel;

        private readonly PictureBox ms_fuel;

        public DashboardManager(
            SerialManager serialManager,
            OmsiManager omsiManager,
            Panel stopScreen,
            Panel mainScreen,
            Panel logoScreen,
            Label warningLabel,
            Panel coolantTempPanel,
            Panel fuelPanel,
            Panel adbluePanel,
            PictureBox ms_fuel)
        {
            this.stopScreen = stopScreen;
            this.mainScreen = mainScreen;
            this.logoScreen = logoScreen;
            this.warningLabel = warningLabel;
            this.serialManager = serialManager;
            this.omsiManager = omsiManager;
            this.coolantTempPanel = coolantTempPanel;
            this.fuelPanel = fuelPanel;
            this.adbluePanel = adbluePanel;

            this.ms_fuel = ms_fuel;

            serialManager.OnDataReceived += HandleSerialInput;
            omsiManager.OnVehicleChanged += HandleVehicleChanged;
            this.fuelPanel = fuelPanel;
            this.adbluePanel = adbluePanel;
        }

        public void Update()
        {
            if (omsiManager.CurrentVehicle == null) return;

            var elec = omsiManager.GetElectricState();
            var speed = omsiManager.GetSpeed();
            var rpm = omsiManager.GetRPM();

            UpdatePowerState(elec);

            if (dashboardOn)
            {
                UpdateSpeedometer(speed);
                UpdateTachometer(rpm);
                /*UpdateBlinkerState();
                UpdateHazardLights();
                UpdateDoorStates();
                UpdateGearIndicator();
                UpdateWarningLights();*/
                UpdateCoolantTemperature();
                UpdateFuel();
            }
        }

        private void UpdatePowerState(int elecStatus)
        {
            if (elecStatus == 1 && !dashboardOn && !logoSequenceRunning)
            {
                ShowLogoAndSwitchToStopScreen();
            }
            else if (elecStatus == 0 && dashboardOn)
            {
                stopScreen.Hide();
                mainScreen.Hide();
                logoScreen.Hide();
                serialManager.WriteLine("RPM:0");
                serialManager.WriteLine("SPEED:0");
                dashboardOn = false;
            }
        }

        private async void ShowLogoAndSwitchToStopScreen()
        {
            logoSequenceRunning = true;
            logoScreen.Show();
            await Task.Delay(5000);
            logoScreen.Hide();
            stopScreen.Show();
            dashboardOn = true;
            logoSequenceRunning = false;
        }

        private void UpdateSpeedometer(double speed)
        {
            int needleSteps = (int)((speed / 125.0) * 685);
            needleSteps = Math.Clamp(needleSteps, 0, 685);
            serialManager.WriteLine("SPEED:" + needleSteps);
            warningLabel.Text = omsiManager.GetRPM().ToString();
        }

        private void UpdateTachometer(double rpm)
        {
            var rpmSteps = (rpm / 3000.0 * 730.0);
            rpmSteps = Math.Clamp(rpmSteps, 0, 730);
            serialManager.WriteLine("RPM:" + rpmSteps);
        }

        /*private void UpdateBlinkerState()
        {
            int currentBlinker = omsiManager.GetBlinkerState();
            if (currentBlinker != lastBlinkerState)
            {
                if (currentBlinker == -1)
                {
                    serialManager.WriteLine("BLINKER LEFT");
                }
                else if (currentBlinker == 0)
                {
                    serialManager.WriteLine("BLINKER OFF");
                }
                else if (currentBlinker == 1)
                {
                    serialManager.WriteLine("BLINKER RIGHT");
                }
                lastBlinkerState = currentBlinker;
            }
        }

        private void UpdateHazardLights()
        {
            bool hazardsOn = omsiManager.GetHazardLightsState();
            serialManager.WriteLine($"HAZARDS:{(hazardsOn ? "ON" : "OFF")}");
        }

        private void UpdateDoorStates()
        {
            for (int i = 1; i <= 3; i++)
            {
                bool doorOpen = omsiManager.GetDoorState(i);
                bool doorLightOn = omsiManager.GetDoorLightState(i);
                serialManager.WriteLine($"DOOR_{i}:{(doorOpen ? "OPEN" : "CLOSED")}");
                serialManager.WriteLine($"DOOR_LIGHT_{i}:{(doorLightOn ? "ON" : "OFF")}");
            }
        }

        private void UpdateGearIndicator()
        {
            int gear = omsiManager.GetGearState();
            serialManager.WriteLine($"GEAR:{(gear == -1 ? "R" : gear == 0 ? "N" : "D")}");
        }

        private void UpdateWarningLights()
        {
            bool engineWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_motorfailure") > 0;
            bool brakeWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_brakefailure") > 0;
            serialManager.WriteLine($"WARNING_ENGINE:{(engineWarning ? "ON" : "OFF")}");
            serialManager.WriteLine($"WARNING_BRAKE:{(brakeWarning ? "ON" : "OFF")}");
        }
*/
        private void UpdateCoolantTemperature()
        {
            if (omsiManager.CurrentVehicle == null) return;
            double coolantTemp = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_temperature"));

            coolantTemp = Math.Clamp(coolantTemp, 40, 120);

            int width = (int)(8 + (coolantTemp - 40) * 642 / 80);

            coolantTempPanel.Width = width;
        }

        private void UpdateFuel()
        {
            double fuelLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_tank_content"));
            double adblueLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("AdBlue_level"));

            fuelLevel = Math.Clamp(fuelLevel, 0, 1);
            int widthfuel = (int)(8 + (fuelLevel) * 638 / 80);
            fuelPanel.Width = widthfuel;

            adblueLevel = Math.Clamp(adblueLevel, 0, 1);
            int widthadblue = (int)(8 + (adblueLevel) * 638 / 80);
            adbluePanel.Width = widthadblue;

            ms_fuel.Visible = fuelLevel < 0.2;

        }

        public void HandleSerialInput(string input)
        {
            if (omsiManager.CurrentVehicle == null) return;

            switch (input)
            {
                case "BLINKER_LEFT":
                    omsiManager.SetBlinkerState(1);
                    break;
                case "BLINKER_RIGHT":
                    omsiManager.SetBlinkerState(2);
                    break;
                case "BLINKER_OFF":
                    omsiManager.SetBlinkerState(0);
                    break;
                case "HAZARDS_ON":
                    omsiManager.SetHazardLightsState(true);
                    break;
                case "HAZARDS_OFF":
                    omsiManager.SetHazardLightsState(false);
                    break;
                case "HORN_ON":
                    omsiManager.SetHornState(true);
                    break;
                case "HORN_OFF":
                    omsiManager.SetHornState(false);
                    break;
                case "LOW_BEAM_ON":
                    omsiManager.SetLowBeamState(true);
                    break;
                case "LOW_BEAM_OFF":
                    omsiManager.SetLowBeamState(false);
                    break;
                case "HIGH_BEAM_ON":
                    omsiManager.SetHighBeamState(true);
                    break;
                case "HIGH_BEAM_OFF":
                    omsiManager.SetHighBeamState(false);
                    break;
                case "ENGINE_START":
                    omsiManager.SetStarterState(true);
                    break;
                case "ENGINE_STOP":
                    omsiManager.SetStarterState(false);
                    break;
                case "HEATING_ON":
                    omsiManager.SetHeatingState(true);
                    break;
                case "HEATING_OFF":
                    omsiManager.SetHeatingState(false);
                    break;
                case "RETARDER_ON":
                    omsiManager.SetRetarderState(true);
                    break;
                case "RETARDER_OFF":
                    omsiManager.SetRetarderState(false);
                    break;
                case "KNEELING_ON":
                    omsiManager.SetKneelingState(true);
                    break;
                case "KNEELING_OFF":
                    omsiManager.SetKneelingState(false);
                    break;

                case "DOOR_1_PRESSED":
                    bool current = omsiManager.GetDoorState(1);
                    omsiManager.SetDoorState(1, !current);
                    break;
                
            }
        }

        private void HandleVehicleChanged(OmsiRoadVehicleInst vehicle)
        {
            // Reset states when vehicle changes
            dashboardOn = false;
            logoSequenceRunning = false;
            lastBlinkerState = 0;
            lastArduinoBlinkerState = 0;
        }
    }
}