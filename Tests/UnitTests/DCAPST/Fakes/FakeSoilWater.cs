using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models.Interfaces;
using Models.Soils;

namespace UnitTests.DCAPST.Fakes
{
    public class FakeSoilWater : Model, ISoilWater
    {
        public double[] Thickness => throw new NotImplementedException();

        public double[] SW { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double[] SWmm => throw new NotImplementedException();

        public double[] ESW { get; } = new double[1];

        public double Eos => throw new NotImplementedException();

        public double Es => throw new NotImplementedException();

        public double Eo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double Runoff => throw new NotImplementedException();

        public double Drainage => throw new NotImplementedException();

        public double Salb => throw new NotImplementedException();

        public double[] LateralOutflow => throw new NotImplementedException();

        public double LeachNO3 => throw new NotImplementedException();

        public double LeachNH4 => throw new NotImplementedException();

        public double LeachUrea => throw new NotImplementedException();

        public double[] FlowNO3 => throw new NotImplementedException();

        public double[] FlowNH4 => throw new NotImplementedException();

        public double[] FlowUrea => throw new NotImplementedException();

        public double[] Flow => throw new NotImplementedException();

        public double[] Flux => throw new NotImplementedException();

        public double PotentialInfiltration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double PrecipitationInterception { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void RemoveWater(double[] amountToRemove)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void SetWaterTable(double InitialDepth)
        {
            throw new NotImplementedException();
        }

        public void Tillage(TillageType Data)
        {
            throw new NotImplementedException();
        }

        public void Tillage(string tillageType)
        {
            throw new NotImplementedException();
        }
    }
}
