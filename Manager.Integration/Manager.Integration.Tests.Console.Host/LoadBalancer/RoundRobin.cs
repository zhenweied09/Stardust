using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using log4net;
using Manager.IntegrationTest.Console.Host.Helpers;
using Manager.IntegrationTest.Console.Host.Log4Net.Extensions;

namespace Manager.IntegrationTest.Console.Host.LoadBalancer
{
	public static class RoundRobin
	{
		private static readonly ILog Logger =
			LogManager.GetLogger(typeof(RoundRobin));

		private static List<Uri> _hosts;

		private static int _currentIndex;

		public static void Set(List<Uri> hosts)
		{
			Logger.LogDebugWithLineNumber("Start.");

			_hosts = hosts;

			if (hosts.Any())
			{
				foreach (var host in hosts)
				{
					Logger.LogDebugWithLineNumber("Load balancer will register manager url : ( " + host  + " )");
				}
			}

			Logger.LogDebugWithLineNumber("Finished.");
		}

		public static Uri Next(HttpRequestMessage request)
		{
			Logger.LogDebugWithLineNumber("Start.");

			Interlocked.Increment(ref _currentIndex);

			var host = _hosts[_currentIndex % _hosts.Count];

			Logger.LogDebugWithLineNumber("Load balancer will use manager url : ( " + host + " ) for request : ( " + request.RequestUri + " )" );

			Logger.LogDebugWithLineNumber("Finsihed.");

			return host;
		}
	}
}