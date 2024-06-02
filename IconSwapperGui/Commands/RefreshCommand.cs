﻿using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands;

public class RefreshCommand : RelayCommand
{
    private readonly MainViewModel _viewModel;

    public RefreshCommand(MainViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.Applications.Clear();
        _viewModel.Icons.Clear();

        _viewModel.PopulateApplicationsList(_viewModel.ApplicationsFolderPath);
        _viewModel.PopulateIconsList(_viewModel.IconsFolderPath);
    }
}