using System;
using System.Collections.Generic;
using System.Text;
using Cosmos.System;
using Mirage.DE;
using PrismAPI.Graphics.Fonts;
using PrismAPI.Hardware.GPU;

namespace altroso.Core
{
    public class Program : Kernel
    {
        public static Display Screen;
        protected override void BeforeRun()
        {
            DesktopEnvironment.Start("altroso", "0.1");
        }
        
        protected override void Run()
        {

        }
    }
}
