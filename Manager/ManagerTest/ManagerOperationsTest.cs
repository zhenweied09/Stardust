﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using ManagerTest.Database;
using ManagerTest.Fakes;
using NUnit.Framework;
using SharpTestsEx;
using Stardust.Manager;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;

namespace ManagerTest
{
    [ManagerOperationTests]
    [TestFixture]
    public class ManagerOperationsTest : DatabaseTest
    {
        public ManagerController Target;
        public IJobRepository JobRepository;
        public IWorkerNodeRepository NodeRepository;
        public INodeManager NodeManager;
        public FakeHttpSender HttpSender;
        public Uri NodeUri; 


        [Test]
        public void ShouldBeAbleToAcknowledgeWhenJobIsReceived()
        {
            var job = new JobRequestModel() {Name = "ShouldBeAbleToAcknowledgeWhenJobIsReceived", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"};
            var result = Target.DoThisJob(job);
            result.Should()
                .Not.Be.Null();
        }

        [Test]
        public void ShouldBeAbleToPersistManyJobs()
        {
            Stopwatch stopwatch = new Stopwatch();

            List<JobRequestModel> jobRequestModels = new List<JobRequestModel>();

            for (int i = 0; i < 500; i++)
            {
                jobRequestModels.Add(new JobRequestModel
                {
                    Name = "Name data " + i,
                    Serialized = "ngtbara",
                    Type = "typngtannat",
                    UserName = "ManagerTests"
                });
            }

            List<Task> tasks = new List<Task>();

            foreach (var jobRequestModel in jobRequestModels)
            {
                var model = jobRequestModel;

                tasks.Add(new Task(() => { Target.DoThisJob(model); }));
            }

            stopwatch.Start();

            Parallel.ForEach(tasks,
                             task => { task.Start(); });

            Task.WaitAll(tasks.ToArray());

            stopwatch.Stop();

            TimeSpan elapsed = stopwatch.Elapsed;

            var sec = elapsed.Seconds;

            Assert.IsTrue(true);
        }


        [Test]
        public void ShouldBeAbleToPersistNewJob()
        {
            var job = new JobRequestModel() {Name = "ShouldBeAbleToPersistNewJob", Serialized = "ngtbara", Type = "typngtannat", UserName = "ManagerTests"};
            Target.DoThisJob(job);
            JobRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(1);
        }

        [Test]
        public void ShouldReturnIdOfPersistedJob()
        {
            Guid newJobId = ((OkNegotiatedContentResult<Guid>) Target.DoThisJob(new JobRequestModel() {Name = "ShouldReturnIdOfPersistedJob", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"})).Content;
            newJobId.Should()
                .Not.Be.Null();
        }

        [Test]
        public void ShouldAddANodeOnInit()
        {
            Target.NodeInitialized(NodeUri);
            NodeRepository.LoadAll()
                .First()
                .Url.Should()
                .Be.EqualTo(NodeUri.ToString());
        }

        [Test]
        public void ShouldBeAbleToSendNewJobToAvailableNode()
        {
            var job = new JobRequestModel() {Name = "ShouldBeAbleToSendNewJobToAvailableNode", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"};
            Target.NodeInitialized(NodeUri);
            Target.DoThisJob(job);
            Target.Heartbeat(NodeUri);
            HttpSender.CalledNodes.Keys.First()
                .Should()
                .Contain(NodeUri.ToString());
        }

        [Test]
        public void ShouldReturnConflictIfNodeIsBusy()
        {
            var job = new JobRequestModel() {Name = "ShouldBeAbleToSendNewJobToAvailableNode", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"};
            thisNodeIsBusy(NodeUri.ToString());

            Target.Heartbeat(NodeUri);

            Target.DoThisJob(job);

            HttpSender.CalledNodes.Count.Should()
                .Be.EqualTo(0);
        }

        [Test]
        public void ShouldBeAbleToSendNewJobToFirstAvailableNode()
        {
            var job = new JobRequestModel() {Name = "ShouldBeAbleToSendNewJobToFirstAvailableNode", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"};
            thisNodeIsBusy(NodeUri.ToString());

            Target.NodeInitialized(NodeUri);
            Target.NodeInitialized(new Uri("localhost:9051/"));

            Target.DoThisJob(job);

            Target.Heartbeat(NodeUri);

            HttpSender.CalledNodes.Count.Should()
                .Be.EqualTo(2);
            HttpSender.CalledNodes.Keys.First()
                .Should()
                .Contain("localhost:9051/");
        }

        [Test]
        public void ShouldAddNodeReferenceToJObDefinition()
        {
            var job = new JobRequestModel() {Name = "ShouldAddNodeReferenceToJObDefinition", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"};

            Target.NodeInitialized(NodeUri);
            Target.DoThisJob(job);
            Target.Heartbeat(NodeUri);
            JobRepository.LoadAll()
                .First()
                .AssignedNode.Should()
                .Contain(NodeUri.ToString());
        }

        [Test]
        public void ShouldDistributePersistedJobsOnHeartbeat()
        {
            string userName = "ManagerTests";
            var job1Id = Guid.NewGuid();
            var job2Id = Guid.NewGuid();
            var job1 = new JobDefinition() {Id = job1Id, AssignedNode = "local", Name = "job", UserName = userName, Serialized = "Fake Serialized", Type = "Fake Type"};
            var job2 = new JobDefinition() {Id = job2Id, Name = "Job2", UserName = userName, Serialized = "Fake Serialized", Type = "Fake Type"};
            JobRepository.Add(job1);
            JobRepository.Add(job2);

            Target.NodeInitialized(NodeUri);
            Target.Heartbeat(NodeUri);
            HttpSender.CalledNodes.Count.Should()
                .Be.EqualTo(2);
        }

        [Test]
        public void ShouldRemoveAQueuedJob()
        {
            Guid jobId = Guid.NewGuid();
            var job = new JobDefinition() {Name = "", Serialized = "", Type = "", UserName = "ManagerTests", Id = jobId};
            JobRepository.Add(job);
            Target.CancelThisJob(jobId);
            JobRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(0);
        }

        [Test]
        public void ShouldNotRemoveARunningJobFromRepo()
        {
            Guid jobId = Guid.NewGuid();
            var job = new JobDefinition() {Name = " ", Serialized = " ", Type = " ", UserName = "ManagerTests", Id = jobId};
            JobRepository.Add(job);
            JobRepository.CheckAndAssignNextJob(new List<WorkerNode>() {new WorkerNode() {Url = NodeUri.ToString() } },
                                                HttpSender);
            thisNodeIsBusy(NodeUri.ToString());
            Target.CancelThisJob(jobId);
            JobRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(1);
        }

        [Test][Ignore]
        public void ShouldBeAbleToCancelJobOnNode()
        {
            Target.Heartbeat(NodeUri);
            Target.Heartbeat(new Uri("localhost:9051/"));

            Guid jobId = Guid.NewGuid();
            JobRepository.Add(new JobDefinition() {Id = jobId, Serialized = "", Name = "", Type = "", UserName = "ManagerTests"});
            JobRepository.CheckAndAssignNextJob(new List<WorkerNode>() {new WorkerNode() {Url = NodeUri.ToString() }, new WorkerNode() {Url = "localhost:9051/" } },
                                                HttpSender);
            HttpSender.CalledNodes.Clear();
            Target.CancelThisJob(jobId);
            HttpSender.CalledNodes.Count()
                .Should()
                .Be.EqualTo(1);
        }

        [Test]
        public void ShouldRemoveTheJobWhenItsFinished()
        {
            Guid jobId = Guid.NewGuid();
            var job = new JobDefinition() {Id = jobId, AssignedNode = NodeUri.ToString(), Name = "job", Serialized = "", Type = "", UserName = "ShouldRemoveTheJobWhenItsFinished"};
            JobRepository.Add(job);
            Target.JobDone(job.Id);
            JobRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(0);
        }

        [Test]
        public void ShouldSendOkWhenJobDoneSignalReceived()
        {
            Guid jobId = Guid.NewGuid();
            var job = new JobDefinition() {Id = jobId, AssignedNode = NodeUri.ToString(), Name = "job", Serialized = "", Type = "", UserName = "ShouldSendOkWhenJobDoneSignalReceived"};
            JobRepository.Add(job);
            var result = Target.JobDone(job.Id);
            result.Should()
                .Not.Be.Null();
        }


        [Test]
        public void ResetJobsOnFalseClaimOnHeartBeatIfItsFree()
        {
            Guid jobId = Guid.NewGuid();
            string userName = "ManagerTests";
            var job = new JobDefinition() {Id = jobId, Name = "job", UserName = userName, Serialized = "Fake Serialized", Type = "Fake Type"};
            JobRepository.Add(job);
            JobRepository.CheckAndAssignNextJob(new List<WorkerNode>() {new WorkerNode() {Url = NodeUri.ToString() } },
                                                HttpSender);
            Target.Heartbeat(NodeUri);
            HttpSender.CalledNodes.First()
                .Key.Should()
                .Contain(NodeUri.ToString());
        }

        [Test]
        public void ShouldNotAddSameNodeTwiceInInit()
        {
            Target.NodeInitialized(NodeUri);
            Target.NodeInitialized(NodeUri);
            NodeRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(1);
        }

        [Test]
        public void ShouldGetUniqueJobIdWhilePersistingJob()
        {
            Target.DoThisJob(new JobRequestModel() {Name = "ShouldGetUniqueJobIdWhilePersistingJob", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"});
            Target.DoThisJob(new JobRequestModel() {Name = "ShouldGetUniqueJobIdWhilePersistingJob", Serialized = "ngt", Type = "bra", UserName = "ManagerTests"});

            JobRepository.LoadAll()
                .Count.Should()
                .Be.EqualTo(2);
        }

        [Test]
        public void ShouldReturnJobHistoryFromJobId()
        {
            var job = new JobRequestModel() {Name = "Name", Serialized = "Ser", Type = "Type", UserName = "ManagerTests"};

            IHttpActionResult doJobResult = Target.DoThisJob(job);

            var okNegotiatedDoJobResult = doJobResult as OkNegotiatedContentResult<Guid>;
            Guid jobId = okNegotiatedDoJobResult.Content;

            IHttpActionResult getResult = Target.JobHistory(jobId);

            var okNegotiatedGetResult = getResult as OkNegotiatedContentResult<JobHistory>;
            JobHistory jobHistory = okNegotiatedGetResult.Content;
            Assert.IsNotNull(jobHistory);
        }

		private void thisNodeIsBusy(string url)
        {
            HttpSender.BusyNodesUrl.Add(url);
        }
    }
}