﻿using System;
using System.Windows.Input;
using Manager.Integration.Test.WPF.ViewModels;

namespace Manager.Integration.Test.WPF.Commands
{
	public class CancelDurationTestCommand : ICommand
	{
		public MainWindowViewModel MainWindowViewModel { get; private set; }

		public CancelDurationTestCommand(MainWindowViewModel mainWindowViewModel)
		{
			MainWindowViewModel = mainWindowViewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			MainWindowViewModel.CancelDurationTest();
		}

		public event EventHandler CanExecuteChanged;

		protected virtual void OnCanExecuteChanged()
		{
			var handler = CanExecuteChanged;
			if (handler != null)
			{
				handler(this, System.EventArgs.Empty);
			}
		}
	}
}