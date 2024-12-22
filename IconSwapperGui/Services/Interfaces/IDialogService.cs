namespace IconSwapperGui.Services.Interfaces;

public interface IDialogService
{
    public void ShowError(string message, string caption);
    public void ShowWarning(string message, string caption);
    public void ShowInformation(string message, string caption);
}