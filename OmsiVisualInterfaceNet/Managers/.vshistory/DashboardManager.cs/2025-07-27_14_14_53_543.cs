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

        // Add new field to track initialization state
        private bool isInitializing;
        private const string BOOT_MESSAGE = "=== SYSTEM BOOT ===";


        // UI Elements
        private readonly Panel stopScreen;
        private readonly Panel mainScreen;
        private readonly Panel logoScreen;
        private readonly Label warningLabel;
        private readonly Panel coolantTempPanel;
        private readonly Panel fuelPanel;
        private readonly Panel adbluePanel;
        private readonly PictureBox ms_fuel;

        private bool lastElecState = false;
        private bool lastHazardsState = false;
        private bool[] lastDoorStates = new bool[3];
        private int lastGearState = 0;
        private bool lastRetarderState = false;
        private bool lastAutoDoorState = false;
        private bool lastCheckEngineWarningState;
        private bool lastParkingBrakeState;
        private bool lastAbsWarningState;
        private bool lastDiagWarningState;
        private bool lastbusStopState;
        private bool lastSTOPState;      
        private bool lastHighBeamState;
        private bool lastOverheatState;
        private int lastBlinkerState = 0;
        private int lastArduinoBlinkerState = 0;
        public bool lastNightLightState;
        private bool lastLowBeamState;
        private bool lastLowFuelState;
        private string lastSerialCommand = null;

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

            // Modify the serial event handler to handle boot sequence
            serialManager.OnDataReceived += async (data) => await HandleSerialInput(data);
        }

        public void Initialize()
        {
            lastNightLightState = false;
            lastCheckEngineWarningState = false;
            lastParkingBrakeState = false;
            lastAbsWarningState = false;
            lastDiagWarningState = false;
            lastbusStopState = false;
            lastSTOPState = false;
            lastHighBeamState = false;
            lastOverheatState = false;
            lastLowFuelState = false;

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
                omsiManager.Update();
                UpdateCoolantTemperature();
                UpdateFuel();
                UpdateClusterLights();
            }
        }

        private void UpdatePowerState(bool elecStatus)
        {
            if (elecStatus == true && !dashboardOn && !logoSequenceRunning)
            {
                ShowLogoAndSwitchToStopScreen();
                Initialize();
            }
            else if (elecStatus == false && dashboardOn)
            {
                serialManager.WriteLine("RPM:0");
                serialManager.WriteLine("SPEED:0");
                stopScreen.Hide();
                mainScreen.Hide();
                logoScreen.Hide();
                dashboardOn = false;
                HandleVehicleChanged(omsiManager.CurrentVehicle);
            }
        }

        private void ShowLogoAndSwitchToStopScreen()
        {
            logoSequenceRunning = true;
            logoScreen.Show();

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000; 
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                logoScreen.Hide();
                stopScreen.Show();
                dashboardOn = true;
                logoSequenceRunning = false;
            };
            timer.Start();
        }

        private void UpdateSpeedometer(double speed)
        {
            if (speed >= 1)
            {
                int needleSteps = (int)((speed / 125.0) * 685);
                needleSteps = Math.Clamp(needleSteps, 0, 685);
                serialManager.WriteLine("SPEED:" + needleSteps);
            }
        }

        private void UpdateTachometer(double rpm)
        {
            if (rpm >= 1)
            { 
                var rpmSteps = (rpm / 3000.0 * 730.0);
                rpmSteps = Math.Clamp(rpmSteps, 0, 730);
                serialManager.WriteLine("RPM:" + rpmSteps);
            }
        }

        private void UpdateBlinkerState()
        {
            int currentBlinker = omsiManager.GetBlinkerState();
            if (currentBlinker != lastBlinkerState)
            {
                if (currentBlinker == -1)
                {
                    serialManager.WriteLine("LEFT_BLINKER_LIGHT_ON");
                    serialManager.WriteLine("RIGHT_BLINKER_LIGHT_OFF");
                }
                else if (currentBlinker == 0)
                {
                    serialManager.WriteLine("LEFT_BLINKER_LIGHT_OFF");
                    serialManager.WriteLine("RIGHT_BLINKER_LIGHT_OFF");
                }
                else if (currentBlinker == 1)
                {
                    serialManager.WriteLine("LEFT_BLINKER_LIGHT_OFF");
                    serialManager.WriteLine("RIGHT_BLINKER_LIGHT_ON");
                }
                lastBlinkerState = currentBlinker;
            }
        }

        private void UpdateHazardLights()
        {
            bool hazardsOn = omsiManager.GetHazardLightsState();
            if (hazardsOn != lastHazardsState)
            {
                if (hazardsOn)
                {
                    serialManager.WriteLine("LEFT_BLINKER_LIGHT_ON");
                    serialManager.WriteLine("RIGHT_BLINKER_LIGHT_ON");
                }
                else
                {
                    if (lastBlinkerState == 0)
                    {
                        serialManager.WriteLine("LEFT_BLINKER_LIGHT_OFF");
                        serialManager.WriteLine("RIGHT_BLINKER_LIGHT_OFF");
                    }
                }
                lastHazardsState = hazardsOn;
            }
        }

        /*private void UpdateDoorStates()
        {
            for (int i = 1; i <= 3; i++)
            {
                bool doorOpen = omsiManager.GetDoorState(i);
                bool doorLightOn = omsiManager.GetDoorLightState(i);

                if (doorOpen != lastDoorStates[i - 1])
                {
                    serialManager.WriteLine($"DOOR_{i}:{(doorOpen ? "OPEN" : "CLOSED")}");
                    lastDoorStates[i - 1] = doorOpen;
                }

                // Update door lights on dashboard
                serialManager.WriteLine($"DOOR_LIGHT_{i}:{(doorLightOn ? "ON" : "OFF")}");
            }
        }*/

        private void UpdateGearIndicator()
        {
            int gear = omsiManager.GetGearState();
            if (gear != lastGearState)
            {
                serialManager.WriteLine($"GEAR:{(gear == -1 ? "R" : gear == 0 ? "N" : "D")}");

                // Update gear indicator lights
                serialManager.WriteLine(gear == -1 ? "REVERSE_LIGHT_ON" : "REVERSE_LIGHT_OFF");
                serialManager.WriteLine(gear == 0 ? "NEUTRAL_LIGHT_ON" : "NEUTRAL_LIGHT_OFF");
                serialManager.WriteLine(gear == 1 ? "DRIVE_LIGHT_ON" : "DRIVE_LIGHT_OFF");

                lastGearState = gear;
            }
        }

        private void UpdateCoolantTemperature()
        {
            if (omsiManager.CurrentVehicle == null) return;
            double coolantTemp = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_temperature"));
            coolantTemp = Math.Clamp(coolantTemp, 40, 120);

            // Update coolant gauge
            int width = (int)(8 + (coolantTemp - 40) * 642 / 80);
            coolantTempPanel.Width = width;

            // Update warning light if overheating
            bool overheat = coolantTemp > 100;
            if (overheat != lastOverheatState)
            {
                serialManager.WriteLine($"ENGINE_ERROR_LIGHT_{(overheat ? "ON" : "OFF")}");
            }
        }

        private void UpdateFuel()
        {
            if (omsiManager.CurrentVehicle == null) return;

            double fuelLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_tank_content"));
            double adblueLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_adblue_content"));

            fuelLevel = Math.Clamp(fuelLevel, 0, 1);
            int widthfuel = (int)(8 + fuelLevel * 638);
            fuelPanel.Width = widthfuel;

            adblueLevel = Math.Clamp(adblueLevel, 0, 1);
            int widthadblue = (int)(8 + adblueLevel * 638);
            adbluePanel.Width = widthadblue;

            // Update low fuel warning light only if state changes
            bool lowFuel = fuelLevel < 0.2;
            ms_fuel.Visible = lowFuel;
            if (lowFuel != lastLowFuelState)
            {
                serialManager.WriteLine($"WARNING_LIGHT_{(lowFuel ? "ON" : "OFF")}");
                lastLowFuelState = lowFuel;
            }
        }

        private async Task SimulatePress(Action<bool> action)
        {
            action(true);  // Press
            await Task.Delay(200);  // Hold for 200 ms
            action(false); // Release
        }

        private void UpdateClusterLights()
        {
            if (serialManager == null || omsiManager.CurrentVehicle == null) 
                return;


            bool nightLight = omsiManager.GetLightSwitch() >0 ;
            if (nightLight != lastNightLightState)
            {
                serialManager.WriteLine($"NIGHT_LIGHT_{(nightLight ? "ON" : "OFF")}");
                lastNightLightState = nightLight;

                for (int i = 0; i < 3; i++)
                    serialManager.WriteLine($"DOOR{i + 1}_{(nightLight ? "DIM" : "OFF")}");
            }

            bool lowBeam = omsiManager.GetLightSwitch() > 0.5f;
            if (lowBeam != lastLowBeamState)
            {
                serialManager.WriteLine($"LOW_BEAM_LIGHT_{(lowBeam ? "ON" : "OFF")}");
                lastLowBeamState = lowBeam;
            }

            bool checkEngineWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_motor") > 0;
            if (checkEngineWarning != lastCheckEngineWarningState)
            {
                serialManager.WriteLine($"ENGINE_ERROR_LIGHT_{(checkEngineWarning ? "ON" : "OFF")}");
                lastCheckEngineWarningState = checkEngineWarning;
            }

            bool parkingBrake = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_parkingbrake") > 0;
            if (parkingBrake != lastParkingBrakeState)
            {
                serialManager.WriteLine($"PARKING_BRAKE_LIGHT_{(parkingBrake ? "ON" : "OFF")}");
                lastParkingBrakeState = parkingBrake;
            }

            bool absWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_ABS") > 0;
            if (absWarning != lastAbsWarningState)
            {
                serialManager.WriteLine($"ABS_LIGHT_{(absWarning ? "ON" : "OFF")}");
                lastAbsWarningState = absWarning;
            }

            bool diagWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_WARNING") > 0;
            if (diagWarning != lastDiagWarningState)
            {
                serialManager.WriteLine($"WARNING_LIGHT_{(diagWarning ? "ON" : "OFF")}");
                lastDiagWarningState = diagWarning;
            }

            bool busStop = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_haltewunsch") > 0;
            if (busStop != lastbusStopState)
            {
                serialManager.WriteLine($"BUS_STOP_LIGHT_{(busStop ? "ON" : "OFF")}");
                lastbusStopState = busStop;
            }


            bool STOPWarning = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_STOP") > 0;
            if (STOPWarning != lastSTOPState)
            {
                serialManager.WriteLine($"STOP_LIGHT_{(STOPWarning ? "ON" : "OFF")}");
                lastSTOPState = STOPWarning;
            }

            bool highBeams = omsiManager.CurrentVehicle.GetVariable("Actia_DASH_Indic_HighBeam") > 0;
            if (highBeams != lastHighBeamState)
            {
                serialManager.WriteLine($"HIGH_BEAM_LIGHT_{(highBeams ? "ON" : "OFF")}");
                lastHighBeamState = highBeams;
            }
        }

        public async Task HandleSerialInput(string input)
        {
            if (isInitializing || omsiManager.CurrentVehicle == null) 
                return;

            switch (input)
            {
                // Electrical system
                case "ELECTRICITY_ON":
                    omsiManager.SetMasterSwitch(true);
                    break;
                case "ELECTRICITY_OFF":
                    omsiManager.SetMasterSwitch(false);
                    break;

                // Engine controls
                case "ENGINE_START":
                    omsiManager.SetStarter(true);
                    break;
                case "ENGINE_START_RELEASED":
                    omsiManager.SetStarter(false);
                    break;
                case "ENGINE_STOP":
                    omsiManager.SetEngineShutdown(true);
                    break;
                case "ENGINE_STOP_RELEASED":
                    omsiManager.SetEngineShutdown(false);
                    break;

                // Lighting system
                case "LOW_BEAM_ON":
                    if(input != lastSerialCommand)
                        omsiManager.SetLightSwitch(1);
                    break;
                case "LOW_BEAM_OFF":
                    if (input != lastSerialCommand)
                        omsiManager.SetLightSwitch(0.5f);
                    break;
                case "DAY_LIGHTS_ON":
                    if (input != lastSerialCommand)
                        omsiManager.SetLightSwitch(0.5f);
                    break;
                case "DAY_LIGHTS_OFF":
                    if (input != lastSerialCommand)
                        omsiManager.SetLightSwitch(0);
                    break;
                /*case "HIGH_BEAM_ON":
                    omsiManager.SetHighBeamState(true);
                    break;
                case "HIGH_BEAM_OFF":
                    omsiManager.SetHighBeamState(false);
                    break;
                case "FRONT_FOG_ON":
                    omsiManager.SetFrontFog(true);
                    break;
                case "FRONT_FOG_OFF":
                    omsiManager.SetFrontFog(false);
                    break;
                case "BACK_FOG_ON":
                    omsiManager.SetRearFog(true);
                    break;
                case "BACK_FOG_OFF":
                    omsiManager.SetRearFog(false);
                    break;*/

                // Blinkers and hazards
                /*case "BLINKER_LEFT_ON":
                    omsiManager.SetBlinkerState(1);
                    break;
                case "BLINKER_RIGHT_ON":
                    omsiManager.SetBlinkerState(2);
                    break;
                case "BLINKER_OFF":
                    omsiManager.SetBlinkerState(0);
                    break;*/
                case "HAZARDS_ON":
                    omsiManager.SetHazards(true);
                    break;
                case "HAZARDS_OFF":
                    omsiManager.SetHazards(false);
                    break;

                // Horn
                case "HORN_ON":
                    omsiManager.SetHornState(true);
                    break;
                case "HORN_OFF":
                    omsiManager.SetHornState(false);
                    break;

                // Gear selection
                case "GEAR_REVERSE":
                    omsiManager.SetGear(-1);
                    break;
                case "GEAR_NEUTRAL":
                    omsiManager.SetGear(0);
                    break;
                case "GEAR_DRIVE":
                    omsiManager.SetGear(1);
                    break;

                // Door controls
                case "DOOR_1_PRESSED":
                    omsiManager.SetDoorButton(0, true);
                    break;
                case "DOOR_2_PRESSED":
                    omsiManager.SetDoorButton(1, true);
                    break;
                case "DOOR_3_PRESSED":
                    omsiManager.SetDoorButton(2, true);
                    break;
                case "AUTO_DOOR_ON":
                    omsiManager.SetAutoDoors(true);
                    break;
                case "AUTO_DOOR_OFF":
                    omsiManager.SetAutoDoors(false);
                    break;

                // Special functions
                case "HEATING_PRESSED":
                    omsiManager.SetHeating(true);
                    break;
                case "RETARDER_OFF_ON":
                    omsiManager.SetRetarderOff(true);
                    break;
                case "RETARDER_OFF_OFF":
                    omsiManager.SetRetarderOff(false);
                    break;
                case "SUSPENSION_UP_PRESSED":
                    omsiManager.SetSuspensionUp(true);
                    break;
                case "SUSPENSION_UP_RELEASED":
                    omsiManager.SetSuspensionUp(false);
                    break;
                case "SUSPENSION_DOWN_PRESSED":
                    omsiManager.SetSuspensionDown(true);
                    break;
                case "SUSPENSION_DOWN_RELEASED":
                    omsiManager.SetSuspensionDown(false);
                    break;
                /*case "STROLLER_PRESSED":
                    omsiManager.ToggleStrollerMode();
                    break;*/
                case "DEV_MODE_PRESSED":
                    omsiManager.SetDevMode(true);
                    break;
                case "STATION_STOP_ON":
                    omsiManager.SetStopBrake(true);
                    break;
                case "STATION_STOP_OFF":
                    omsiManager.SetStopBrake(false);
                    break;
                // Actia display controls
                case "SCREEN_UP_PRESSED":
                    omsiManager.SetActiaScreenUp(true);
                    break;
                case "SCREEN_DOWN_PRESSED":
                    omsiManager.SetActiaScreenDown(true);
                    break;

                // Interior lights
                case "DRIVER_LIGHT_BTN_ON":
                    omsiManager.SetDriverLight(true);
                    break;
                case "DRIVER_LIGHT_BTN_OFF":
                    omsiManager.SetDriverLight(false);
                    break;
                case "INTERIOR_LIGHTS_BTN_ON":
                    omsiManager.SetPassengerLight(true);
                    break;
                case "INTERIOR_LIGHTS_BTN_OFF":
                    omsiManager.SetPassengerLight(false);
                    break;
            }
            lastSerialCommand = input;
        }

        private void HandleVehicleChanged(OmsiRoadVehicleInst vehicle)
        {
            // Reset states when vehicle changes
            dashboardOn = false;
            logoSequenceRunning = false;
            lastBlinkerState = 0;
            lastNightLightState = false;
            lastLowBeamState = false;
            lastArduinoBlinkerState = 0;
            lastElecState = false;
            lastHazardsState = false;
            lastGearState = 0;
            lastRetarderState = false;
            lastAutoDoorState = false;
            lastCheckEngineWarningState = false;
            lastParkingBrakeState = false;
            lastAbsWarningState = false;
            lastDiagWarningState = false;
            lastbusStopState = false;
            lastSTOPState = false;
            lastHighBeamState = false;
            lastOverheatState = false;
            lastLowFuelState = false;

            Array.Fill(lastDoorStates, false);

            // Reset all dashboard lights
            for (int i = 0; i < 3; i++)
            {
                serialManager.WriteLine($"DOOR_LIGHT_{i}:OFF");
            }

            serialManager.WriteLine("LEFT_BLINKER_LIGHT_OFF");
            serialManager.WriteLine("RIGHT_BLINKER_LIGHT_OFF");
            serialManager.WriteLine("HAZARDS_LIGHT_OFF");
            serialManager.WriteLine("LOW_BEAM_LIGHT_OFF");
            serialManager.WriteLine("HIGH_BEAM_LIGHT_OFF");
            serialManager.WriteLine("CHECK_ENGINE_LIGHT_OFF");
            serialManager.WriteLine("ENGINE_ERROR_LIGHT_OFF");
            serialManager.WriteLine("BRAKE_ERROR_LIGHT_OFF");
            serialManager.WriteLine("PARKING_BRAKE_LIGHT_OFF");
            serialManager.WriteLine("ABS_LIGHT_OFF");
            serialManager.WriteLine("WARNING_LIGHT_OFF");
            serialManager.WriteLine("NIGHT_LIGHT_OFF");
            serialManager.WriteLine("REVERSE_LIGHT_OFF");
            serialManager.WriteLine("NEUTRAL_LIGHT_OFF");
            serialManager.WriteLine("DRIVE_LIGHT_OFF");

            // Reset gauges
            serialManager.WriteLine("SPEED:0");
            serialManager.WriteLine("RPM:0");
        }
    }
}