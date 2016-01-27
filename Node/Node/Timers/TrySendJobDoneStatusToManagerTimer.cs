﻿using Stardust.Node.Constants;
using Stardust.Node.Extensions;
using Stardust.Node.Interfaces;

namespace Stardust.Node.Timers
{
    public class TrySendJobDoneStatusToManagerTimer : TrySendStatusToManagerTimer
    {
        public TrySendJobDoneStatusToManagerTimer(JobToDo jobToDo,
            INodeConfiguration nodeConfiguration,
            double interval = 10000) : base(jobToDo,
                nodeConfiguration,
                nodeConfiguration.GenerateUri(ManagerRouteConstants.JobDone),
                null,
                interval)
        {
        }
    }
}