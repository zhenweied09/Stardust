﻿using System.Configuration;
using Autofac;
using NUnit.Framework;
using SharpTestsEx;
using Stardust.Manager;
using Stardust.Manager.Interfaces;

namespace ManagerTest
{
	[TestFixture]
	public class ManagerModuleTest
	{
		private ContainerBuilder _containerBuilder;

		[SetUp]
		public void SetUp()
		{
			_containerBuilder = new ContainerBuilder();
			
			_containerBuilder.RegisterType<NodeManager>().As<INodeManager>();
			_containerBuilder.RegisterType<JobManager>();
			_containerBuilder.RegisterType<HttpSender>().As<IHttpSender>();
			_containerBuilder.RegisterType<ManagerController>();

			_containerBuilder.Register(
				 c => new JobRepository(ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString))
				 .As<IJobRepository>();

			_containerBuilder.Register(
				 c => new WorkerNodeRepository(ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString))
				 .As<IWorkerNodeRepository>();

			

		}

		[Test]
		public void ShouldResolveManagerController()
		{
			using (var ioc = _containerBuilder.Build())
			{
				using (var scope = ioc.BeginLifetimeScope())
				{
					var controller = scope.Resolve<ManagerController>();
					controller.Should().Not.Be.Null();
				}
			}
		}

		[Test]
		public void ShouldResolveNodeManager()
		{
			using (var ioc = _containerBuilder.Build())
			{
				using (var scope = ioc.BeginLifetimeScope())
				{
					scope.Resolve<INodeManager>();
					scope.Resolve<IHttpSender>();
					scope.Resolve<IJobRepository>();
					scope.Resolve<IWorkerNodeRepository>();

					ioc.IsRegistered<INodeManager>().Should().Be.True();
					ioc.IsRegistered<IHttpSender>().Should().Be.True();
					ioc.IsRegistered<IJobRepository>().Should().Be.True();
					ioc.IsRegistered<IWorkerNodeRepository>().Should().Be.True();
				}
			}
		}

		
	}
}
