﻿using System;
using System.ComponentModel;
using System.Windows.Input;
using Manager.Integration.Test.WPF.ViewModels;

namespace Manager.Integration.Test.WPF.Commands
{
    public class ClearLoggingTableInDatabaseCommand : ICommand
    {
        public MainWindowViewModel MainWindowViewModel { get; private set; }

        public ClearLoggingTableInDatabaseCommand(MainWindowViewModel mainWindowViewModel)
        {
            MainWindowViewModel = mainWindowViewModel;

            //MainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        private void MainWindowViewModel_PropertyChanged(object sender,
                                                         PropertyChangedEventArgs e)
        {
            OnCanExecuteChanged();
        }

        public bool CanExecute(object parameter)
        {
            return MainWindowViewModel.DatabaseContainsInformation();
        }

        public void Execute(object parameter)
        {
            MainWindowViewModel.DatabaseClearAllInformation();
        }

        public event EventHandler CanExecuteChanged;

        protected virtual void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this,
								  System.EventArgs.Empty);
            }
        }
    }
}