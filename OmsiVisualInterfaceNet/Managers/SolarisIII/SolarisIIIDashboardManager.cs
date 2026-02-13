using OmsiHook;

using System.Diagnostics;

namespace OmsiVisualInterfaceNet.Managers.SolarisIII
{
    public class SolarisIIIDashboardManager
    {
        private readonly SerialManager serialManager;
        private readonly SolarisIIIOmsiManager omsiManager;
        private readonly SolarisIIIScreenManager screenManager;
        private readonly ConstantsManager constantsManager;
        private bool dashboardOn = false;
        private bool engineOn = false;
        private readonly Form form;
        private readonly Panel mainScreen;
        private readonly PictureBox pb_fuel;
        private readonly PictureBox pb_adBlue;
        private readonly PictureBox pb_luftTank1;
        private readonly PictureBox pb_luftTank2;
        private readonly PictureBox pb_luftBremse1;
        private readonly PictureBox pb_luftBremse2;
        private readonly PictureBox pb_dpf;
        private readonly PictureBox pb_temp;
        private PictureBox pb_door0;
        private PictureBox pb_door1;
        private PictureBox pb_door2;
        private PictureBox pb_door3;

        private string lastDoor0Image;
        private string lastDoor1Image;
        private string lastDoor2Image;
        private string lastDoor3Image;

        private bool lastElecState = false;
        private bool lastHazardsState = false;
        private bool lastHazardsLightState;
        private bool[] lastDoorStates = new bool[3];
        private int lastGearState = 0;
        private bool lastCheckEngineWarningState;
        private bool lastParkingBrakeState;
        private bool lastAbsWarningState;
        private bool lastDiagWarningState;
        private bool lastbusStopState;
        private bool lastSTOPState;
        private bool lastHighBeamState;
        private bool lastOverheatState;
        private int lastBlinkerState = 0;
        public bool lastNightLightState;
        private bool lastLowBeamState;
        private bool lastLowFuelState;
        private bool buzzerOn;
        private string lastSerialCommand = null;

        public SolarisIIIDashboardManager(
            Form form,
            SerialManager serialManager,
            SolarisIIIOmsiManager omsiManager,
            SolarisIIIScreenManager screenManager,
            ConstantsManager constantsManager,
            Panel mainScreen,
            PictureBox pb_fuel,
            PictureBox pb_adBlue,
            PictureBox pb_luftTank1,
            PictureBox pb_luftTank2,
            PictureBox pb_luftBremse1,
            PictureBox pb_luftBremse2,
            PictureBox pb_dpf,
            PictureBox pb_temp,
            PictureBox pb_door0,
            PictureBox pb_door1,
            PictureBox pb_door2,
            PictureBox pb_door3)
        {
            this.form = form;
            this.mainScreen = mainScreen;
            this.serialManager = serialManager;
            this.omsiManager = omsiManager;
            this.screenManager = screenManager;
            this.constantsManager = constantsManager;
            this.pb_fuel = pb_fuel;
            this.pb_adBlue = pb_adBlue;
            this.pb_luftTank1 = pb_luftTank1;
            this.pb_luftTank2 = pb_luftTank2;
            this.pb_luftBremse1 = pb_luftBremse1;
            this.pb_luftBremse2 = pb_luftBremse2;
            this.pb_dpf = pb_dpf;
            this.pb_temp = pb_temp;
            this.pb_door0 = pb_door0;
            this.pb_door1 = pb_door1;
            this.pb_door2 = pb_door2;
            this.pb_door3 = pb_door3;

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
            buzzerOn = false;
        }

        private void UpdateSpeedometer(double speed)
        {
            if (speed >= 1)
            {
                int needleSteps = (int)(speed / 125.0 * 685);
                needleSteps = Math.Clamp(needleSteps, 0, 685);
                serialManager.WriteHighPriority($"SPEED:{needleSteps}");
            }
        }

        private void UpdateTachometer(double rpm)
        {
            if (rpm >= 1)
            {
                var rpmSteps = rpm / 3000.0 * 730.0;
                rpmSteps = Math.Clamp(rpmSteps, 0, 730);
                serialManager.WriteHighPriority($"RPM:{rpmSteps}");
                if (rpm > 300)
                    engineOn = true;
            }
            else
                engineOn = false;
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
            UpdateClusterLights();
            UpdateFuel();
            UpdatePressures();
            UpdateDpf();
            UpdateTemp();
            omsiManager.Update();
            form.TopMost = true;
            screenManager.UpdateMainScreenIcons();
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

        private void UpdateFuel()
        {
            if (omsiManager.CurrentVehicle == null) return;

            double fuelLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_tank_content"));
            double fuelPercent = (fuelLevel / omsiManager.fuelcapacity) * 100.0;
            double adBlueLevel = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("AdBlue_sensor"));
            double adBluePercent = (adBlueLevel / 40) * 100.0;


            screenManager.UpdateFuelImage(pb_fuel, fuelPercent);
            screenManager.UpdateAdBlueImage(pb_adBlue, adBluePercent);

        }

        private void UpdatePressures()
        {
            if (omsiManager.CurrentVehicle == null) return;

            double luft_tank1 = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_tank01_sensor"));
            double luft_tank2 = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_tank02_sensor"));
            double luft_tank1Value = luft_tank1 / 10000;
            double luft_tank2Value = luft_tank2 / 10000;

            double luft_bremse1 = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_Brzyl_VA"));
            double luft_bremse2 = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("bremse_p_Brzyl_HA"));
            double luft_bremse1Value = (luft_bremse1 - 100000) / 10000;
            double luft_bremse2Value = (luft_bremse2 - 100000) / 10000;

            screenManager.UpdateLuftTank1Image(pb_luftTank1, luft_tank1Value);
            screenManager.UpdateLuftTank2Image(pb_luftTank2, luft_tank2Value);

            screenManager.UpdateLuftBremse1Image(pb_luftBremse1, luft_bremse1Value);
            screenManager.UpdateLuftBremse2Image(pb_luftBremse2, luft_bremse2Value);
        }

        private void UpdateDpf()
        {
            if (omsiManager.CurrentVehicle == null) return;
            double dpf = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("Engine_DPF_state"));
            double dpfPercent = (dpf / 160) * 100.0;
            screenManager.UpdateDpfImage(pb_dpf, dpfPercent);
        }

        private void UpdateTemp()
        {
            if (omsiManager.CurrentVehicle == null) return;
            double temp = Convert.ToDouble(omsiManager.CurrentVehicle.GetVariable("engine_temperature"));
            screenManager.UpdateTempImage(pb_temp, temp);
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

        private void UpdateClusterLights()
        {
            if (serialManager == null || omsiManager.CurrentVehicle == null)
                return;

            bool nightLight = omsiManager.GetLightSwitch() > 0;
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

            bool checkEngineWarning = omsiManager.CurrentVehicle.GetVariable("indic_CheckEngine") > 0;
            if (checkEngineWarning != lastCheckEngineWarningState)
            {
                serialManager.WriteLine($"ENGINE_ERROR_LIGHT_{(checkEngineWarning ? "ON" : "OFF")}");
                lastCheckEngineWarningState = checkEngineWarning;
            }

            bool parkingBrake = omsiManager.CurrentVehicle.GetVariable("indic_nur_bremsdruck") > 0;
            if (parkingBrake != lastParkingBrakeState)
            {
                serialManager.WriteLine($"PARKING_BRAKE_LIGHT_{(parkingBrake ? "ON" : "OFF")}");
                screenManager.UpdateMainScreen();
                lastParkingBrakeState = parkingBrake;
            }

            bool absWarning = omsiManager.CurrentVehicle.GetVariable("indic_ABS_ASR") > 0;
            if (absWarning != lastAbsWarningState)
            {
                serialManager.WriteLine($"ABS_LIGHT_{(absWarning ? "ON" : "OFF")}");
                lastAbsWarningState = absWarning;
            }

            bool diagWarning = omsiManager.CurrentVehicle.GetVariable("indic_faillight") > 0;
            if (diagWarning != lastDiagWarningState)
            {
                serialManager.WriteLine($"WARNING_LIGHT_{(diagWarning ? "ON" : "OFF")}");
                lastDiagWarningState = diagWarning;
            }

            bool busStop = omsiManager.CurrentVehicle.GetVariable("indic_haltewunsch") > 0;
            if (busStop != lastbusStopState)
            {
                serialManager.WriteLine($"BUS_STOP_LIGHT_{(busStop ? "ON" : "OFF")}");
                lastbusStopState = busStop;
            }

            bool STOPWarning = omsiManager.CurrentVehicle.GetVariable("indic_masterlight") > 0;
            if (STOPWarning != lastSTOPState)
            {
                serialManager.WriteLine($"STOP_LIGHT_{(STOPWarning ? "ON" : "OFF")}");
                if (STOPWarning && engineOn)
                    serialManager.WriteLine("BUZZER_ON_900");
                else
                    serialManager.WriteLine("BUZZER_OFF");
                lastSTOPState = STOPWarning;
            }

            bool highBeams = omsiManager.CurrentVehicle.GetVariable("indic_lights_fern") > 0;
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
            if (omsiManager.CurrentVehicle == null)
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

                // Blinkers
                case "BLINKER_LEFT_ON":
                    omsiManager.SetBlinkers(true, false);
                    break;
                case "BLINKER_RIGHT_ON":
                    omsiManager.SetBlinkers(false, true);
                    break;
                case "BLINKER_LEFT_OFF":
                    omsiManager.SetBlinkers(false, false);
                    break;
                case "BLINKER_RIGHT_OFF":
                    omsiManager.SetBlinkers(false, false);
                    break;
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
                    omsiManager.SetGear(-1, true);
                    break;
                case "GEAR_REVERSE_RELEASED":
                    omsiManager.SetGear(-1, false);
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
                    screenManager.UpdateMainScreen();
                    break;
                case "DOOR_2_PRESSED":
                    omsiManager.SetDoorButton(1, true);
                    screenManager.UpdateMainScreen();
                    break;
                case "DOOR_3_PRESSED":
                    omsiManager.SetDoorButton(2, true);
                    screenManager.UpdateMainScreen();
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

                // Other functions
                case "HEATING_PRESSED":
                    omsiManager.SetHeating(true);
                    break;
                /*case "RETARDER_OFF_ON":
                    omsiManager.SetRetarderOff(true);
                    //screenManager.UpdateIcon("ms_retarderOff", true);
                    break;
                case "RETARDER_OFF_OFF":
                    omsiManager.SetRetarderOff(false);
                    //screenManager.UpdateIcon("ms_retarderOff", false);
                    break;*/
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
                case "DEV_MODE_PRESSED":
                    omsiManager.SetDevMode();
                    break;
                case "STATION_STOP_ON":
                    omsiManager.SetStopBrake(true);
                    screenManager.UpdateMainScreen();
                    break;
                case "STATION_STOP_OFF":
                    omsiManager.SetStopBrake(false);
                    screenManager.UpdateMainScreen();
                    break;
                // Actia display controls
                /*case "SCREEN_UP_PRESSED":
                    omsiManager.SetActiaScreenUp(true);
                    screenManager.ChangeMode(true);
                    break;
                case "SCREEN_DOWN_PRESSED":
                    omsiManager.SetActiaScreenDown(true);
                    screenManager.ChangeMode(false);
                    break;*/

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
            dashboardOn = false;
            lastBlinkerState = 0;
            lastNightLightState = false;
            lastLowBeamState = false;
            lastElecState = false;
            lastHazardsState = false;
            lastGearState = 0;
            lastCheckEngineWarningState = false;
            lastParkingBrakeState = false;
            lastAbsWarningState = false;
            lastDiagWarningState = false;
            lastbusStopState = false;
            lastSTOPState = false;
            lastHighBeamState = false;
            lastOverheatState = false;
            lastLowFuelState = false;
            engineOn = false;
            screenManager.startupTimerTicker.Stop();
            screenManager.HideAllScreens();
            Array.Fill(lastDoorStates, false);

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
            serialManager.WriteLine("STOP_LIGHT_OFF");
            serialManager.WriteLine("NIGHT_LIGHT_OFF");
            serialManager.WriteLine("REVERSE_LIGHT_OFF");
            serialManager.WriteLine("NEUTRAL_LIGHT_OFF");
            serialManager.WriteLine("DRIVE_LIGHT_OFF");
            serialManager.WriteLine("SPEED:0");
            serialManager.WriteLine("RPM:0");
        }
    }
}