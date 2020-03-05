using System;
using System.Collections.Generic;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Environment
{
    [TestFixture]
    public class RadiationTests
    {
        private FakeSolarGeometry geometry;

        private SolarRadiation radiation;

        [SetUp]
        public void SetUp()
        {
            geometry = new FakeSolarGeometry()
            {
                Sunrise = 5.5206087540876512,
                Sunset = 18.47939124591235,
                DayLength = 12.958782491824698,
                SolarConstant = 1360
            };

            radiation = new SolarRadiation()
            {
                RPAR = 0.5
            };

            var dcapst = new FakeDCAPST();
            dcapst.Children.Add(geometry);
            dcapst.Children.Add(radiation);

            var test = new Simulation()
            {
                Name = "Test",
                Children = new List<Model>()
                {
                    new FakeWeather()
                    {
                        Radn = 16.5
                    },
                    new MockClock(),
                    new MockSummary()
                }
            };
            test.Children.Add(dcapst);
            Apsim.ParentAllChildren(test);

            var links = new Links();
            links.Resolve(test, true);         
        }

        [TestCaseSource(typeof(RadiationTestData), "HourlyRadiationTestCases")]
        public void HourlyRadiation_WhenTimeOutOfBounds_ThrowsException(double time, double sunAngle)
        {
            // Arrange            
            geometry.ExpectedAngle = sunAngle;

            // Act

            // Assert
            Assert.Throws<Exception>(() => radiation.UpdateRadiationValues(time));
        }

        [TestCaseSource(typeof(RadiationTestData), "IncidentRadiationTestCases")]
        public void IncidentRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            geometry.ExpectedAngle = sunAngle;

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Total;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(typeof(RadiationTestData), "DiffuseRadiationTestCases")]
        public void DiffuseRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            geometry.ExpectedAngle = sunAngle;

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Diffuse;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(typeof(RadiationTestData), "DirectRadiationTestCases")]
        public void DirectRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            geometry.ExpectedAngle = sunAngle;

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Direct;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(typeof(RadiationTestData), "DiffuseRadiationParTestCases")]
        public void DiffuseRadiationPAR_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            geometry.ExpectedAngle = sunAngle;

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.DiffusePAR;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(typeof(RadiationTestData), "DirectRadiationParTestCases")]
        public void DirectRadiationPAR_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            geometry.ExpectedAngle = sunAngle;

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.DirectPAR;

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
