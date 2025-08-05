using OmsiHook;

using System;
using System.Threading.Tasks;

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

        public async Task Initialize()
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

        public double GetSpeed() => CurrentVehicle?.Tacho ?? 0;
        public double GetRPM() => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable("engine_n"));
        public int GetElectricState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("elec_busbar_main"));
        public int GetBlinkerState() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle.GetVariable("lights_sw_blinker"));
        public bool GetHazardLightsState() => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable("lights_sw_warnblinker"));
        public bool GetDoorState(int door) => CurrentVehicle != null && Convert.ToBoolean(CurrentVehicle.GetVariable($"door_{door}"));
        public int GetLightSwitch() => CurrentVehicle == null ? 0 : Convert.ToInt32(CurrentVehicle?.GetVariable("cp_light_sw"));
        public int GetGearState() => CurrentVehicle == null ? 0 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gangR")) ? -1 :
            Convert.ToBoolean(CurrentVehicle.GetVariable("cockpit_gang1")) ? 1 : 0;

        public double GetVariable(string v) => CurrentVehicle == null ? 0 : Convert.ToDouble(CurrentVehicle.GetVariable(v));

        public void SetMasterSwitch(bool on) => CurrentVehicle?.SetTrigger("cp_batterietrennschalter_toggle", on ? true : false);
        public void SetStarter(bool pressed)
        {
            CurrentVehicle?.SetVariable("cp_taster_anlasser", pressed ? 1 : 0);
            if (pressed && GetGearState() == 0)
            {
                CurrentVehicle?.SetVariable("engine_injection_on", 1);
                CurrentVehicle?.SetVariable("engine_starter", 1);
            }
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
            bool door1Open = GetDoorState(0);

            if (door == 0 && door1Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront0", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_0", door1Open ? 0 : 1);
                CurrentVehicle?.SetTrigger($"bus_doorfront1", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_1", door1Open ? 0 : 1);
            }
            else if (door == 0 && !door1Open)
            {
                CurrentVehicle?.SetTrigger($"bus_doorfront0_off", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_0", door1Open ? 0 : 1);
                CurrentVehicle?.SetTrigger($"bus_doorfront1_off", door1Open ? false : true);
                CurrentVehicle?.SetVariable($"doorTarget_1", door1Open ? 0 : 1);
            }
            else if (door == 1)
                CurrentVehicle?.SetTrigger("bus_doorfront23", pressed ? true : false);
                
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