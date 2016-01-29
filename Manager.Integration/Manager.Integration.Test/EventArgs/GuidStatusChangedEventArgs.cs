﻿using System;

namespace Manager.Integration.Test.EventArgs
{
    public class GuidStatusChangedEventArgs : System.EventArgs
    {
        public Guid Guid { get; private set; }
        public string OldStatus { get; private set; }
        public string NewStatus { get; private set; }

        public GuidStatusChangedEventArgs(Guid guid,
                                          string oldStatus,
                                          string newStatus)
        {
            Guid = guid;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}