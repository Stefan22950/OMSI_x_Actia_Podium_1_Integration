using OmsiHook;

namespace OmsiVisualInterfaceNet.Managers
{
    public interface IOmsiManager
    {
        string vehicleName { get; }
        OmsiRoadVehicleInst CurrentVehicle { get; }
    }
}
