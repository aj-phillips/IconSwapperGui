namespace IconSwapperGui.Services.Interfaces;

public interface IElevationService
{
    void ElevateApplicationViaUac();
    bool IsRunningAsAdministrator();
}