using OmsiHook;

using System.Diagnostics;

namespace OmsiVisualInterfaceNet.Managers.SolarisIII
{
    public class SolarisIIIOmsiManager : IDisposable, IOmsiManager
    {
        private readonly OmsiHook.OmsiHook omsi;
        private readonly SerialManager serialManager;
        private readonly ConstantsManager constantsManager;
        private readonly string logPath;
        private readonly bool enableLogging;

        // Centralized Solaris-specific variable/trigger mapping.
        private readonly Dictionary<string, string> variableMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // core variables
            { "engine_n", "engine_n" },
            { "elec_busbar_main", "elec_busbar_main" },
            { "elec_busbar_main_sw", "elec_busbar_main_sw" },
            { "lights_sw_blinker", "lights_sw_blinker" },
            { "lights_sw_warnblinker", "lights_sw_warnblinker" },
            { "cp_schluessel_rot", "cp_schluessel_rot" },
            { "engine_tank_content", "engine_tank_content" },
            { "engine_tank_capacity", "engine_tank_capacity" },
            { "engine_temperature", "engine_temperature" },
            { "engine_adblue_content", "engine_adblue_content" },
            { "bremse_p_Tank01", "bremse_p_Tank01" },
            { "bremse_p_Tank02", "bremse_p_Tank02" },

            // gear indicators
            { "cockpit_gangR", "cockpit_gangR" },
            { "cockpit_gang1", "cockpit_gang1" },

            // doors
            { "door_0", "door_0" },
            { "door_1", "door_1" },
            { "door_2", "door_2" },
            { "door_3", "door_3" },
            { "door_4", "door_4" },
            { "door_5", "door_5" },

            // actia / dashboard
            { "Actia_DASH_Indic_parkingbrake", "Actia_DASH_Indic_parkingbrake" },
            { "Actia_DASH_Indic_motor", "Actia_DASH_Indic_motor" },
            { "Actia_DASH_Indic_ABS", "Actia_DASH_Indic_ABS" },
            { "Actia_DASH_Indic_WARNING", "Actia_DASH_Indic_WARNING" },
            { "Actia_DASH_Indic_haltewunsch", "Actia_DASH_Indic_haltewunsch" },
            { "Actia_DASH_Indic_STOP", "Actia_DASH_Indic_STOP" },
            { "Actia_BTN_door0_block", "Actia_BTN_door0_block" },

            // triggers
            { "kw_m_engine_startbutton", "kw_m_engine_startbutton" },
            { "kw_m_engineshutdown", "kw_m_engineshutdown" },
            { "bus_dooraft", "bus_dooraft" },
            { "bus_doorfront0", "bus_doorfront0" },
            { "bus_doorfront0_off", "bus_doorfront0_off" },
            { "bus_doorfront1", "bus_doorfront1" },
            { "bus_doorfront1_off", "bus_doorfront1_off" },
            { "bus_doorfront2", "bus_doorfront2" },
            { "bus_doorfront2_off", "bus_doorfront2_off" },
            { "automatic_R", "automatic_R" },
            { "automatic_R_off", "automatic_R_off" },
            { "automatic_N", "automatic_N" },
            { "automatic_N_off", "automatic_N_off" },
            { "automatic_D", "automatic_D" },
            { "automatic_D_off", "automatic_D_off" },
            { "Actia_Dash_changemode_up", "Actia_Dash_changemode_up" },
            { "Actia_Dash_changemode_up_off", "Actia_Dash_changemode_up_off" },
            { "Actia_Dash_changemode_down", "Actia_Dash_changemode_down" },
            { "Actia_Dash_changemode_down_off", "Actia_Dash_changemode_down_off" },
            { "cp_kneel_up_toggle", "cp_kneel_up_toggle" },
            { "cp_kneel_down_toggle", "cp_kneel_down_toggle" },
            { "CG_active", "CG_active" },
            { "kw_m_engine_startbutton_off", "kw_m_engine_startbutton_off" } // placeholder
        };

        public event Action<OmsiRoadVehicleInst> OnVehicleChanged;
        public OmsiRoadVehicleInst CurrentVehicle { get; private set; }
        private bool lastElecState;
        private int lastBlinkerState;
        private bool lastHazardsState;
        private int lastGearState;
        private bool lastnightLightState;
        private bool lastHighBeamState = false;
        private bool nightLightState;
        private bool[] lastDoorStates = new bool[3];
        private bool stopBrakeState = false;
        private bool highBeamIntent = false;
        public string vehicleName { get; set; }
        public double fuelcapacity;

        public SolarisIIIOmsiManager(SerialManager SerialManager, bool EnableLogging)
        {
            serialManager = SerialManager;
            constantsManager = new ConstantsManager(this);
            enableLogging = EnableLogging;
            omsi = new OmsiHook.OmsiHook();

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

        // Map a logical name to the Solaris-specific name (fallback to provided name).
        private string Map(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return variableMap.TryGetValue(name, out var mapped) ? mapped : name;
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
            vehicleName = CurrentVehicle?.RoadVehicle?.FileName ?? "No vehicle";

            var cap = constantsManager.FindConstantValue("engine_tank_capacity");
            fuelcapacity = cap.HasValue ? Convert.ToDouble(cap.Value) : 1.0;
            OnVehicleChanged?.Invoke(CurrentVehicle);
        }

        public void Update()
        {
            if (CurrentVehicle == null) return;

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
                        serialManager.WriteLine($"DOOR{i + 1}_ON");
                    }
                    else
                    {
                        if (GetLightSwitch() > 0)
                            serialManager.WriteLine($"DOOR{i + 1}_DIM");
                        else
                            serialManager.WriteLine($"DOOR{i + 1}_OFF");
                    }

                    lastDoorStates[i] = doorOpen;
                }
            }

        }

        // Helper: read a mapped variable as double (safe)
        private double ReadDouble(string logical)
        {
            if (CurrentVehicle == null) return 0;
            var name = Map(logical);
            try
            {
                var val = CurrentVehicle.GetVariable(name);
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        // Helper: read a mapped variable as bool (safe)
        private bool ReadBool(string logical)
        {
            if (CurrentVehicle == null) return false;
            var name = Map(logical);
            try
            {
                var val = CurrentVehicle.GetVariable(name);
                return Convert.ToBoolean(val);
            }
            catch
            {
                return false;
            }
        }

        public double GetSpeed() => CurrentVehicle?.Tacho ?? 0;
        public double GetRPM() => ReadDouble("engine_n");
        public bool GetElectricState() => ReadBool("elec_busbar_main");
        public int GetBlinkerState()
        {
            if (CurrentVehicle == null) return 0;
            var val = ReadDouble("lights_sw_blinker");
            return Convert.ToInt32(val);
        }
        public bool GetHazardLightsState() => ReadBool("lights_sw_warnblinker");

        public bool GetDoorState(int door)
        {
            if (CurrentVehicle == null)
                return false;

            if (door == 0)
            {
                if (ReadBool("door_0") || ReadBool("door_1"))
                    return true;
            }
            else if (door == 1)
            {
                if (ReadBool("door_2") && ReadBool("door_3"))
                    return true;
            }
            else if (door == 2)
            {
                if (ReadBool("door_4") && ReadBool("door_5"))
                    return true;
            }
            return false;
        }
        public float GetLightSwitch()
        {
            if (CurrentVehicle == null)
                return 0;

            try
            {
                var val = CurrentVehicle.GetVariable(Map("cp_schluessel_rot"));
                return Convert.ToSingle(val);
            }
            catch
            {
                return 0;
            }
        }

        public int GetMainScreen()
        {
            var page = Convert.ToInt32(CurrentVehicle.GetVariable("vdv_display_mode"));
            Debug.WriteLine("PAGE" + page);

            return page;
        }
        public int GetGearState() => CurrentVehicle == null ? 0 :
            ReadBool("cockpit_gangR") ? -1 :
            ReadBool("cockpit_gang1") ? 1 : 0;

        public double GetHalfDoor() => ReadDouble("tuersperre_sw");

        public double GetVariable(string v) => ReadDouble(v);

        // Set helpers: use mapped names
        private void SetVar(string logical, float value) => CurrentVehicle?.SetVariable(Map(logical), value);
        private void SetTrig(string logical, bool state) => CurrentVehicle?.SetTrigger(Map(logical), state);

        public void SetMasterSwitch(bool on) => SetVar("elec_busbar_main_sw", on ? 1 : 0);
        public void SetStarter(bool pressed) => SetTrig("kw_m_engine_startbutton", pressed);
        public void SetEngineShutdown(bool pressed) => SetTrig("kw_m_engineshutdown", pressed);
        public void SetHeating(bool on) => SetTrig($"taster_standheizung_wabco{(on ? "" : "_off")}", true);
        public void SetRetarderOff(bool on) => SetVar("cp_retarder_sw_disable", on ? 1 : 0);
        public void SetHazards(bool on)
        {
            SetVar("lights_sw_warnblinker", on ? 1 : 0);
            SetVar("cp_taster_warnblinker", on ? 1 : 0);
        }
        public void SetActiaScreenUp(bool pressed)
        {
            SetTrig("Actia_Dash_changemode_up", true);
            SetTrig("Actia_Dash_changemode_up_off", true);
        }
        public void SetActiaScreenDown(bool pressed)
        {
            SetTrig("Actia_Dash_changemode_down", true);
            SetTrig("Actia_Dash_changemode_down_off", true);
        }
        public void SetDriverLight(bool on) => SetVar("cp_fahrerlicht_sw", on ? 1 : 0);
        public void SetPassengerLight(bool on) => SetVar("cp_licht_unterdeck_sw", on ? 1 : 0);
        public void SetSuspensionUp(bool pressed) => SetTrig($"cp_kneel_up_toggle{(pressed ? "" : "_off")}", true);
        public void SetSuspensionDown(bool pressed) => SetTrig($"cp_kneel_down_toggle{(pressed ? "" : "_off")}", true);
        public void SetStrollerMode(bool on) => SetVar("cockpit_light_kinderwagenwunsch", on ? 1 : 0);
        public void SetDevMode() => SetTrig("door_all", true);
        public void SetAutoDoors(bool on) => SetVar("CG_active", on ? 1 : 0);
        public void SetHalfDoor(bool on) => SetVar("tuersperre_sw", on ? -0.5f : 0.5f);

        public void SetPressurization(bool on) => SetVar("pressurizePin", on ? 1 : 0);
        public void SetStopBrake(bool on)
        {
            if (on == true)
                SetVar("bremse_halte_sw", 0);
            SetTrig("bus_20h-switch", true);
            stopBrakeState = on;
        }

        public void SetDoorButton(int door, bool pressed)
        {
            if (CurrentVehicle == null) return;

            LogMessage($"Setting door {door} button: {pressed}");

            if (door == 0)
            {
                SetTrig("bus_doorfront0", true);
                SetTrig("bus_doorfront0_off", true);
            }
            else if (door == 1)
            {
                SetTrig("bus_doorfront1", true);
                SetTrig("bus_doorfront1_off", true);
            }
            else if (door == 2)
            {
                SetTrig("bus_doorfront2", true);
                SetTrig("bus_doorfront2_off", true);
            }

        }
        public void SetGear(int pos, bool pressed)
        {
            if (CurrentVehicle == null) return;

            try
            {
                switch (pos)
                {
                    case -1:
                        SetTrig("automatic_R", pressed);
                        if (!pressed) SetTrig("automatic_R_off", true);
                        break;
                    case 0:
                        SetTrig("automatic_N", pressed);
                        if (!pressed) SetTrig("automatic_N_off", true);
                        break;
                    case 1:
                        SetTrig("automatic_D", pressed);
                        if (!pressed) SetTrig("automatic_D_off", true);
                        break;
                    default:
                        return;
                }

            }
            catch (KeyNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("Gear variables not found in bus model");
            }
        }
        public void SetLightSwitch(float pos)
        {
            SetVar("cp_schluessel_rot", pos);

            if (pos < 1)
            {
                if (lastHighBeamState)
                {
                    highBeamIntent = true;
                    SetHighBeam(false);
                }
            }

            if (pos > 0.5f)
            {
                if (highBeamIntent)
                    SetHighBeam(true);
            }
        }
        public void SetFrontFog(bool on) => SetVar("lights_nebel", on ? 1 : 0);
        public void SetBackFog(bool on) => SetVar("lights_nebelschluss", on ? 1 : 0);

        public void SetHighBeam(bool on)
        {
            if (GetLightSwitch() > 0.5f && on)
            {
                SetVar("lights_sw_fern", 1);
                lastHighBeamState = true;
            }
            else
            {
                SetVar("lights_sw_fern", 0);
                lastHighBeamState = false;
            }
        }

        public void SetBlinkers(bool left, bool right)
        {
            if (left && !right)
                SetVar("lights_sw_blinker", 1);
            else if (!left && right)
                SetVar("lights_sw_blinker", 2);
            else
                SetVar("lights_sw_blinker", 0);
        }

        public void SetHornState(bool on) => SetVar("cockpit_hupe", on ? 1 : 0);
        public void SetWiper(string mode)
        {
            int val = mode == "INT" ? 3 : mode == "1" ? 4 : mode == "2" ? 5 : 6;
            SetVar("windscreen_wiper", val);
        }

        public void Dispose() => omsi.Dispose();

    }
}