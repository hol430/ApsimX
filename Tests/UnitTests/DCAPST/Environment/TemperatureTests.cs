using System;
using System.Collections.Generic;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

using UnitTests.DCAPST.Environment.Fakes;


namespace UnitTests.DCAPST.Environment
{
    [TestFixture]
    public class TemperatureTests
    {
        private Temperature temperature;

        [SetUp]
        public void SetUp()
        {
            var geometry = new FakeSolarGeometry()
            {                
                Sunset = 18.47939124591235,
                DayLength = 12.958782491824698,
            };

            var weather = new FakeWeather()
            {
                MaxT = 28,
                MinT = 16
            };

            temperature = new Temperature();

            var dcapst = new FakeDCAPST();
            dcapst.Children.Add(geometry);
            dcapst.Children.Add(weather);
            dcapst.Children.Add(temperature);

            Apsim.ParentAllChildren(dcapst);

            var links = new Links();
            links.Resolve(dcapst, true);
        }

        [TestCaseSource(typeof(TemperatureTestData), "InvalidTimeTestCases")]
        public void UpdateAirTemperature_IfInvalidTime_ThrowsException(double time)
        {
            // Arrange

            // Act

            // Assert
            Assert.Throws<Exception>(() => temperature.UpdateAirTemperature(time));
        }

        [TestCaseSource(typeof(TemperatureTestData), "ValidTimeTestCases")]
        public void UpdateAirTemperature_IfValidTime_SetsCorrectTemperature(double time, double expected)
        {
            // Arrange

            // Act
            temperature.UpdateAirTemperature(time);
            var actual = temperature.AirTemperature;

            // Assert
            Assert.AreEqual(expected, actual);
        }        
    }
}
