﻿using System;
using System.Web.Http;
using log4net;
using Manager.IntegrationTest.Console.Host.Log4Net.Extensions;

namespace Manager.IntegrationTest.Console.Host
{
	public class IntegrationController : ApiController
	{
		public IntegrationController()
		{
			WhoAmI = "[INTEGRATION CONTROLLER, " + Environment.MachineName.ToUpper() + "]";

			this.Log().InfoWithLineNumber(WhoAmI);
		}

		public string WhoAmI { get; set; }

		[HttpPost, Route("appdomain/managers")]
		public IHttpActionResult StartNewManager()
		{
			this.Log().DebugWithLineNumber("Called API controller.");

			string friendlyname;

			Program.StartNewManager(out friendlyname);

			return Ok(friendlyname);
		}

		[HttpPost, Route("appdomain/nodes")]
		public IHttpActionResult StartNewNode()
		{
			this.Log().InfoWithLineNumber("StartNewNode.");

			string friendlyname;

			Program.StartNewNode(out friendlyname);

			return Ok(friendlyname);
		}

		[HttpDelete, Route("appdomain/managers/{id}")]
		public IHttpActionResult DeleteManager(string id)
		{
			this.Log().InfoWithLineNumber("DeleteManager");

			if (string.IsNullOrEmpty(id))
			{
				this.Log().WarningWithLineNumber("Bad request, id : " + id);
				return BadRequest(id);
			}

			this.Log().DebugWithLineNumber("Try shut down Manager with id : " + id);

			var success = Program.ShutDownManager(id);

			if (success)
			{
				this.Log().InfoWithLineNumber("Manager has been shut down, with id : " + id);

				return Ok(id);
			}

			this.Log().WarningWithLineNumber("Id not found, id : " + id);

			return NotFound();
		}

		[HttpDelete, Route("appdomain/nodes/{id}")]
		public IHttpActionResult DeleteNode(string id)
		{
			this.Log().InfoWithLineNumber("Delete Node");

			if (string.IsNullOrEmpty(id))
			{
				this.Log().WarningWithLineNumber("Bad request, id : " + id);
				return BadRequest(id);
			}

			this.Log().DebugWithLineNumber("Try shut down Node with id : " + id);

			var success = Program.ShutDownNode(id);

			if (success)
			{
				this.Log().InfoWithLineNumber("Node has been shut down, with id : " + id);

				return Ok(id);
			}

			this.Log().WarningWithLineNumber("Id not found, id : " + id);

			return NotFound();
		}

		[HttpGet, Route("appdomain/managers")]
		public IHttpActionResult GetAllManagers()
		{
			this.Log().DebugWithLineNumber("GetAllManagers");

			var appDomainsList = Program.GetAllmanagers();

			return Ok(appDomainsList);
		}

		[HttpGet, Route("appdomain/nodes")]
		public IHttpActionResult GetAllNodes()
		{
			this.Log().DebugWithLineNumber("GetAllNodes");

			var appDomainsList = Program.GetAllNodes();

			return Ok(appDomainsList);
		}
	}
}