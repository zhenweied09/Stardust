﻿using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace Manager.Integration.Test.Helpers
{
	public static class JobHelper
	{
		public static void GiveNodesTimeToInitialize(int numberOfSeconds = 10)
		{
			var time = TimeSpan.FromSeconds(numberOfSeconds);

			Thread.Sleep(time);
		}

		public static TimeSpan GenerateTimeoutTimeInMinutes(int numberOfRequest,
		                                                    int latencyPerRequestInMinutes = 1)
		{
			if (numberOfRequest <= 0)
			{
				throw new ArgumentException("numberOfRequest");
			}

			return new TimeSpan(0, numberOfRequest*latencyPerRequestInMinutes, 0);
		}

		public static TimeSpan GenerateTimeoutTimeInSeconds(int numberOfRequest,
		                                                    int latencyPerRequestInSeconds = 1)
		{
			if (numberOfRequest <= 0)
			{
				throw new ArgumentException("numberOfRequest");
			}

			return new TimeSpan(0,
			                    0,
			                    numberOfRequest*latencyPerRequestInSeconds);
		}

		public static List<JobRequestModel> GenerateLongRunningParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var longRunningJobParams = new LongRunningJobParams("Job name " + i);

					var longRunningJobParamsJson = JsonConvert.SerializeObject(longRunningJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = longRunningJobParamsJson,
						Type = "NodeTest.JobHandlers.LongRunningJobParams",
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobRequestModel> GenerateFailingJobParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var failingJobParams = new FailingJobParams("Error message " + i);

					var failingJobParamsJson = JsonConvert.SerializeObject(failingJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = failingJobParamsJson,
						Type = "NodeTest.JobHandlers.FailingJobParams",
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobRequestModel> GenerateFastJobParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var fastJobParams = new FastJobParams("Name " + i);

					var fastJobParamsJson = JsonConvert.SerializeObject(fastJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = fastJobParamsJson,
						Type = "NodeTest.JobHandlers.FastJobParams",
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobRequestModel> GenerateInvalidTestJobParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
														  "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = null,
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobRequestModel> GenerateTestReportProgressJobParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
														  "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = "NodeTest.JobHandlers.TestReportProgressJobParams",
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobRequestModel> GenerateTestJobParamsRequests(int numberOfJobRequests)
		{
			List<JobRequestModel> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobRequestModel>();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
					                                      "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobRequestModel
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = "NodeTest.JobHandlers.TestJobParams",
						UserName = SecurityHelper.GetLoggedInUser()
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}
	}
}