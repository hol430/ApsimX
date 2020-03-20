using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

namespace UnitTests.DCAPST.Environment.Fakes
{
    public class FakeSolarGeometry : Model, ISolarGeometry
    {
        public double Sunrise { get; set; }

        public double Sunset { get; set; }

        public double DayLength { get; set; }

        public double SolarConstant { get; set; }

        public double ExpectedAngle { get; set; }

        public void InitialiseDay()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the expected angle value for a given time, and this function will return it.
        /// </summary>
        public double SunAngle(double time)
        {
            return ExpectedAngle;
        }
    }
}
