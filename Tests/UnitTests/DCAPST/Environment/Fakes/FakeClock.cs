using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models;
using Models.Functions.SupplyFunctions.DCAPST;

namespace UnitTests.DCAPST.Environment.Fakes
{
    public class FakeClock : Model, IClock
    {
        public DateTime Today { get; set; }

        public double FractionComplete => throw new NotImplementedException();
    }
}
