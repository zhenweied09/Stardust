//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Manager.Integration.Test.WPF
{
    using System;
    using System.Collections.Generic;
    
    public partial class JobHistory
    {
        public System.Guid JobId { get; set; }
        public string Name { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime Created { get; set; }
        public Nullable<System.DateTime> Started { get; set; }
        public Nullable<System.DateTime> Ended { get; set; }
        public string Serialized { get; set; }
        public string Type { get; set; }
        public string SentTo { get; set; }
        public string Result { get; set; }
    }
}
