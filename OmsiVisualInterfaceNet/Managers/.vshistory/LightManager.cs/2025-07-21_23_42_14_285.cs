using System;

namespace OmsiVisualInterfaceNet
{
    public class LightManager
    {
        private readonly OmsiManager omsiManager;
        private readonly SerialManager serialManager;

        private enum LightState
        {
            LIGHTS_OFF,
            PARKING_LIGHTS,
            LOW_BEAM,
            HIGH_BEAM,
            FRONT_FOG,
            BACK_FOG
        }

        private LightState currentState = LightState.LIGHTS_OFF;
        private LightState lastState = LightState.LIGHTS_OFF;
        private bool highBeamForcedOff = false;

        public LightManager(OmsiManager omsiManager, SerialManager serialManager)
        {
            this.omsiManager = omsiManager;
            this.serialManager = serialManager;
            serialManager.OnDataReceived += HandleSerialInput;
        }

        public void Update()
        {
            if (omsiManager.CurrentVehicle == null) return;

            var newState = CalculateCurrentState();

            if (newState != currentState || highBeamForcedOff)
            {
                currentState = newState;
                UpdateOmsiLightVariables();
                serialManager.WriteLine($"LIGHTS_{currentState}");
            }
        }

        private LightState CalculateCurrentState()
        {
            // Get actual OMSI light states
            bool parkingLights = omsiManager.CurrentVehicle.GetVariable("lights_stand") > 0;
            bool lowBeam = omsiManager.CurrentVehicle.GetVariable("lights_abbl") > 0;
            bool highBeam = omsiManager.CurrentVehicle.GetVariable("lights_fern") > 0;
            bool frontFog = omsiManager.CurrentVehicle.GetVariable("lights_nebelschw") > 0;
            bool backFog = omsiManager.CurrentVehicle.GetVariable("lights_nebelschluss") > 0;

            // Determine state hierarchy
            if (backFog) return LightState.BACK_FOG;
            if (frontFog) return LightState.FRONT_FOG;
            if (highBeam && !highBeamForcedOff) return LightState.HIGH_BEAM;
            if (lowBeam) return LightState.LOW_BEAM;
            if (parkingLights) return LightState.PARKING_LIGHTS;
            return LightState.LIGHTS_OFF;
        }

        private void UpdateOmsiLightVariables()
        {
            // Set light switch position first (0=Off, 1=Parking, 2=Low Beam)
            int switchPosition = currentState switch
            {
                LightState.LIGHTS_OFF => 0,
                LightState.PARKING_LIGHTS => 1,
                _ => 2 // LOW_BEAM and above
            };
            omsiManager.CurrentVehicle.SetVariable("cp_light_sw", switchPosition);

            // Set individual light states
            omsiManager.CurrentVehicle.SetVariable("lights_stand",
                currentState >= LightState.PARKING_LIGHTS ? 1 : 0);

            omsiManager.CurrentVehicle.SetVariable("lights_abbl",
                currentState >= LightState.LOW_BEAM ? 1 : 0);

            omsiManager.CurrentVehicle.SetVariable("lights_fern",
                currentState == LightState.HIGH_BEAM && !highBeamForcedOff ? 1 : 0);

            omsiManager.CurrentVehicle.SetVariable("lights_nebelschw",
                currentState == LightState.FRONT_FOG || currentState == LightState.BACK_FOG ? 1 : 0);

            omsiManager.CurrentVehicle.SetVariable("lights_nebelschluss",
                currentState == LightState.BACK_FOG ? 1 : 0);

            // Activate fog light switches if needed
            omsiManager.CurrentVehicle.SetVariable("cp_licht_nebelschw_sw",
                currentState == LightState.FRONT_FOG || currentState == LightState.BACK_FOG ? 1 : 0);

            omsiManager.CurrentVehicle.SetVariable("cp_taster_nebelschluss_target",
                currentState == LightState.BACK_FOG ? 1 : 0);

            // Update door button lights based on headlight state
            bool headlightsOn = currentState >= LightState.PARKING_LIGHTS;
            
            for (int i = 1; i <= 3; i++)
            {
                bool doorOpen = omsiManager.GetDoorState(i);
                if (doorOpen)
                {
                    serialManager.WriteLine($"DOOR{i}_ON");  // Full brightness for open doors
                }
                else
                {
                    if (headlightsOn)
                        serialManager.WriteLine($"DOOR{i}_DIM");  // Dim when headlights on
                    else
                        serialManager.WriteLine($"DOOR{i}_OFF");  // Off when headlights off
                }
            }
        }

        public void HandleSerialInput(string input)
        {
            if (omsiManager.CurrentVehicle == null) return;

            switch (input)
            {
                case "LIGHTS_OFF":
                    currentState = LightState.LIGHTS_OFF;
                    highBeamForcedOff = false;
                    break;

                case "DAY_LIGHTS_ON":
                case "PARKING_LIGHTS":
                    currentState = LightState.PARKING_LIGHTS;
                    highBeamForcedOff = false;
                    break;

                case "LOW_BEAM":
                case "LOW_BEAM_ON":
                    currentState = LightState.LOW_BEAM;
                    highBeamForcedOff = false;
                    break;

                case "LOW_BEAM_OFF":
                    currentState = LightState.PARKING_LIGHTS;
                    highBeamForcedOff = false;
                    break;

                case "HIGH_BEAM_ON":
                    currentState = LightState.HIGH_BEAM;
                    highBeamForcedOff = false;
                    break;

                case "HIGH_BEAM_OFF":
                    highBeamForcedOff = true;
                    if (currentState == LightState.HIGH_BEAM)
                        currentState = LightState.LOW_BEAM;
                    break;

                case "FRONT_FOG_ON":
                    currentState = LightState.FRONT_FOG;
                    break;

                case "FRONT_FOG_OFF":
                    if (currentState == LightState.FRONT_FOG)
                        currentState = LightState.LOW_BEAM;
                    break;

                case "BACK_FOG_ON":
                    currentState = LightState.BACK_FOG;
                    break;

                case "BACK_FOG_OFF":
                    if (currentState == LightState.BACK_FOG)
                        currentState = omsiManager.CurrentVehicle.GetVariable("lights_nebelschw") > 0 ?
                            LightState.FRONT_FOG : LightState.LOW_BEAM;
                    break;

                case "LIGHT_SWITCH_ROTATE":
                    currentState = currentState switch
                    {
                        LightState.LIGHTS_OFF => LightState.PARKING_LIGHTS,
                        LightState.PARKING_LIGHTS => LightState.LOW_BEAM,
                        _ => LightState.LIGHTS_OFF
                    };
                    break;
            }

            UpdateOmsiLightVariables();
        }

        public bool AreHeadlightsOn()
        {
            return currentState >= LightState.PARKING_LIGHTS;
        }
    }
}