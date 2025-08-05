using OmsiHook;

using OmsiVisualInterfaceNet.Managers;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OmsiVisualInterfaceNet
{
    public class DashboardManager
    {
        private readonly SerialManager serialManager;
        private readonly OmsiManager omsiManager;
        private readonly ScreenManager screenManager;
        private readonly ConstantsManager constantsManager;
        private bool dashboardOn = false;
        private bool logoSequenceRunning = false;
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
        private bool lastHazardsLightState;
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
        private bool lastLowAirPressureState;
        private string lastSerialCommand = null;

        public DashboardManager(
            SerialManager serialManager,
            OmsiManager omsiManager,
            ScreenManager screenManager,
            ConstantsManager constantsManager,
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
            this.screenManager = screenManager;
            this.constantsManager = constantsManager;
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
            lastHazardsLightState = false;
            lastCheckEngineWarningState = false;
            lastParkingBrakeState = false;
            lastAbsWarningState = false;
            lastDiagWarningState = false;
            lastbusStopState = false;
            lastSTOPState = false;
            lastHighBeamState = false;
            lastOverheatState = false;
            lastLowFuelState = false;
            lastLowAirPressureState = false;
        }

        private void UpdateSpeedometer(double speed)
        {
            if (speed >= 1)
            {
                int needleSteps = (int)((speed / 125.0) * 685);
                needleSteps = Math.Clamp(needleSteps, 0, 685);
                serialManager.WriteHighPriority($"SPEED:{needleSteps}");
            }
        }

        private void UpdateTachometer(double rpm)
        {
            if (rpm >= 1)
            {
                var rpmSteps = (rpm / 3000.0 * 730.0);
                rpmSteps = Math.Clamp(rpmSteps, 0, 730);
                serialManager.WriteHighPriority($"RPM:{rpmSteps}");
            }
        }

        public void UpdateCritical()
        {
            UpdateSpeedometer(omsiManager.GetSpeed());
            UpdateTachometer(omsiManager.GetRPM());
        }

        public void UpdateNonCritical()
        {
            UpdatePowerState(omsiManager.GetElectricState());
            UpdateBlinkerState();
            UpdateHazardLights();
            UpdateGearIndicator();
            UpdateCoolantTemperature();
            UpdateFuel();
            UpdateAirPressure();
            UpdateClusterLights();
            
        }


        private void UpdatePowerState(bool elecStatus)
        {
            if (elecStatus == true && !dashboardOn)
            {
                ShowLogoAndSwitchToStopScreen();
                Initialize();
            }
            else if (elecStatus == false && dashboardOn)
            {
                serialManager.WriteLine("RPM:0");
                serialManager.WriteLine("SPEED:0");
                dashboardOn = false;
                HandleVehicleChanged(omsiManager.CurrentVehicle);
            }
        }

        private void ShowLogoAndSwitchToStopScreen()
        {
            screenManager.RestartStartupSequence();
            dashboardOn = true;
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

        private void UpdateAirPressure()
        {
            if (omsiManager.CurrentVehicle == null) return;

            double air1Level = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_Tank01"));
            double air2Level = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_Tank02"));
           
            const int minWidth = 8;
            const int maxWidth = 638;
            const double maxPressure = 12000;

            air1Level = Math.Clamp(air1Level, 0, maxPressure);
            double ratio = air1Level / maxPressure; // 0.0 to 1.0
            int widthAir1 = (int)(minWidth + ratio * (maxWidth - minWidth));
            fuelPanel.Width = widthAir1;

            air2Level = Math.Clamp(air2Level, 0, maxPressure);
            ratio = air2Level / maxPressure; // 0.0 to 1.0
            int widthAir2 = (int)(minWidth + ratio * (maxWidth - minWidth));
            fuelPanel.Width = widthAir2;

            // Update low fuel warning light only if state changes
            bool lowair = air1Level < 4000 || air2Level < 4000;
            if (lowair != lastLowAirPressureState)
            {
                serialManager.WriteLine($"STOP_LIGHT_{(lowair ? "ON" : "OFF")}");
                lastLowAirPressureState = lowair;
            }
        }

        private void UpdateFuel()
        {
            if (omsiManager.CurrentVehicle == null) return;

            double fuelLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_tank_content"));
            double fuelcapacity = Convert.ToDouble(omsiManager.fuelcapacity);
            double adblueLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_adblue_content"));

            const int minWidth = 8;
            const int maxWidth = 638;

            fuelLevel = Math.Clamp(fuelLevel, 0, fuelcapacity);
            double ratio = fuelLevel / fuelcapacity; // 0.0 to 1.0
            int widthfuel = (int)(minWidth + ratio * (maxWidth - minWidth));
            fuelPanel.Width = widthfuel;

            adblueLevel = Math.Clamp(adblueLevel, 0, 1);
            int widthadblue = (int)(minWidth + adblueLevel * (maxWidth - minWidth));
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

            bool hazardsLight = omsiManager.GetElectricState();
            if (hazardsLight != lastHazardsLightState)
            {
                serialManager.WriteLine($"HAZARDS_LIGHT_{(hazardsLight ? "ON" : "OFF")}");
                lastHazardsLightState = hazardsLight;
            }
        }

        public async Task HandleSerialInput(string input)
        {
            if ( omsiManager.CurrentVehicle == null) 
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
                    omsiManager.SetLightSwitch(1);
                    break;
                case "LOW_BEAM_OFF":
                    omsiManager.SetLightSwitch(0.5f);
                    break;
                case "DAY_LIGHTS_ON":
                    omsiManager.SetLightSwitch(0.5f);
                    break;
                case "DAY_LIGHTS_OFF":
                    omsiManager.SetLightSwitch(0);
                    break;
                case "HIGH_BEAM_ON":
                    omsiManager.SetHighBeam(true);
                    break;
                case "HIGH_BEAM_OFF":
                    omsiManager.SetHighBeam(false);
                    break;
                case "FRONT_FOG_ON":
                    omsiManager.SetFrontFog(true);
                    break;
                case "FRONT_FOG_OFF":
                    omsiManager.SetFrontFog(false);
                    break;
                case "BACK_FOG_ON":
                    omsiManager.SetBackFog(true);
                    break;
                case "BACK_FOG_OFF":
                    omsiManager.SetBackFog(false);
                    break;

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
                case "HAZARDS_BTN_ON":
                    omsiManager.SetHazards(true);
                    break;
                case "HAZARDS_BTN_OFF":
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
                    omsiManager.SetGear(-1,true);
                    break;
                case "GEAR_REVERSE_RELEASED":
                    omsiManager.SetGear(-1,false);
                    break;
                case "GEAR_NEUTRAL":
                    omsiManager.SetGear(0, true);
                    break;
                case "GEAR_NEUTRAL_RELEASED":
                    omsiManager.SetGear(0, false);
                    break;
                case "GEAR_DRIVE":
                    omsiManager.SetGear(1, true);
                    break;
                case "GEAR_DRIVE_RELEASED":
                    omsiManager.SetGear(1, false);
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
                case "HALF_DOOR_OFF":
                    omsiManager.SetHalfDoor(false);
                    break;
                case "HALF_DOOR_ON":
                    omsiManager.SetHalfDoor(true);
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
                    omsiManager.SetDevMode();
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
                    screenManager.ChangeMode(true);
                    break;
                case "SCREEN_DOWN_PRESSED":
                    omsiManager.SetActiaScreenDown(true);
                    screenManager.ChangeMode(false);
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
            screenManager.startupTimerTicker.Stop();
            screenManager.HideAllScreens();


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