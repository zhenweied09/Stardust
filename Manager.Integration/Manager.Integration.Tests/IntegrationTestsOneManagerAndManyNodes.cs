﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using Manager.Integration.Test.Constants;
using Manager.Integration.Test.Helpers;
using Manager.Integration.Test.Timers;
using NUnit.Framework;

namespace Manager.Integration.Test
{
    [TestFixture]
    public class IntegrationTestsOneManagerAndManyNodes
    {
        private const int NumberOfNodesToStart = 2;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (IntegrationTestsOneManagerAndManyNodes));

        private Process StartManagerIntegrationConsoleHostProcess { get; set; }

        [SetUp]
        public void SetUp()
        {
            ManagerApiHelper = new ManagerApiHelper();

            ProcessHelper.ShutDownAllManagerIntegrationConsoleHostProcesses();

            StartManagerIntegrationConsoleHostProcess =
                ProcessHelper.StartManagerIntegrationConsoleHostProcess(NumberOfNodesToStart);

            DatabaseHelper.ClearDatabase();
        }

        private ManagerApiHelper ManagerApiHelper { get; set; }

        [Test]
        public void Create10RequestShouldReturnBothCancelAndDeleteStatuses()
        {
            JobHelper.GiveNodesTimeToInitialize();

            List<JobRequestModel> requests = JobHelper.GenerateLongRunningParamsRequests(10);

            List<Task> tasks = new List<Task>();

            foreach (var jobRequestModel in requests)
            {
                tasks.Add(ManagerApiHelper.CreateManagerDoThisTask(jobRequestModel));

                Logger.Debug("Created task for add job :" + jobRequestModel.Name);
            }


            ManagerApiHelper.CheckJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(requests.Count,
                                                                                         5000,
                                                                                         StatusConstants.CanceledStatus,
                                                                                         StatusConstants.DeletedStatus);

            ManagerApiHelper.CheckJobHistoryStatusTimer.GuidAddedEventHandler += (sender,
                                                                                  args) =>
            {
                var cancelJobTask = ManagerApiHelper.CreateManagerCancelTask(args.Guid);

                Logger.Debug("Created task for cancel job :" + args.Guid);

                cancelJobTask.Start();
            };

            ManagerApiHelper.CheckJobHistoryStatusTimer.Start();

            Parallel.ForEach(tasks,
                             task => { task.Start(); });

            ManagerApiHelper.CheckJobHistoryStatusTimer.ManualResetEventSlim.Wait();

            ProcessHelper.CloseProcess(StartManagerIntegrationConsoleHostProcess);

            var numberOfStatuses =
                ManagerApiHelper.CheckJobHistoryStatusTimer.Guids.Values.Where(s => s == StatusConstants.CanceledStatus || s == StatusConstants.DeletedStatus)
                    .ToList()
                    .Count;

            Assert.IsTrue(ManagerApiHelper.CheckJobHistoryStatusTimer.Guids.Keys.Count == numberOfStatuses);
        }

        [Test]
        public void FailJobTest()
        {
            string status = string.Empty;

            JobHelper.GiveNodesTimeToInitialize(5);

            List<JobRequestModel> requests = JobHelper.GenerateFailingJobParamsRequests(1);
            List<Task> tasks = new List<Task>();

            foreach (var jobRequestModel in requests)
            {
                tasks.Add(ManagerApiHelper.CreateManagerDoThisTask(jobRequestModel));
            }

            ManagerApiHelper.CheckJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(requests.Count,
                                                                                         5000,
                                                                                         StatusConstants.FailedStatus);

            ManagerApiHelper.CheckJobHistoryStatusTimer.GuidStatusChangedEvent += (sender,
                                                                                   args) =>
            { status = args.NewStatus; };

            ManagerApiHelper.CheckJobHistoryStatusTimer.Start();

            Parallel.ForEach(tasks,
                             task => { task.Start(); });

            ManagerApiHelper.CheckJobHistoryStatusTimer.ManualResetEventSlim.Wait();

            Assert.AreEqual(StatusConstants.FailedStatus,
                            status);

            ProcessHelper.CloseProcess(StartManagerIntegrationConsoleHostProcess);
        }

        [Test]
        public void CancelWrongJob()
        {
            JobHelper.GiveNodesTimeToInitialize(5);

            Task<HttpResponseMessage> task = ManagerApiHelper.CreateManagerCancelTask(Guid.NewGuid());

            task.Start();

            task.Wait();
        }

        [Test]
        public void ShouldBeAbleToCreate10SuccessfullJobRequest()
        {
            string status = string.Empty;

            JobHelper.GiveNodesTimeToInitialize();

            List<JobRequestModel> requests = JobHelper.GenerateTestJobParamsRequests(10);

            List<Task> tasks = new List<Task>();

            foreach (var jobRequestModel in requests)
            {
                tasks.Add(ManagerApiHelper.CreateManagerDoThisTask(jobRequestModel));
            }

            ManagerApiHelper.CheckJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(requests.Count,
                                                                                         5000,
                                                                                         StatusConstants.SuccessStatus);

            ManagerApiHelper.CheckJobHistoryStatusTimer.GuidStatusChangedEvent += (sender,
                                                                                   args) =>
            { status = args.NewStatus; };

            ManagerApiHelper.CheckJobHistoryStatusTimer.Start();

            Parallel.ForEach(tasks,
                             task => { task.Start(); });

            ManagerApiHelper.CheckJobHistoryStatusTimer.ManualResetEventSlim.Wait();

            Assert.AreEqual(StatusConstants.SuccessStatus,
                            status);

            ProcessHelper.CloseProcess(StartManagerIntegrationConsoleHostProcess);
        }
    }
}