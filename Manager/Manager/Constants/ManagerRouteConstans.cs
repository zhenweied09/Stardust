﻿namespace Stardust.Manager.Constants
{
	public static class ManagerRouteConstants
	{
		public const string JobIdOptionalParameter = "{jobId}";

		//Node
		public const string NodeHasBeenInitialized = "nodeinit";

		public const string Heartbeat = "heartbeat";

		public const string JobHasBeenCanceled = "status/cancel/" + JobIdOptionalParameter;

		public const string JobDone = "status/done/" + JobIdOptionalParameter;

		public const string JobFailed = "status/fail";

		public const string JobProgress = "status/progress";

		//Client
		public const string Job = "job";

		public const string CancelJob = "job/" + JobIdOptionalParameter;

		public const string JobByJobId = "job/" + JobIdOptionalParameter;

		public const string Jobs = "job";

		public const string Ping = "ping";

		public const string JobDetailByJobJobId = "jobdetail/" + JobIdOptionalParameter;

		public const string Nodes = "node/";
	}
}