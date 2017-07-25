using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.ApdexCalculator
{
    public class Measure
    {
        public string Name { get; set; }
        public int TolerationThreshold { get; set; }
        public int FrustrationThreshold { get; set; }
    }
}
