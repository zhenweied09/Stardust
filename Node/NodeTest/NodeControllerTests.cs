﻿using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Results;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using NodeTest.Fakes;
using NodeTest.Fakes.InvokeHandlers;
using NodeTest.Fakes.Timers;
using NodeTest.JobHandlers;
using NUnit.Framework;
using Stardust.Node.API;
using Stardust.Node.Helpers;
using Stardust.Node.Interfaces;
using Stardust.Node.Workers;

namespace NodeTest
{
    [TestFixture]
    public class NodeControllerTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var baseAddress = new Uri(ConfigurationManager.AppSettings["BaseAddress"]);

            var managerLocation = new Uri(ConfigurationManager.AppSettings["ManagerLocation"]);

            var handlerAssembly = Assembly.Load(ConfigurationManager.AppSettings["HandlerAssembly"]);

            var nodeName = ConfigurationManager.AppSettings["NodeName"];

            _nodeConfigurationFake = new NodeConfigurationFake(baseAddress,
                                                               managerLocation,
                                                               handlerAssembly,
                                                               nodeName);


            _callBackTemplateUriFake = managerLocation;
#if DEBUG
            var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));
#endif
        }

        [SetUp]
        public void SetUp()
        {
            var parameters = new TestJobParams("hejhopp",
                                   "i lingonskogen");
            var ser = JsonConvert.SerializeObject(parameters);

            _jobToDo = new JobToDo
            {
                Id = Guid.NewGuid(),
                Name = "JobToDo Name",
                Serialized = ser,
                Type = "NodeTest.JobHandlers.TestJobParams"
            };

            _nodeStartupNotification = new NodeStartupNotificationToManagerFake(_nodeConfigurationFake,
                    _callBackTemplateUriFake);
            _pingToManagerFake = new PingToManagerFake();
            _sendJobDoneTimer = new SendJobDoneTimerFake(_nodeConfigurationFake,
                _callBackTemplateUriFake);
            _sendJobCanceledTimer = new SendJobCanceledTimerFake(_nodeConfigurationFake,
                _callBackTemplateUriFake);
            _sendJobFaultedTimer = new SendJobFaultedTimerFake(_nodeConfigurationFake,
                _callBackTemplateUriFake);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.LogInfoWithLineNumber(Logger, "Closing NodeControllerTests...");
        }

        private NodeConfigurationFake _nodeConfigurationFake;
        private IWorkerWrapper _workerWrapper;
        private NodeController _nodeController;
        private JobToDo _jobToDo;
        private Uri _callBackTemplateUriFake;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NodeControllerTests));
        private PingToManagerFake _pingToManagerFake;
        private NodeStartupNotificationToManagerFake _nodeStartupNotification;
        private SendJobDoneTimerFake _sendJobDoneTimer;
        private SendJobCanceledTimerFake _sendJobCanceledTimer;
        private SendJobFaultedTimerFake _sendJobFaultedTimer;

        [Test]
        public void CancelJobShouldReturnNotFoundWhenCancellingJobWhenIdle()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            var actionResultCancel = _nodeController.TryCancelJob(_jobToDo.Id);
            Assert.IsInstanceOf(typeof (NotFoundResult),
                                actionResultCancel);
        }
    
        [Test]
        public void CancelJobShouldReturnNotFoundWhenCancellingWrongJob()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            var wrongJobToDo = new JobToDo
            {
                Id = Guid.NewGuid(),
                Name = "Another name",
                Type = "NodeTest.JobHandlers.TestJobParams"
            };

            _nodeController.StartJob(_jobToDo);
            var actionResult = _nodeController.TryCancelJob(wrongJobToDo.Id);

            _sendJobDoneTimer.Wait.Wait(TimeSpan.FromMinutes(1));
            Assert.IsInstanceOf(typeof (NotFoundResult),
                                actionResult);
        }

        [Test]
        public void CancelJobShouldReturnOkWhenSuccessful()
        {
            _workerWrapper = new WorkerWrapper(new LongRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            _nodeController.StartJob(_jobToDo);

            var actionResult = _nodeController.TryCancelJob(_jobToDo.Id);

            _sendJobCanceledTimer.Wait.Wait(TimeSpan.FromMinutes(1));

            Assert.IsInstanceOf(typeof (OkResult),
                                actionResult);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowArgumentNullExceptionWhenJobDefinitionIsNullCancelJob()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            _nodeController.TryCancelJob(Guid.Empty);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowArgumentNullExceptionWhenJobDefinitionIsNullStartJob()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            _nodeController.StartJob(null);
        }

        [Test]
        public void StartJobShouldReturnConflictWhenAlreadyProcessingJob()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            var parameters = new TestJobParams("hejhopp",
                                               "i lingonskogen");
            var ser = JsonConvert.SerializeObject(parameters);

            var jobToDo2 = new JobToDo {Id = Guid.NewGuid(), Name = "Another name", Serialized = ser};

            _nodeController.StartJob(_jobToDo);

            var actionResult = _nodeController.StartJob(jobToDo2);

            _sendJobDoneTimer.Wait.Wait(TimeSpan.FromMinutes(1));
            Assert.IsInstanceOf(typeof (ConflictResult),
                                actionResult);
        }

        [Test]
        public void StartJobShouldReturnOkIfNotRunningJobAlready()
        {
            _workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
                      _nodeConfigurationFake,
                      _nodeStartupNotification,
                      _pingToManagerFake,
                      _sendJobDoneTimer,
                      _sendJobCanceledTimer,
                      _sendJobFaultedTimer,
                      new PostHttpRequestFake());

            _nodeController = new NodeController(_workerWrapper) { Request = new HttpRequestMessage() };

            LogHelper.LogInfoWithLineNumber(Logger, "Starting test...");
            var actionResult = _nodeController.StartJob(_jobToDo);
            _sendJobDoneTimer.Wait.Wait(TimeSpan.FromMinutes(1));
            Assert.IsInstanceOf(typeof (OkNegotiatedContentResult<string>),
                                actionResult);
        }
    }
}