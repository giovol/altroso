using System;
using System.Collections.Generic;
using System.Text;
using altroso.GUI.DE;
using Cosmos.System;

namespace altroso.Core
{
    public class Program : Kernel
    {
        public static string OS_Name = "altroso";
        public static string OS_Version = "debug";
        public static string Username, Hostname;
        protected override void BeforeRun()
        {
            Hostname = "cosmos";
            DesktopEnvironment.Start(OS_Name, OS_Version);
        }
        
        protected override void Run()
        {
			
        }
    }
}
