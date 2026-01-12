using System.Windows;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.UI.Services;

public class EventDispatcherService : IDispatcher
{
    public void Invoke(Action action)
    {
        if (Application.Current == null)
        {
            action();
            return;
        }

        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}