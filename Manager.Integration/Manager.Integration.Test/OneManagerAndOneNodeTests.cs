﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Manager.Integration.Test.Constants;
using Manager.Integration.Test.Helpers;
using Manager.Integration.Test.Notifications;
using Manager.Integration.Test.Properties;
using Manager.Integration.Test.Scripts;
using Manager.Integration.Test.Tasks;
using Manager.Integration.Test.Timers;
using NUnit.Framework;

namespace Manager.Integration.Test
{
    [TestFixture]
    public class OneManagerAndOneNodeTests
    {
        private static readonly ILog Logger =
            LogManager.GetLogger(typeof (OneManagerAndOneNodeTests));

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ManagerDbConnectionString =
                ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString;

            var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));

            LogHelper.LogInfoWithLineNumber("Start TestFixtureSetUp",
                                            Logger);

            TryCreateSqlLoggingTable();


#if (DEBUG)
            // Do nothing.
#else
            _clearDatabase = true;
            _buildMode = "Release";
#endif

            if (_clearDatabase)
            {
                DatabaseHelper.TryClearDatabase();
            }

            CancellationTokenSource = new CancellationTokenSource();

            AppDomainTask = new AppDomainTask(_buildMode);

            Task = AppDomainTask.StartTask(cancellationTokenSource: CancellationTokenSource,
                                           numberOfNodes: 1);

            LogHelper.LogInfoWithLineNumber("Finshed TestFixtureSetUp",
                                            Logger);
        }

        private string ManagerDbConnectionString { get; set; }

        private Task Task { get; set; }

        private AppDomainTask AppDomainTask { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private void CurrentDomain_UnhandledException(object sender,
                                                      UnhandledExceptionEventArgs e)
        {
        }

        private static void TryCreateSqlLoggingTable()
        {
            LogHelper.LogInfoWithLineNumber("Run sql script to create logging file started.",
                                            Logger);

            FileInfo scriptFile =
                new FileInfo(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                          Settings.Default.CreateLoggingTableSqlScriptLocationAndFileName));

            ScriptExecuteHelper.ExecuteScriptFile(scriptFile,
                                                  ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString);

            LogHelper.LogInfoWithLineNumber("Run sql script to create logging file finished.",
                                            Logger);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.LogInfoWithLineNumber("Start TestFixtureTearDown",
                                            Logger);

            if (AppDomainTask != null)
            {
                AppDomainTask.Dispose();
            }

            LogHelper.LogInfoWithLineNumber("Finished TestFixtureTearDown",
                                            Logger);
        }

        private bool _clearDatabase = true;

        private string _buildMode = "Debug";

        [Test]
        public void CreateSeveralRequestShouldReturnBothCancelAndDeleteStatusesTest()
        {
            LogHelper.LogInfoWithLineNumber("Start.",
                                            Logger);

            List<JobRequestModel> createNewJobRequests =
                JobHelper.GenerateLongRunningParamsRequests(1);

            var timeout = JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
                                                                 5);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(1,
                                                                      cancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(timeout);

            sqlNotifier.Dispose();

            List<JobManagerTaskCreator> jobManagerTaskCreators = new List<JobManagerTaskCreator>();

            var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
                                                                            StatusConstants.SuccessStatus,
                                                                            StatusConstants.DeletedStatus,
                                                                            StatusConstants.FailedStatus,
                                                                            StatusConstants.CanceledStatus);

            foreach (var jobRequestModel in createNewJobRequests)
            {
                var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);

                jobManagerTaskCreators.Add(jobManagerTaskCreator);
            }

            var startJobTaskHelper = new StartJobTaskHelper();

            var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
                                                                      CancellationTokenSource,
                                                                      timeout);

            checkJobHistoryStatusTimer.GuidAddedEventHandler += (sender,
                                                                 args) =>
            {
                Task.Factory.StartNew(() =>
                {
                    NodeStatusNotifier nodeStartedNotifier =
                        new NodeStatusNotifier(ManagerDbConnectionString);

                    nodeStartedNotifier.StartJobDefinitionStatusNotifier(args.Guid,
                                                                         "Started",
                                                                         CancellationTokenSource);

                    nodeStartedNotifier.JobDefinitionStatusNotify.Wait(timeout);

                    var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                    jobManagerTaskCreator.CreateDeleteJobToManagerTask(args.Guid);

                    jobManagerTaskCreator.StartAndWaitDeleteJobToManagerTask(timeout);

                    nodeStartedNotifier.Dispose();
                    jobManagerTaskCreator.Dispose();
                },
                CancellationTokenSource.Token);
            };

            checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);

            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.CanceledStatus ||
                                                                       pair.Value == StatusConstants.DeletedStatus));

            taskHlp.Dispose();

            foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
            {
                jobManagerTaskCreator.Dispose();
            }

            LogHelper.LogInfoWithLineNumber("Finished.",
                                            Logger);
        }


        [Test]
        public void JobShouldHaveStatusFailedIfFailedTest()
        {
            LogHelper.LogInfoWithLineNumber("Start.",
                                            Logger);

            List<JobRequestModel> createNewJobRequests = JobHelper.GenerateFailingJobParamsRequests(1);

            var timeout = JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(1,
                                                                      cancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(timeout);

            sqlNotifier.Dispose();


            List<JobManagerTaskCreator> jobManagerTaskCreators = new List<JobManagerTaskCreator>();

            var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
                                                                            StatusConstants.SuccessStatus,
                                                                            StatusConstants.DeletedStatus,
                                                                            StatusConstants.FailedStatus,
                                                                            StatusConstants.CanceledStatus);
            foreach (var jobRequestModel in createNewJobRequests)
            {
                var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);

                jobManagerTaskCreators.Add(jobManagerTaskCreator);
            }

            StartJobTaskHelper startJobTaskHelper = new StartJobTaskHelper();

            var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
                                                                      CancellationTokenSource,
                                                                      timeout);

            checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);
            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.FailedStatus));

            taskHlp.Dispose();

            foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
            {
                jobManagerTaskCreator.Dispose();
            }

            LogHelper.LogInfoWithLineNumber("Finished.",
                                            Logger);
        }

        [Test]
        public void CancelWrongJobsTest()
        {
            LogHelper.LogInfoWithLineNumber("Start.",
                                            Logger);

            List<JobRequestModel> createNewJobRequests = JobHelper.GenerateTestJobParamsRequests(1);

            LogHelper.LogInfoWithLineNumber("( " + createNewJobRequests.Count + " ) jobs will be created.",
                                            Logger);

            TimeSpan timeout = JobHelper.GenerateTimeoutTimeInSeconds(createNewJobRequests.Count);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(1,
                                                                      cancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(timeout);

            sqlNotifier.Dispose();

            List<JobManagerTaskCreator> jobManagerTaskCreators = new List<JobManagerTaskCreator>();

            var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
                                                                            StatusConstants.SuccessStatus,
                                                                            StatusConstants.DeletedStatus,
                                                                            StatusConstants.FailedStatus,
                                                                            StatusConstants.CanceledStatus);

            foreach (var jobRequestModel in createNewJobRequests)
            {
                var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);

                jobManagerTaskCreators.Add(jobManagerTaskCreator);
            }

            StartJobTaskHelper startJobTaskHelper = new StartJobTaskHelper();

            var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
                                                                      CancellationTokenSource,
                                                                      timeout);

            checkJobHistoryStatusTimer.GuidAddedEventHandler += (sender,
                                                                 args) =>
            {
                //-----------------------------------
                // Wait for job to start.
                //-----------------------------------
                NodeStatusNotifier nodeStartedNotifier =
                    new NodeStatusNotifier(ManagerDbConnectionString);

                nodeStartedNotifier.StartJobDefinitionStatusNotifier(args.Guid,
                                                                     "Started",
                                                                     CancellationTokenSource);

                nodeStartedNotifier.JobDefinitionStatusNotify.Wait(timeout);

                //-----------------------------------
                // Send wrong id to cancel.
                //-----------------------------------
                var newGuid = Guid.NewGuid();

                var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                jobManagerTaskCreator.CreateDeleteJobToManagerTask(newGuid);

                jobManagerTaskCreator.StartAndWaitDeleteJobToManagerTask(timeout);

                jobManagerTaskCreator.Dispose();
            };

            checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);
            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.SuccessStatus));

            taskHlp.Dispose();

            foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
            {
                jobManagerTaskCreator.Dispose();
            }

            LogHelper.LogInfoWithLineNumber("Finished.",
                                            Logger);
        }

        /// <summary>
        ///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
        ///     netsh http add urlacl url=http://+:9050/ user=everyone listen=yes
        /// </summary>
        [Test]
        public void ShouldBeAbleToCreateManySuccessJobRequestTest()
        {
            LogHelper.LogInfoWithLineNumber("Start.",
                                            Logger);

            List<JobRequestModel> createNewJobRequests =
                JobHelper.GenerateTestJobParamsRequests(1);

            LogHelper.LogInfoWithLineNumber("( " + createNewJobRequests.Count + " ) jobs will be created.",
                                            Logger);

            TimeSpan timeout = JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(1,
                                                                      cancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(timeout);

            sqlNotifier.Dispose();

            List<JobManagerTaskCreator> jobManagerTaskCreators = new List<JobManagerTaskCreator>();

            var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
                                                                            StatusConstants.SuccessStatus,
                                                                            StatusConstants.DeletedStatus,
                                                                            StatusConstants.FailedStatus,
                                                                            StatusConstants.CanceledStatus);

            foreach (var jobRequestModel in createNewJobRequests)
            {
                var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);

                jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);

                jobManagerTaskCreators.Add(jobManagerTaskCreator);
            }

            StartJobTaskHelper startJobTaskHelper = new StartJobTaskHelper();

            var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
                                                                      CancellationTokenSource,
                                                                      timeout);

            checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);
            Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.SuccessStatus));

            taskHlp.Dispose();

            foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
            {
                jobManagerTaskCreator.Dispose();
            }

            LogHelper.LogInfoWithLineNumber("Finished.",
                                            Logger);
        }
    }
}