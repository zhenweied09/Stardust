﻿using System;
using System.Windows.Input;
using Manager.Integration.Test.Helpers;
using Manager.IntegrationTest.Console.Host.Helpers;
using Newtonsoft.Json;

namespace Manager.Integration.Test.WPF.Commands
{
	public class CreateNewJobCommand : ICommand
	{
		private HttpSender HttpSender { get; set; }

		public CreateNewJobCommand()
		{
			HttpSender = new HttpSender();
			ManagerUriBuilder = new ManagerUriBuilder();
		}

		public ManagerUriBuilder ManagerUriBuilder { get; set; }

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			var uri = ManagerUriBuilder.GetStartJobUri();

			var testJobParams = new TestJobParams("Dummy data",
												  "Name data");

			var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

			var job = new JobRequestModel
			{
				Name = "Job Name ",
				Serialized = testJobParamsJson,
				Type = "NodeTest.JobHandlers.TestJobParams",
				UserName = SecurityHelper.GetLoggedInUser()
			};

			var response = HttpSender.PostAsync(uri, job);

		}

		public event EventHandler CanExecuteChanged;
	}
}