using System;
using System.Collections.Generic;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Environment
{
    [TestFixture]
    public class SolarGeometryTests
    {
        private SolarGeometry solar;

        [SetUp]
        public void SetUp()
        {
            solar = new SolarGeometry();

            var weather = new FakeWeather()
            {
                Latitude = 18.3
            };

            var clock = new FakeClock()
            {
                Today = new DateTime() + new TimeSpan(143, 0, 0, 0)
            };

            var dcapst = new FakeDCAPST();
            dcapst.Children.Add(weather);
            dcapst.Children.Add(clock);
            dcapst.Children.Add(solar);

            Apsim.ParentAllChildren(dcapst);

            var links = new Links();
            links.Resolve(dcapst, true);            
        }

        [TestCase(12.958782491824698)]
        public void DayLength_AfterDailyInitialise_MatchesExpectedValue(double expected)
        {
            // Arrange

            // Act
            solar.InitialiseDay();
            var actual = solar.DayLength;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCase(5.5206087540876512)]
        public void SunriseTest(double expected)
        {
            // Arrange

            // Act
            solar.InitialiseDay();
            var actual = solar.Sunrise;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCase(18.47939124591235)]
        public void SunsetTest(double expected)
        {
            // Arrange

            // Act
            solar.InitialiseDay();
            var actual = solar.Sunset;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(typeof(SolarGeometryTestData), "SunAngleTestCases")]
        public void SunAngle_AtTimeOfDay_MatchesExpectedValue(double hour, double expected)
        {
            // Arrange

            // Act
            solar.InitialiseDay();
            var actual = solar.SunAngle(hour) * 180 / Math.PI;

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
