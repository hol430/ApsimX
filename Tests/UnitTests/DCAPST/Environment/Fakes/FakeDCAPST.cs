using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

namespace UnitTests.DCAPST.Environment.Fakes
{
    class FakeDCAPST : Model, IDCAPSTModel
    {
        public void DailyRun(double lai, double SLN, double soilWater, double RootShootRatio, double MaxHourlyTRate = 100)
        {
            throw new NotImplementedException();
        }
    }
}
