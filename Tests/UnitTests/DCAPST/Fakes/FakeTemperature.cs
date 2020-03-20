using System;
using System.Collections.Generic;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Environment.Fakes
{
    public class FakeTemperature : Model, ITemperature
    {
        public double AirMolarDensity { get; set; }

        public double AirTemperature { get; set; }

        public void UpdateAirTemperature(double time)
        {
            throw new NotImplementedException();
        }
    }
}
