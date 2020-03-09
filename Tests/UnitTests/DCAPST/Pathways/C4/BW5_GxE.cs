using System;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;
using Models.PMF;
using Models.PMF.Organs;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Pathways
{
    [TestFixture]
    public class C4
    {
        private double delta = 0.0000000000001;

        private DCAPSTModel dcapst;
        private Plant plant;
        private SorghumArbitrator arbitrator;
        private SorghumLeaf sorghum;
        private FakeWeather weather;
        private FakeClock clock;

        [SetUp]
        public void SetUp()
        {
            var psi = 0.4;

            var c4 = new AssimilationC4()
            {
                AirCO2 = 363,                
                AirO2 = 210000,
                MesophyllElectronTransportFraction = 0.4,
                IntercellularToAirCO2Ratio = 0.45,
                ExtraATPCost = 2,
                DiffusivitySolubilityRatio = 0.047,
                PEPRegeneration = 120,
                BundleSheathConductance = 0.003
            };

            var electron = new TemperatureResponseParameters()
            {
                Name = "ElectronTransportRate",
                TMin = 0,
                TOpt = 37.8649150880407,
                TMax = 55,
                B = 1,
                C = 0.711229539802063
            };

            var mesophyll = new TemperatureResponseParameters()
            {
                Name = "MesophyllCO2Conductance",
                TMin = 0,
                TOpt = 42,
                TMax = 55,
                B = 1,
                C = 0.462820450976839
            };

            var response = new TemperatureResponse()
            {
                RubiscoCarboxylationAt25 = 1210,
                RubiscoCarboxylationFactor = 64200,
                RubiscoOxygenationAt25 = 292000,
                RubiscoOxygenationFactor = 10500,
                RubiscoCarboxylationToOxygenationAt25 = 5.51328906454566,
                RubiscoCarboxylationToOxygenationFactor = 21265.4029552906,
                RubiscoActivityFactor = 78000,
                PEPcAt25 = 75,
                PEPcFactor = 36300,
                PEPcActivityFactor = 57043.2677590512,
                RespirationFactor = 46390,
                CurvatureFactor = 0.7,
                SpectralCorrectionFactor = 0.15
            };            

            response.Children.Add(electron);
            response.Children.Add(mesophyll);

            var interaction = new WaterInteraction();

            var sunlit = new AssimilationArea() { Name = "Sunlit" };
            sunlit.Children.Add(new AssimilationPathway() { Type = PathwayType.Ac1, Name = "Ac1" });
            sunlit.Children.Add(new AssimilationPathway() { Type = PathwayType.Ac2, Name = "Ac2" });
            sunlit.Children.Add(new AssimilationPathway() { Type = PathwayType.Aj, Name = "Aj" });

            var shaded = new AssimilationArea() { Name = "Shaded" };
            shaded.Children.Add(new AssimilationPathway() { Type = PathwayType.Ac1, Name = "Ac1" });
            shaded.Children.Add(new AssimilationPathway() { Type = PathwayType.Ac2, Name = "Ac2" });
            shaded.Children.Add(new AssimilationPathway() { Type = PathwayType.Aj, Name = "Aj" });

            var structure = new CanopyStructure()
            {
                DiffuseExtCoeff = 0.78,
                DiffuseExtCoeffNIR = 0.8,
                DiffuseReflectionCoeff = 0.036,
                DiffuseReflectionCoeffNIR = 0.389,
                LeafScatteringCoeff = 0.15,
                LeafScatteringCoeffNIR = 0.8,
                LeafAngle = 60,                
                LeafWidth = 0.15,
                RespirationSLNRatio = 0.0 * psi,
                SLNRatioTop = 1.3,
                MinimumN = 14,
                WindSpeed = 1.5,
                WindSpeedExtinction = 1.5,
                MaxRubiscoActivitySLNRatio = 0.465 * psi,
                MaxElectronTransportSLNRatio = 2.7 * psi,
                MaxPEPcActivitySLNRatio = 1.55 * psi,
                MesophyllCO2ConductanceSLNRatio = 0.0135 * psi
            };

            structure.Children.Add(c4);
            structure.Children.Add(response);
            structure.Children.Add(interaction);
            structure.Children.Add(sunlit);
            structure.Children.Add(shaded);

            weather = new FakeWeather()
            {
                AirPressure = 1.01325
            };

            clock = new FakeClock();
            
            var geometry = new SolarGeometry() 
            { 
                SolarConstant = 1360
            };

            var radiation = new SolarRadiation() 
            { 
                DiffuseFraction = 0.1725,
                RPAR = 0.5 
            };
            
            var temperature = new Temperature()
            {
                XLag = 1.8,
                YLag = 2.2,
                ZLag = 1.0
            };

            dcapst = new DCAPSTModel() { B = 0.409 };
            dcapst.Children.Add(weather);
            dcapst.Children.Add(clock);
            dcapst.Children.Add(geometry);
            dcapst.Children.Add(radiation);
            dcapst.Children.Add(temperature);
            dcapst.Children.Add(structure);

            arbitrator = new SorghumArbitrator();

            sorghum = new SorghumLeaf();
            sorghum.Children.Add(dcapst);

            plant = new Plant();

            plant.AboveGround = new Biomass();
            plant.Root = new Root();

            plant.Children.Add(arbitrator);
            plant.Children.Add(sorghum);

            Apsim.ParentAllChildren(plant);

            var links = new Links();
            links.Resolve(dcapst, true);
        }

        [TestCaseSource(typeof(PathwayTestData), "BW5_GxE_Data")]
        public void BW5_GxE
        (
            int DOY, 
            double latitude, 
            double maxT, 
            double minT, 
            double radn, 
            double RootShootRatio, 
            double SLN, 
            double SWAvailable, 
            double lai,
            double expectedBIOshootDAY,
            double expectedEcanDemand,
            double expectedEcanSupply,
            double expectedRadIntDcaps,
            double expectedBIOshootDAYPot
        )
        {
            clock.Today = new DateTime() + new TimeSpan(DOY - 1, 0, 0, 0);
            weather.Latitude = latitude;
            weather.MaxT = maxT;
            weather.MinT = minT;
            weather.Radn = radn;

            sorghum.SLN = SLN;
            sorghum.LAI = lai;

            /* NOTE: The root-shoot ratio is calculated as AboveGround.Wt / (AboveGround.Wt + Root.Wt).
             * Since this test uses a predetermined value for the root-shoot ratio, we have to initialise
             * the weights in such a way that after the calculation is performed, we return the desired 
             * ratio. This is achieved by setting the numerator to the value we want, and ensuring the 
             * denominator is always 1.
             */

            plant.AboveGround.StructuralWt = RootShootRatio;
            plant.Root.Live.StructuralWt = (1.0 - RootShootRatio);

            arbitrator.WatSupply = SWAvailable;

            dcapst.DailyRun(SWAvailable);

            Assert.AreEqual(expectedBIOshootDAY, dcapst.ActualBiomass, delta);
            Assert.AreEqual(expectedEcanDemand, dcapst.WaterDemanded, delta);
            Assert.AreEqual(expectedEcanSupply, dcapst.WaterSupplied, delta);
            Assert.AreEqual(expectedRadIntDcaps, dcapst.InterceptedRadiation, delta);
            Assert.AreEqual(expectedBIOshootDAYPot, dcapst.PotentialBiomass, delta);
        }
    }
}
