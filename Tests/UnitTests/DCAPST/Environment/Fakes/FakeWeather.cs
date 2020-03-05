using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models.Interfaces;
using Models.Functions.SupplyFunctions.DCAPST;

namespace UnitTests.DCAPST.Environment.Fakes
{
    public class FakeWeather : Model, IWeather
    {
        public DateTime StartDate => throw new NotImplementedException();

        public DateTime EndDate => throw new NotImplementedException();

        public double MaxT => throw new NotImplementedException();

        public double MinT => throw new NotImplementedException();

        public double MeanT => throw new NotImplementedException();

        public double VPD => throw new NotImplementedException();

        public double Rain => throw new NotImplementedException();

        public double Radn { get; set; }

        public double VP => throw new NotImplementedException();

        public double Wind => throw new NotImplementedException();

        public double CO2 => throw new NotImplementedException();

        public double AirPressure => throw new NotImplementedException();

        public double Latitude { get; set; }

        public double Tav => throw new NotImplementedException();

        public double Amp => throw new NotImplementedException();

        public double CalculateDayLength(double Twilight)
        {
            throw new NotImplementedException();
        }
    }
}
