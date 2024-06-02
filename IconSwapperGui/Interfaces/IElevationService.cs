namespace IconSwapperGui.Interfaces;

public interface IElevationService
{
    void ElevateApplicationViaUac();
    bool IsRunningAsAdministrator();
}