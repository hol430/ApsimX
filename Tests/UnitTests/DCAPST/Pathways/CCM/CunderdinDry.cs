using System;
using NUnit.Framework;

using Models.Core;
using Models.Functions.SupplyFunctions.DCAPST;
using Models.PMF;
using Models.PMF.Organs;
using Models.Soils;

using UnitTests.DCAPST.Environment.Fakes;

namespace UnitTests.DCAPST.Pathways
{
    [TestFixture]
    public class CCM
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
            var psi = 1.0;

            var ccm = new AssimilationCCM()
            {
                AirCO2 = 370,
                AirO2 = 210000,
                MesophyllElectronTransportFraction = 0.4,
                IntercellularToAirCO2Ratio = 0.7,
                ExtraATPCost = 0.75,
                DiffusivitySolubilityRatio = 0.047,
                PEPRegeneration = 400,
                BundleSheathConductance = 0.5,
                PS2ActivityFraction = 0.1
            };

            ccm.MesophyllElectronTransportFraction = ccm.ExtraATPCost / (3.0 + ccm.ExtraATPCost);
            var fcyc = 0.25 * ccm.ExtraATPCost; // Fraction of cyclic electron flow
            ccm.ATPProductionElectronTransportFactor = (3.0 - fcyc) / (4.0 * (1.0 - fcyc));

            var electron = new TemperatureResponseParameters()
            {
                Name = "ElectronTransportRate",
                TMin = 0.0,
                TOpt = 30.0,
                TMax = 45.0,
                B = 1.0,
                C = 0.911017958600129
            };

            var mesophyll = new TemperatureResponseParameters()
            {
                Name = "MesophyllCO2Conductance",
                TMin = 0.0,
                TOpt = 29.2338417788683,
                TMax = 45.0,
                B = 1,
                C = 0.875790608584141
            };

            var response = new TemperatureResponse()
            {
                RubiscoCarboxylationAt25 = 273.422964228666,
                RubiscoCarboxylationFactor = 93720.0,
                RubiscoOxygenationAt25 = 165824.064155384,
                RubiscoOxygenationFactor = 33600.0,
                RubiscoCarboxylationToOxygenationAt25 = 4.59217066521612,
                RubiscoCarboxylationToOxygenationFactor = 35713.1987127717,
                RubiscoActivityFactor = 65330.0,
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
                LeafWidth = 0.05,
                RespirationSLNRatio = 0.0 * psi,
                SLNRatioTop = 1.3,
                MinimumN = 14,
                WindSpeed = 1.5,
                WindSpeedExtinction = 1.5,
                MaxRubiscoActivitySLNRatio = 1.1 * psi,
                MaxElectronTransportSLNRatio = 1.9484 * psi,
                MaxPEPcActivitySLNRatio = 0.373684157583268 * psi,
                MesophyllCO2ConductanceSLNRatio = 0.00412 * psi
            };

            structure.Children.Add(ccm);
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

        [TestCaseSource(typeof(PathwayTestData), "CunderdinDryData")]
        public void CunderdinDry
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

            /* NOTE: The Cunderdin data comes from a wheat crop. At the time of writing, there is no wheat 
             * model in ApsimX which implements the required SLN and LAI properties. The SorghumLeaf model
             * is being used as a placeholder to inject the necessary parameters.
             */

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

            dcapst.DailyRun();

            Assert.AreEqual(expectedBIOshootDAY, dcapst.ActualBiomass, delta);
            Assert.AreEqual(expectedEcanDemand, dcapst.WaterDemanded, delta);
            Assert.AreEqual(expectedEcanSupply, dcapst.WaterSupplied, delta);
            Assert.AreEqual(expectedRadIntDcaps, dcapst.InterceptedRadiation, delta);
            Assert.AreEqual(expectedBIOshootDAYPot, dcapst.PotentialBiomass, delta);
        }
    }
}
