﻿using System;
using System.Threading;
using log4net;
using Stardust.Node.Extensions;
using Stardust.Node.Helpers;
using Stardust.Node.Interfaces;

namespace NodeTest.JobHandlers
{
	public class FailingJobWorker : IHandle<FailingJobParams>

	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof (FailingJobWorker));

		public FailingJobWorker()
		{
			Logger.LogDebugWithLineNumber("'Failing Job Worker' class constructor called.");
		}

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public void Handle(FailingJobParams parameters,
		                   CancellationTokenSource cancellationTokenSource,
		                   Action<string> sendProgress)
		{
			Logger.LogDebugWithLineNumber("'Failing Job Worker' handle method called.");

			CancellationTokenSource = cancellationTokenSource;

			var doTheRealThing = new FailingJobCode();

			doTheRealThing.DoTheThing(parameters,
			                          cancellationTokenSource,
			                          sendProgress);
		}
	}
}