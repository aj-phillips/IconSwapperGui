namespace IconSwapperGui.Core.Interfaces;

public interface IDispatcher
{
    void Invoke(Action action);
}