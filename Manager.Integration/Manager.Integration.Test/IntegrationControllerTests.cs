﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Manager.Integration.Test.Helpers;
using Manager.Integration.Test.Notifications;
using Manager.Integration.Test.Properties;
using Manager.Integration.Test.Scripts;
using Manager.Integration.Test.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Manager.Integration.Test
{
    [TestFixture]
    public class IntegrationControllerTests
    {
        private static readonly ILog Logger =
            LogManager.GetLogger(typeof (IntegrationControllerTests));

        private bool _clearDatabase = true;
        private string _buildMode = "Debug";

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.LogDebugWithLineNumber("Start TestFixtureTearDown",
                                             Logger);

            if (AppDomainTask != null)
            {
                AppDomainTask.Dispose();
            }

            LogHelper.LogDebugWithLineNumber("Finished TestFixtureTearDown",
                                             Logger);
        }

        private static void TryCreateSqlLoggingTable(string connectionString)
        {
            LogHelper.LogDebugWithLineNumber("Run sql script to create logging file started.",
                                             Logger);

            FileInfo scriptFile =
                new FileInfo(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                          Settings.Default.CreateLoggingTableSqlScriptLocationAndFileName));

            ScriptExecuteHelper.ExecuteScriptFile(scriptFile,
                                                  connectionString);

            LogHelper.LogDebugWithLineNumber("Run sql script to create logging file finished.",
                                             Logger);
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
#if (DEBUG)
            // Do nothing.
#else
            _clearDatabase = true;
            _buildMode = "Release";
#endif
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ManagerDbConnectionString =
                ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString;

            var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));

            LogHelper.LogDebugWithLineNumber("Start TestFixtureSetUp",
                                             Logger);

            TryCreateSqlLoggingTable(ManagerDbConnectionString);

            if (_clearDatabase)
            {
                DatabaseHelper.TryClearDatabase();
            }

            CancellationTokenSource = new CancellationTokenSource();

            AppDomainTask = new AppDomainTask(_buildMode);

            Task = AppDomainTask.StartTask(CancellationTokenSource,
                                           2);

            LogHelper.LogDebugWithLineNumber("Finshed TestFixtureSetUp",
                                             Logger);
        }

        private static void CurrentDomain_UnhandledException(object sender,
                                                             UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                LogHelper.LogFatalWithLineNumber(exception.Message,
                                                 Logger,
                                                 exception);
            }
        }

        private string ManagerDbConnectionString { get; set; }

        private Task Task { get; set; }

        private AppDomainTask AppDomainTask { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        ///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
        ///     netsh http add urlacl url=http://+:9100/ user=everyone listen=yes
        /// </summary>
        [Test]
        public async void ShouldBeAbleToStartNewNode()
        {
            LogHelper.LogDebugWithLineNumber("Start test.",
                                             Logger);

            //---------------------------------------------
            // Notify when all 2 nodes are up and running. 
            //---------------------------------------------
            LogHelper.LogDebugWithLineNumber("Waiting for all 2 nodes to start up.",
                                             Logger);

            CancellationTokenSource sqlNotiferCancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(2,
                                                                      sqlNotiferCancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(TimeSpan.FromMinutes(30));

            sqlNotifier.Dispose();

            LogHelper.LogDebugWithLineNumber("All 2 nodes has started.",
                                             Logger);

            //---------------------------------------------
            // Start actual test.
            //---------------------------------------------
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            HttpResponseMessage response = null;

            string nodeName = null;

            using (var client = new HttpClient())
            {
                UriBuilder uriBuilder =
                    new UriBuilder(Settings.Default.ManagerIntegrationTestControllerBaseAddress);

                uriBuilder.Path += "appdomain/";

                Uri uri = uriBuilder.Uri;

                LogHelper.LogDebugWithLineNumber("Start calling Post Async ( " + uri + " ) ",
                                                 Logger);

                try
                {
                    response = await client.PostAsync(uriBuilder.Uri,
                                                      null,
                                                      cancellationTokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        nodeName = await response.Content.ReadAsStringAsync();

                        LogHelper.LogDebugWithLineNumber("Succeeded calling Post Async ( " + uri + " ) ",
                                                         Logger);
                    }
                }
                catch (Exception exp)
                {
                    LogHelper.LogErrorWithLineNumber(exp.Message,
                                                     Logger,
                                                     exp);
                }
            }

            Assert.IsTrue(response.IsSuccessStatusCode,
                          "Response code should be success.");

            Assert.IsNotNull(nodeName,
                             "Node must have a friendly name.");

            cancellationTokenSource.Cancel();

            LogHelper.LogDebugWithLineNumber("Finished test.",
                                             Logger);
        }

        /// <summary>
        ///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
        ///     netsh http add urlacl url=http://+:9100/ user=everyone listen=yes
        /// </summary>
        [Test]
        public async void ShouldUnloadNode1AppDomain()
        {
            LogHelper.LogDebugWithLineNumber("Start test.",
                                             Logger);

            //---------------------------------------------
            // Notify when all 2 nodes are up and running. 
            //---------------------------------------------
            LogHelper.LogDebugWithLineNumber("Waiting for all 2 nodes to start up.",
                                             Logger);

            CancellationTokenSource sqlNotiferCancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(2,
                                                                      sqlNotiferCancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(TimeSpan.FromMinutes(30));

            sqlNotifier.Dispose();

            LogHelper.LogDebugWithLineNumber("All 2 nodes has started.",
                                             Logger);

            //---------------------------------------------
            // Start actual test.
            //---------------------------------------------
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            HttpResponseMessage response = null;

            using (var client = new HttpClient())
            {
                UriBuilder uriBuilder =
                    new UriBuilder(Settings.Default.ManagerIntegrationTestControllerBaseAddress);

                uriBuilder.Path += "appdomain/" + "Node1.config";

                Uri uri = uriBuilder.Uri;

                LogHelper.LogDebugWithLineNumber("Start calling Delete Async ( " + uri + " ) ",
                                                 Logger);

                try
                {
                    response = await client.DeleteAsync(uriBuilder.Uri,
                                                        cancellationTokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        LogHelper.LogDebugWithLineNumber("Succeeded calling Delete Async ( " + uri + " ) ",
                                                         Logger);
                    }
                }
                catch (Exception exp)
                {
                    LogHelper.LogErrorWithLineNumber(exp.Message,
                                                     Logger,
                                                     exp);
                }
            }

            Assert.IsTrue(response.IsSuccessStatusCode);

            cancellationTokenSource.Cancel();

            LogHelper.LogDebugWithLineNumber("Finished test.",
                                             Logger);
        }

        /// <summary>
        ///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
        ///     netsh http add urlacl url=http://+:9100/ user=everyone listen=yes
        /// </summary>
        [Test]
        public async void ShouldReturnAllAppDomainKeys()
        {
            LogHelper.LogDebugWithLineNumber("Start test.",
                                             Logger);

            //---------------------------------------------
            // Notify when 2 nodes are up. 
            //---------------------------------------------
            LogHelper.LogDebugWithLineNumber("Waiting for 2 nodes to start up.",
                                             Logger);

            CancellationTokenSource sqlNotiferCancellationTokenSource = new CancellationTokenSource();

            SqlNotifier sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

            Task task = sqlNotifier.CreateNotifyWhenAllNodesAreUpTask(2,
                                                                      sqlNotiferCancellationTokenSource);
            task.Start();

            sqlNotifier.NotifyWhenAllNodesAreUp.Wait(TimeSpan.FromMinutes(30));

            sqlNotifier.Dispose();

            LogHelper.LogDebugWithLineNumber("All 2 nodes has started.",
                                             Logger);

            //---------------------------------------------
            // Start actual test.
            //---------------------------------------------
            HttpResponseMessage response = null;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                UriBuilder uriBuilder =
                    new UriBuilder(Settings.Default.ManagerIntegrationTestControllerBaseAddress);

                uriBuilder.Path += "appdomain";

                Uri uri = uriBuilder.Uri;

                LogHelper.LogDebugWithLineNumber("Start calling Get Async ( " + uri + " ) ",
                                                 Logger);

                try
                {
                    response = await client.GetAsync(uriBuilder.Uri,
                                                     cancellationTokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        LogHelper.LogDebugWithLineNumber("Succeeded calling Get Async ( " + uri + " ) ",
                                                         Logger);

                        string content = await response.Content.ReadAsStringAsync();

                        List<string> list =
                            JsonConvert.DeserializeObject<List<string>>(content);

                        if (list.Any())
                        {
                            foreach (var l in list)
                            {
                                LogHelper.LogDebugWithLineNumber(l,
                                                                 Logger);
                            }
                        }

                        Assert.IsTrue(list.Any());
                    }
                }

                catch (Exception exp)
                {
                    LogHelper.LogErrorWithLineNumber(exp.Message,
                                                     Logger,
                                                     exp);
                }
            }

            Assert.IsTrue(response.IsSuccessStatusCode);

            cancellationTokenSource.Cancel();

            task.Dispose();

            LogHelper.LogDebugWithLineNumber("Finished test.",
                                             Logger);
        }
    }
}