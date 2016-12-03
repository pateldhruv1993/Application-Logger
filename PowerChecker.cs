using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;

namespace ApplicationLogger
{
    public class PowerChecker
    {


        public PowerChecker()
        {
            SystemEvents.PowerModeChanged += OnPowerChange;

        }


        

        private void OnPowerChange(object s, PowerModeChangedEventArgs e) 
        {
            Console.WriteLine("---------------------------------------");
            Console.WriteLine(e.ToString());
        }

    }
}