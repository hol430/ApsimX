using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Models.Core;
using Models.Functions;

namespace UnitTests.DCAPST.Fakes
{
    public class FakeFunction : Model, IFunction
    {
        public double Value { get; set; }

        double IFunction.Value(int arrayIndex)
        {
            return Value;
        }
    }
}
