using OmsiHook;

using System;
using System.Threading.Tasks;

namespace OmsiVisualInterfaceNet
{
    public class OmsiManager : IDisposable
    {
        private readonly OmsiHook.OmsiHook omsi;
        private readonly SerialManager serialManager;
        private readonly string logPath;
        private readonly bool enableLogging;

        public event Action<OmsiRoadVehicleInst> OnVehicleChanged;
        public OmsiRoadVehicleInst CurrentVehicle { get; private set; }
        private bool lastElecState;
        private int lastBlinkerState;
        private bool lastHazardsState;
        private int lastGearState;
        private bool lastnightLightState;
        private bool nightLightState;
        private bool[] lastDoorStates = new bool[3];

        public OmsiManager(SerialManager SerialManager, bool EnableLogging)
        {
            serialManager = SerialManager;
            enableLogging = EnableLogging;
            omsi = new OmsiHook.OmsiHook();

            // Setup logging
            if (enableLogging)
            {
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serial_comms.log");
                LogMessage("=== Session Started ===");
            }

            for (int i = 0; i < 3; i++)
            {
                bool doorOpen = GetDoorState(i);
                lastDoorStates[i] = doorOpen;
            }
        }

        private void LogMessage(string message)
        {
            if (!enableLogging) return;

            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }

        public async Task Initialize()
        {
            await omsi.AttachToOMSI();
            omsi.OnActiveVehicleChanged += (s, v) =>
            {
                CurrentVehicle = v;
                OnVehicleChanged?.Invoke(v);
                LogMessage($"Vehicle changed: {v?.ToString() ?? "null"}");
            };
            CurrentVehicle = omsi.Globals.PlayerVehicle;
            OnVehicleChanged?.Invoke(CurrentVehicle);
        }

        public bool GetNightLightsState()
        {
            return nightLightState;
        }

        public void Update()
        {
            if (CurrentVehicle == null) return;

            if(nightLightState !=lastnightLightState)
            {
                serialManager.WriteLine($"NIGHT_LIGHT_{(nightLightState ? "ON" : "OFF")}");
            }

            var blinkerState = GetBlinkerState();
            if (blinkerState != lastBlinkerState)
            {
                LogMessage($"Blinker state changed: {lastBlinkerState} -> {blinkerState}");
                if (blinkerState == 1)
                {
                    serialManager.WriteLine("LEFT_BLINKER_ON");
                    serialManager.WriteLine("RIGHT_BLINKER_OFF");
                }
                    
                else if (blinkerState == 2)
                {
                    serialManager.WriteLine("LEFT_BLINKER_OFF");
                    serialManager.WriteLine("RIGHT_BLINKER_ON");
                }
                else
                {
                    serialManager.WriteLine("LEFT_BLINKER_OFF");
                    serialManager.WriteLine("RIGHT_BLINKER_OFF");
                }
                lastBlinkerState = blinkerState;
            }

            var hazardsState = GetHazardLightsState();
            if (hazardsState != lastHazardsState)
            {
                LogMessage($"Hazards state changed: {lastHazardsState} -> {hazardsState}");
                serialManager.WriteLine($"HAZARDS_BTN_{(hazardsState ? "ON" : "OFF")}");
                lastHazardsState = hazardsState;
            }

            var gearState = GetGearState();
            if (gearState != lastGearState)
            {
                LogMessage($"Gear state changed: {lastGearState} -> {gearState}");
                switch (gearState)
                {
                    case -1:
                        serialManager.WriteLine("GEAR_REVERSE");
                        break;
                    case 0:
                        serialManager.WriteLine("GEAR_NEUTRAL");
                        break;
                    case 1:
                        serialManager.WriteLine("GEAR_DRIVE");
                        break;
                }
                lastGearState = gearState;
            }

            for (int i = 0; i < 3; i++)
            {
                bool doorOpen = GetDoorState(i);
                if (doorOpen != lastDoorStates[i])
                {
                    LogMessage($"Door {i} state changed: {lastDoorStates[i]} -> {doorOpen}");

                    if (doorOpen)
                    {
                        serialManager.WriteLine($"DOOR{i+1}_ON");
                    }
                    else
                    {
                        bool headlightsOn = nightLightState;
                        if (headlightsOn)
                            serialManager.WriteLine($"DOOR{i+1}_DIM");
                        else
                            serialManager.WriteLine($"DOOR{i+1}_OFF");
                    }

                    lastDoorStates[i] = doorOpen;
                }
            }
        }


        public double GetSpeed() => CurrentVehicle?.Tacho ?? 0;
        public double GetRPM() => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable("engine_n"));
        public bool GetElectricState() => CurrentVehicle == null ? false : Convert.ToBoolean(CurrentVehicle.GetVariable("elec_busbar_main"));
        public int GetBlinkerState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("lights_sw_blinker"));
        public bool GetHazardLightsState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_sw_warnblinker"));
        public bool GetDoorState(int door)
        {
            if (CurrentVehicle == null)
                return false;

            if (door == 0)
            {
                if (Convert.ToBoolean(CurrentVehicle.GetVariable($"door_0")) || Convert.ToBoolean(CurrentVehicle.GetVariable($"door_1")))
                    return true;
            }
            else if (door == 1)
            {
                if (Convert.ToBoolean(CurrentVehicle.GetVariable($"door_2")) && Convert.ToBoolean(CurrentVehicle.GetVariable($"door_3")))
                    return true;
            }
            else if (door == 2)
            {
                if (Convert.ToBoolean(CurrentVehicle.GetVariable($"door_4")) && Convert.ToBoolean(CurrentVehicle.GetVariable($"door_5")))
                    return true;
            }
            return false;
        }
        public int GetLightSwitch() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle?.GetVariable("cp_light_sw"));
        public int GetGearState() => CurrentVehicle == null ? 0 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gangR")) ? -1 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gang1")) ? 1 : 0;

        public double GetVariable(string v) => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable(v));

        public void SetMasterSwitch(bool on) => CurrentVehicle?.SetVariable("elec_busbar_main_sw", on ? 1 : 0);
        public void SetStarter(bool pressed) => CurrentVehicle?.SetTrigger("kw_m_engine_startbutton", pressed ? true : false);
        
        public void SetEngineShutdown(bool pressed) => CurrentVehicle?.SetTrigger("kw_m_engineshutdown", pressed ? true : false);
        public void SetHeating(bool on) => CurrentVehicle?.SetVariable("cp_heizluefter_sw", on ? 1 : 0);
        public void SetRetarderOff(bool on) => CurrentVehicle?.SetVariable("cp_retarder_sw_disable", on ? 1 : 0);
        public void SetHazards(bool on) => CurrentVehicle?.SetVariable("lights_sw_warnblinker", on ? 1 : 0);
        public void SetActiaScreenUp(bool pressed) => CurrentVehicle?.SetVariable("Actia_BTN_chmod_up", pressed ? 1 : 0);
        public void SetActiaScreenDown(bool pressed) => CurrentVehicle?.SetVariable("Actia_BTN_chmod_down", pressed ? 1 : 0);
        public void SetDriverLight(bool on) => CurrentVehicle?.SetVariable("cp_fahrerlicht_sw", on ? 1 : 0);
        public void SetPassengerLight(bool on) => CurrentVehicle?.SetVariable("cp_licht_unterdeck_sw", on ? 1 : 0);
        public void SetSuspensionUp(bool pressed) => CurrentVehicle?.SetVariable("cp_hub_up_sw", pressed ? 1 : 0);
        public void SetSuspensionDown(bool pressed) => CurrentVehicle?.SetVariable("cp_hub_dn_sw", pressed ? 1 : 0);
        public void SetStrollerMode(bool on) => CurrentVehicle?.SetVariable("cockpit_light_kinderwagenwunsch", on ? 1 : 0);
        public void SetDevMode(bool on) => CurrentVehicle?.SetTrigger("CG_active", on ? true : false);
        public void SetAutoDoors(bool on) => CurrentVehicle?.SetVariable("door_DISABLED_Req", on ? 0 : 1);
        public void SetPressurization(bool on) => CurrentVehicle?.SetVariable("pressurizePin", on ? 1 : 0);
        public void SetStopBrake(bool on) => CurrentVehicle?.SetTrigger("bus_dooraft", on ? true : false);

        public void ActivateDriveTrigger()
        {
            CurrentVehicle?.SetTrigger("automatic_D", true);
        }

        public void SetDoorButton(int door, bool pressed)
        {
            if (CurrentVehicle == null) return;

            LogMessage($"Setting door {door} button: {pressed}");

            bool doorOpen = GetDoorState(door);
            string triggerVar;
            string targetVar;

            if (door == 0)
            {
                CurrentVehicle?.SetTrigger(doorOpen ? "bus_doorfront0" : "bus_doorfront0_off", !doorOpen);
                CurrentVehicle?.SetVariable("doorTarget_0", doorOpen ? 0 : 1);
                CurrentVehicle?.SetTrigger(doorOpen ? "bus_doorfront1" : "bus_doorfront1_off", !doorOpen);
                CurrentVehicle?.SetVariable("doorTarget_1", doorOpen ? 0 : 1);
            }
            else if (door == 1)
            {
                CurrentVehicle?.SetTrigger(doorOpen ? "bus_doorfront23" : "bus_doorfront23_off", !doorOpen);
                CurrentVehicle?.SetVariable("doorTarget_23", doorOpen ? 0 : 1);
            }else if(door == 2)
            {
                CurrentVehicle?.SetTrigger(doorOpen ? "bus_doorfront45" : "bus_doorfront45_off", !doorOpen);
                CurrentVehicle?.SetVariable("doorTarget_45", doorOpen ? 0 : 1);
            }
            
        }
        public void SetGear(int pos)
        {
            if (CurrentVehicle == null) return;

            try
            {
                // Map gear position to transmission variables
                string gearVar = "antrieb_getr_gangwahl";
                float gearVal;

                switch (pos)
                {
                    case -1: // Reverse
                        gearVal = 0;
                        break;
                    case 0:  // Neutral 
                        gearVal = 1;
                        break;
                    case 1:  // Drive
                        gearVal = 4;
                        break;
                    default:
                        return;
                }

                CurrentVehicle.SetVariable(gearVar, gearVal);
            }
            catch (KeyNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("Gear variables not found in bus model");
            }
        }
        public void SetLightSwitch(int pos)
        {
            //var var1= CurrentVehicle?.GetVariable("cp_schluessel_rot");
            //CurrentVehicle?.SetVariable("lights_sw_fern ", pos);
            //var var2 = CurrentVehicle?.GetVariable("cp_schluessel_rot");

            if (pos == 0)
             {
                 CurrentVehicle?.SetTrigger("kw_standlicht_toggle", true);
                nightLightState = false;
             }
             else if (pos == 1)
             {
                 CurrentVehicle?.SetTrigger("kw_standlicht_toggle", true);
                nightLightState = true;
             }
             else if (pos == 2)
             {
                 CurrentVehicle?.SetTrigger("kw_scheinwerfer_toggle", true);
                nightLightState = true;
             }

            lastnightLightState = nightLightState;


        }
        public void SetHighBeam(bool on) => CurrentVehicle?.SetVariable("lights_fern", on ? 1 : 0);
        public void SetHornState(bool on) => CurrentVehicle?.SetVariable("cockpit_horn", on ? 1 : 0);
        public void SetWiper(string mode)
        {
            int val = mode == "INT" ? 3 : mode == "1" ? 4 : mode == "2" ? 5 : 6;
            CurrentVehicle?.SetVariable("windscreen_wiper", val);
        }

        public void Dispose() => omsi.Dispose();

    }
}