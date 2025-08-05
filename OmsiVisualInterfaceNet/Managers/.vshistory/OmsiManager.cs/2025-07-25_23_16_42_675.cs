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

        public OmsiManager()
        {
            this.serialManager = serialManager;
            this.enableLogging = enableLogging;
            omsi = new OmsiHook.OmsiHook();

            // Setup logging
            if (enableLogging)
            {
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serial_comms.log");
                LogMessage("=== Session Started ===");
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

        public void Update()
        {
            if (CurrentVehicle == null) return;

            var elecState = GetElectricState();
            if (elecState != lastElecState)
            {
                LogMessage($"Electric state changed: {lastElecState} -> {elecState}");
                CurrentVehicle?.SetVariable("elec_busbar_main_sw", elecState ? 1 : 0);
                lastElecState = elecState;
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
                serialManager.WriteLine($"HAZARDS:{(hazardsState ? "ON" : "OFF")}");
                lastHazardsState = hazardsState;
            }

            var gearState = GetGearState();
            if (gearState != lastGearState)
            {
                LogMessage($"Gear state changed: {lastGearState} -> {gearState}");
                switch (gearState)
                {
                    case -1:
                        serialManager.WriteLine("GEAR:R");
                        break;
                    case 0:
                        serialManager.WriteLine("GEAR:N");
                        break;
                    case 1:
                        serialManager.WriteLine("GEAR:D");
                        break;
                }
                lastGearState = gearState;
            }

            // Always update these since they change frequently
            serialManager.WriteLine($"SPEED:{GetSpeed():F0}");
            serialManager.WriteLine($"RPM:{GetRPM():F0}");
        }


        public double GetSpeed() => CurrentVehicle?.Tacho ?? 0;
        public double GetRPM() => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable("engine_n"));
        public bool GetElectricState() => CurrentVehicle == null ? false : Convert.ToBoolean(CurrentVehicle.GetVariable("elec_busbar_main"));
        public int GetBlinkerState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("lights_sw_blinker"));
        public bool GetHazardLightsState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_sw_warnblinker"));
        public bool GetDoorState(int door) => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable($"door_{door}"));
        public int GetLightSwitch() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle?.GetVariable("cp_light_sw"));
        public int GetGearState() => CurrentVehicle == null ? 0 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gangR")) ? -1 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gang1")) ? 1 : 0;

        public double GetVariable(string v) => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable(v));

        public void SetMasterSwitch(bool on) => CurrentVehicle?.SetVariable("elec_busbar_main_sw", on ? 1 : 0);
        public void SetStarter(bool pressed)
        {
            CurrentVehicle?.SetVariable("cp_taster_anlasser", pressed ? 1 : 0);
        }
        public void SetEngineShutdown(bool pressed) => CurrentVehicle?.SetVariable("cp_taster_motorabstellung", pressed ? 1 : 0);
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
            bool door0Open = GetDoorState(0);
            bool door1Open = GetDoorState(1);
            bool door2Open = GetDoorState(2);

            if (door == 0 && door0Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront0", door0Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_0", door0Open ? 0 : 1);
                CurrentVehicle?.SetTrigger($"bus_doorfront1", door0Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_1", door0Open ? 0 : 1);
            }
            else if (door == 0 && !door0Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront0_off", door0Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_0", door0Open ? 0 : 1);
                CurrentVehicle?.SetTrigger($"bus_doorfront1_off", door0Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_1", door0Open ? 0 : 1);
            }

            if (door == 1 && door1Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront23", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_23", door1Open ? 0 : 1);
                
            }
            else if (door == 1 && !door1Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront23_off", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_23", door1Open ? 0 : 1);
            }

            if (door == 2 && door2Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront45", door2Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_45", door2Open ? 0 : 1);

            }
            else if (door == 2 && !door2Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront45_off", door2Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_45", door2Open ? 0 : 1);
            }
        }

        public async Task SetDoorState(int door, bool open)
        {
            if (CurrentVehicle == null) return;

            string doorVar = $"bus_doorfront{door - 1}";
            CurrentVehicle?.SetTrigger(doorVar, true);
            await Task.Delay(100);
            //CurrentVehicle?.SetTrigger(doorVar, false);


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
            CurrentVehicle?.SetVariable("cp_light_sw", pos);
            // 1=parking, 2=low
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