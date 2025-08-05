using OmsiHook;

using System;

namespace OmsiVisualInterfaceNet
{
    public class OmsiManager : IDisposable
    {
        private readonly OmsiHook.OmsiHook omsi;
        public event Action<OmsiRoadVehicleInst> OnVehicleChanged;

        public OmsiRoadVehicleInst CurrentVehicle { get; private set; }

        public OmsiManager()
        {
            omsi = new OmsiHook.OmsiHook();
        }

        public async System.Threading.Tasks.Task Initialize()
        {
            await omsi.AttachToOMSI();
            omsi.OnActiveVehicleChanged += (s, v) =>
            {
                CurrentVehicle = v;
                OnVehicleChanged?.Invoke(v);
            };
            CurrentVehicle = omsi.Globals.PlayerVehicle;
            OnVehicleChanged?.Invoke(CurrentVehicle);
        }

        // ===== Vehicle Data =====

        public double GetSpeed() => CurrentVehicle?.Tacho ?? 0;
        public double GetRPM() => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable("engine_n"));
        public int GetElectricState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("elec_busbar_main"));
        public int GetBlinkerState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("lights_sw_blinker"));
        public bool GetHazardLightsState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_sw_warnblinker"));
        public bool GetDoorState(int door) => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable($"door_{door}"));
        public int GetGearState()
        {
            if (CurrentVehicle == null) return 0;
            bool reverse = Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gangR"));
            bool drive = Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gang1"));
            return reverse ? -1 : drive ? 1 : 0;
        }

        public bool GetDoorLightState(int door)
        {
            // Lights from Actia dashboard door indicators
            return CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable($"Actia_DASH_Light_doorfront_active"));
        }

        public bool GetLowBeamState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_abbl"));
        public bool GetParkingLightState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_stand"));
        public bool GetHighBeamState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_fern"));

        public double GetVariable(string v) => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable(v));

        // ===== Setters =====

        public void SetMasterSwitch(bool on) => CurrentVehicle?.SetVariable("elec_busbar_main_sw", on ? 1 : 0);

        public void SetStarter(bool pressed)
        {
            if (CurrentVehicle == null) return;
            CurrentVehicle.SetVariable("cp_taster_anlasser", pressed ? 1 : 0);
            if (pressed && GetGearState() == 0)
            {
                CurrentVehicle.SetVariable("engine_injection_on", 1);
                CurrentVehicle.SetVariable("engine_starter", 1);
            }
        }

        public void SetEngineShutdown(bool pressed)
        {
            if (CurrentVehicle == null) return;
            CurrentVehicle.SetVariable("cp_taster_motorabstellung", pressed ? 1 : 0);
        }

        public void SetLowBeamState(bool on) => CurrentVehicle?.SetVariable("cp_light_sw", on ? 2 : 0);
        public void SetHighBeamState(bool on) => CurrentVehicle?.SetVariable("lights_fern", on ? 1 : 0);
        public void SetFrontFog(bool on) => CurrentVehicle?.SetVariable("lights_nebel_v", on ? 1 : 0);
        public void SetRearFog(bool on) => CurrentVehicle?.SetVariable("lights_nebel_h", on ? 1 : 0);

        public void SetBlinkerState(int state)
        {
            // -1=left, 0=off, 1=right
            CurrentVehicle?.SetVariable("lights_sw_blinker", state);
        }

        public void SetHazards(bool on) => CurrentVehicle?.SetVariable("lights_sw_warnblinker", on ? 1 : 0);
        public void SetHornState(bool on) => CurrentVehicle?.SetVariable("cockpit_horn", on ? 1 : 0);

        public void SetGear(int pos)
        {
            // -1=Reverse, 0=Neutral, 1=Drive
            if (CurrentVehicle == null) return;
            CurrentVehicle.SetVariable("cockpit_gangR", pos == -1 ? 1 : 0);
            CurrentVehicle.SetVariable("cockpit_gangN", pos == 0 ? 1 : 0);
            CurrentVehicle.SetVariable("cockpit_gang1", pos == 1 ? 1 : 0);
        }

        public void SetDoorState(int door, bool open)
        {
            if (CurrentVehicle == null) return;
            CurrentVehicle.SetVariable($"door_{door}", open ? 1 : 0);
            CurrentVehicle.SetVariable($"cockpit_tuertaster{door}", open ? 1 : 0);
        }

        public void ToggleDoorState(int door)
        {
            if (CurrentVehicle == null) return;
            bool current = GetDoorState(door);
            SetDoorState(door, !current);
        }

        public void SetAutoDoors(bool on) => CurrentVehicle?.SetVariable("door_DISABLED_Req", on ? 0 : 1);

        public void ToggleHeating()
        {
            if (CurrentVehicle == null) return;
            bool current = Convert.ToBoolean(CurrentVehicle.GetVariable("cp_heizluefter_sw"));
            CurrentVehicle.SetVariable("cp_heizluefter_sw", current ? 0 : 1);
        }

        public void SetRetarderOff(bool on) => CurrentVehicle?.SetVariable("cp_retarder_sw_disable", on ? 1 : 0);

        public void SetSuspensionUp(bool pressed) => CurrentVehicle?.SetVariable("cp_hub_up_sw", pressed ? 1 : 0);
        public void SetSuspensionDown(bool pressed) => CurrentVehicle?.SetVariable("cp_hub_dn_sw", pressed ? 1 : 0);

        public void ToggleStrollerMode()
        {
            if (CurrentVehicle == null) return;
            bool current = Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_light_kinderwagenwunsch"));
            CurrentVehicle.SetVariable("cockpit_light_kinderwagenwunsch", current ? 0 : 1);
        }

        public void ToggleDevMode()
        {
            if (CurrentVehicle == null) return;
            bool current = Convert.ToBoolean(CurrentVehicle.GetVariable("DEV"));
            CurrentVehicle.SetVariable("DEV", current ? 0 : 1);
        }

        public void SetActiaScreenUp(bool pressed) => CurrentVehicle?.SetVariable("Actia_BTN_chmod_up", pressed ? 1 : 0);
        public void SetActiaScreenDown(bool pressed) => CurrentVehicle?.SetVariable("Actia_BTN_chmod_down", pressed ? 1 : 0);

        public void SetDriverLight(bool on) => CurrentVehicle?.SetVariable("cp_fahrerlicht_sw", on ? 1 : 0);
        public void SetPassengerLight(bool on) => CurrentVehicle?.SetVariable("cp_licht_unterdeck_sw", on ? 1 : 0);

        public void Dispose() => omsi.Dispose();
    }
}
