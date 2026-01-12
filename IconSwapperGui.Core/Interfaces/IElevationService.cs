namespace IconSwapperGui.Core.Interfaces;

public interface IElevationService
{
    void ElevateApplicationViaUac();
    bool IsRunningAsAdministrator();
}