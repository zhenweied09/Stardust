﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Manager.IntegrationTest.Console.Host.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("../../../../Manager/ManagerConsoleHost/bin/debug/")]
        public string ManagerAssemblyLocationFullPath {
            get {
                return ((string)(this["ManagerAssemblyLocationFullPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ManagerConsoleHost.exe")]
        public string ManagerAssemblyName {
            get {
                return ((string)(this["ManagerAssemblyName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("NodeConsoleHost.exe")]
        public string NodeAssemblyName {
            get {
                return ((string)(this["NodeAssemblyName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ManagerConsoleHost.exe.config")]
        public string ManagerConfigurationFileName {
            get {
                return ((string)(this["ManagerConfigurationFileName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("NodeConsoleHost.exe.config")]
        public string NodeConfigurationFileName {
            get {
                return ((string)(this["NodeConfigurationFileName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int NumberOfNodesToStart {
            get {
                return ((int)(this["NumberOfNodesToStart"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("http://localhost:9000/")]
        public string ManagerLocationUri {
            get {
                return ((string)(this["ManagerLocationUri"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("NodeTest.JobHandlers, Version=1.0.0.0")]
        public string HandlerAssembly {
            get {
                return ((string)(this["HandlerAssembly"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://localhost:PORTNUMBER")]
        public string NodeEndpointUriTemplate {
            get {
                return ((string)(this["NodeEndpointUriTemplate"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9050")]
        public int NodeEndpointPortNumberStart {
            get {
                return ((int)(this["NodeEndpointPortNumberStart"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("../../../../ManagerConsoleHost/bin/debug/")]
        public string ManagerConfigurationFileFullPath {
            get {
                return ((string)(this["ManagerConfigurationFileFullPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("../../../../Node/NodeConsoleHost/bin/debug/")]
        public string NodeAssemblyLocationFullPath {
            get {
                return ((string)(this["NodeAssemblyLocationFullPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("../../../../Node/NodeConsoleHost/bin/debug/")]
        public string NodeConfigurationFileFullPath {
            get {
                return ((string)(this["NodeConfigurationFileFullPath"]));
            }
        }
    }
}
