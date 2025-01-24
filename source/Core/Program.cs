using System;
using System.Collections.Generic;
using System.Text;
using altroso.GUI.DE;
using Cosmos.System;

namespace altroso.Core
{
    public class Program : Kernel
    {
        protected override void BeforeRun()
        {
            DesktopEnvironment.Start("altroso", "0.1");
        }
        
        protected override void Run()
        {
			
        }
    }
}
