using System;
using System.Collections.Generic;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Environment
{
    public class WaterInteractionTests
    {
        private WaterInteraction water;

        [SetUp]
        public void SetUp()
        {
            var weather = new FakeWeather()
            {
                AirPressure = 1010,
                MinT = 16.2
            };

            var temperature = new FakeTemperature()
            {
                AirMolarDensity = 40.63,
                AirTemperature = 27.0
            };

            water = new WaterInteraction();

            var dcapst = new FakeDCAPST();
            dcapst.Children.Add(weather);
            dcapst.Children.Add(temperature);
            dcapst.Children.Add(water);
            Apsim.ParentAllChildren(dcapst);

            var links = new Links();
            links.Resolve(dcapst, true);
        }

        [Test]
        public void UnlimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var leafTemp = 27.0;
            var gbh = 0.127634;

            var A = 4.5;
            var Ca = 380.0;
            var Ci = 152.0;

            var expected = 1262.0178666386046;

            // Act
            water.SetConditions(leafTemp, gbh);
            var actual = water.UnlimitedWaterResistance(A, Ca, Ci);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void LimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var leafTemp = 27;
            var gbh = 0.127634;

            var available = 0.15;
            var rn = 230;

            var expected = 340.83946167121144;

            // Act
            water.SetConditions(leafTemp, gbh);
            var actual = water.LimitedWaterResistance(available, rn);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void HourlyWaterUse_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 0.080424818708166368;

            // Act
            water.SetConditions(leafTemp, gbh);
            var actual = water.HourlyWaterUse(rtw, rn);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Gt_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 180;

            var expected = 0.1437732786549164;

            // Act
            water.SetConditions(leafTemp, gbh);
            var actual = water.TotalCO2Conductance(rtw);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Temperature_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 28.732384941224293;

            // Act
            water.SetConditions(leafTemp, gbh);
            var actual = water.LeafTemperature(rtw, rn);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
