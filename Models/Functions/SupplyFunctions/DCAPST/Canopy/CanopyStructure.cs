using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the structure of the canopy
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IDCAPSTModel))]
    public class CanopyStructure : Model, ICanopyStructure
    {
        /// <summary>
        /// The part of the canopy in sunlight
        /// </summary>
        [Link(ByName = true)]
        public IAssimilationArea Sunlit { get; private set; }

        /// <summary>
        /// The part of the canopy in shade
        /// </summary>
        [Link(ByName = true)]
        public IAssimilationArea Shaded { get; private set; }

        /// <summary>
        /// Models radiation absorbed by the canopy
        /// </summary>
        private AbsorbedRadiation Absorbed { get; set; }

        /// <summary>
        /// Leaf area index of the canopy
        /// </summary>
        private double LAI { get; set; }

        #region Properties

        /// <summary>
        /// Extinction coefficient for diffuse radiation
        /// </summary>
        [Description("Diffuse radiation extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeff { get; set; }

        /// <summary>
        /// Extinction coefficient for near-infrared diffuse radiation
        /// </summary>
        [Description("Diffuse NIR extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeffNIR { get; set; }

        /// <summary>
        /// Reflection coefficient for diffuse radiation
        /// </summary>
        [Description("Diffuse radiation reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeff { get; set; }

        /// <summary>
        /// Reflection coefficient for near-infrared diffuse radiation
        /// </summary>
        [Description("Diffuse NIR reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeffNIR { get; set; }

        /// <summary>
        /// Canopy average leaf inclination relative to the horizontal (degrees)
        /// </summary>
        [Description("Average leaf angle (relative to horizontal)")]
        [Units("Degrees")]
        public double LeafAngle { get; set; }

        /// <summary>
        /// The leaf width in the canopy
        /// </summary>
        [Description("Average leaf width")]
        [Units("")]
        public double LeafWidth { get; set; }

        /// <summary>
        /// Leaf-level coefficient of scattering radiation
        /// </summary>
        [Description("Leaf-level coefficient of scattering radiation")]
        [Units("")]
        public double LeafScatteringCoeff { get; set; }

        /// <summary>
        /// Leaf-level coefficient of near-infrared scattering radiation
        /// </summary>
        [Description("Leaf-level coefficient of scattering NIR")]
        [Units("")]
        public double LeafScatteringCoeffNIR { get; set; }

        /// <summary>
        /// Local wind speed
        /// </summary>
        [Description("Local wind speed")]
        [Units("")]
        public double WindSpeed { get; set; }

        /// <summary>
        /// Extinction coefficient for local wind speed
        /// </summary>
        [Description("Wind speed extinction coefficient")]
        [Units("")]
        public double WindSpeedExtinction { get; set; }

        /// <summary>
        /// The minimum nitrogen value at or below which CO2 assimilation rate is zero (mmol N m^-2)
        /// </summary>
        [Description("Minimum nitrogen for assimilation")]
        [Units("")]
        public double MinimumN { get; set; }

        /// <summary>
        /// Ratio of Rubisco activity to SLN
        /// </summary>
        [Description("Ratio of SLN to max Rubisco activity")]
        [Units("")]
        double MaxRubiscoActivitySLNRatio { get; set; }

        /// <summary>
        /// Ratio of respiration to SLN
        /// </summary>
        [Description("Ratio of SLN to respiration")]
        [Units("")]
        public double RespirationSLNRatio { get; set; }

        /// <summary>
        /// Ratio of electron transport to SLN
        /// </summary>
        [Description("Ratio of SLN to max electron transport")]
        [Units("")]
        double MaxElectronTransportSLNRatio { get; set; }

        /// <summary>
        /// Ratio of PEPc Activity to SLN
        /// </summary>
        [Description("Ratio of SLN to max PEPc activity")]
        [Units("")]
        double MaxPEPcActivitySLNRatio { get; set; }

        /// <summary>
        /// Ratio of Mesophyll CO2 conductance to SLN
        /// </summary>
        [Description("Ratio of SLN to Mesophyll CO2 conductance")]
        [Units("")]
        double MesophyllCO2ConductanceSLNRatio { get; set; }        

        /// <summary>
        /// Ratio of the average canopy specific leaf nitrogen (SLN) to the SLN at the top of canopy (g N m^-2 leaf)
        /// </summary>
        [Description("Ratio of average SLN to canopy top SLN")]
        [Units("")]
        public double SLNRatioTop { get; set; }
        
        #endregion

        /// <summary>
        /// Nitrogen at the top of the canopy
        /// </summary>
        private double LeafNTopCanopy { get; set; }

        /// <summary>
        /// Coefficient of nitrogen allocation through the canopy
        /// </summary>
        private double NAllocation { get; set; }

        /// <summary>
        /// The number of layers in the canopy
        /// </summary>
        public int Layers { get; set; } = 1;

        /// <summary>
        /// Establishes the initial conditions for the daily photosynthesis calculation
        /// </summary>
        public void InitialiseDay(double lai, double sln)
        {
            LAI = lai;

            var SLNTop = sln * SLNRatioTop;
            LeafNTopCanopy = SLNTop * 1000 / 14;

            var NcAv = sln * 1000 / 14;
            NAllocation = -1 * Math.Log((NcAv - MinimumN) / (LeafNTopCanopy - MinimumN)) * 2;           

            Absorbed = new AbsorbedRadiation(Layers, LAI)
            {
                DiffuseExtinction = DiffuseExtCoeff,
                LeafScattering = LeafScatteringCoeff,
                DiffuseReflection = DiffuseReflectionCoeff
            };         
        }

        /// <summary>
        /// Recalculates canopy parameters for a new time step
        /// </summary>
        public void DoTimestepAdjustment(ISolarRadiation radiation)
        {
            CalcLAI();
            CalcAbsorbedRadiations(radiation);
            CalcMaximumRates();
        }

        /// <summary>
        /// Calculates the LAI for the sunlit/shaded areas of the canopy, based on the position of the sun
        /// </summary>
        private void CalcLAI()
        {
            Sunlit.LAI = Absorbed.CalculateSunlitLAI();
            Shaded.LAI = LAI - Sunlit.LAI;
        }

        /// <summary>
        /// Calculates the radiation absorbed by the canopy, based on the position of the sun
        /// </summary>
        private void CalcAbsorbedRadiations(ISolarRadiation radiation)
        {
            // Set parameters
            Absorbed.DiffuseExtinction = DiffuseExtCoeff;
            Absorbed.LeafScattering = LeafScatteringCoeff;
            Absorbed.DiffuseReflection = DiffuseReflectionCoeff;

            // Photon calculations (used by photosynthesis)
            var photons = Absorbed.CalcTotalRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Sunlit.PhotonCount = Absorbed.CalcSunlitRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Shaded.PhotonCount = photons - Sunlit.PhotonCount;

            // Energy calculations (used by water interaction)
            var PARDirect = radiation.Direct * 0.5 * 1000000;
            var PARDiffuse = radiation.Diffuse * 0.5 * 1000000;
            var NIRDirect = radiation.Direct * 0.5 * 1000000;
            var NIRDiffuse = radiation.Diffuse * 0.5 * 1000000;

            var PARTotalIrradiance = Absorbed.CalcTotalRadiation(PARDirect, PARDiffuse);
            var SunlitPARTotalIrradiance = Absorbed.CalcSunlitRadiation(PARDirect, PARDiffuse);
            var ShadedPARTotalIrradiance = PARTotalIrradiance - SunlitPARTotalIrradiance;

            // Adjust parameters for NIR calculations
            Absorbed.DiffuseExtinction = DiffuseExtCoeffNIR;
            Absorbed.LeafScattering = LeafScatteringCoeffNIR;
            Absorbed.DiffuseReflection = DiffuseReflectionCoeffNIR;

            var NIRTotalIrradiance = Absorbed.CalcTotalRadiation(NIRDirect, NIRDiffuse);
            var SunlitNIRTotalIrradiance = Absorbed.CalcSunlitRadiation(NIRDirect, NIRDiffuse);
            var ShadedNIRTotalIrradiance = NIRTotalIrradiance - SunlitNIRTotalIrradiance;

            Sunlit.AbsorbedRadiation = SunlitPARTotalIrradiance + SunlitNIRTotalIrradiance;
            Shaded.AbsorbedRadiation = ShadedPARTotalIrradiance + ShadedNIRTotalIrradiance;
        }

        /// <summary>
        /// Calculates properties of the canopy, based on how much of the canopy is currently in direct sunlight
        /// </summary>
        private void CalcMaximumRates()
        {
            var coefficient = NAllocation;
            var sunlitCoefficient = NAllocation + (Absorbed.DirectExtinction * LAI);

            var RubiscoActivity25 = CalcMaximumRate(MaxRubiscoActivitySLNRatio, coefficient);
            Sunlit.At25C.VcMax = CalcMaximumRate(MaxRubiscoActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VcMax = RubiscoActivity25 - Sunlit.At25C.VcMax;

            var Rd25 = CalcMaximumRate(RespirationSLNRatio, coefficient);
            Sunlit.At25C.Rd = CalcMaximumRate(RespirationSLNRatio, sunlitCoefficient);
            Shaded.At25C.Rd = Rd25 - Sunlit.At25C.Rd;

            var JMax25 = CalcMaximumRate(MaxElectronTransportSLNRatio, coefficient);
            Sunlit.At25C.JMax = CalcMaximumRate(MaxElectronTransportSLNRatio, sunlitCoefficient);
            Shaded.At25C.JMax = JMax25 - Sunlit.At25C.JMax;

            var PEPcActivity25 = CalcMaximumRate(MaxPEPcActivitySLNRatio, coefficient);
            Sunlit.At25C.VpMax = CalcMaximumRate(MaxPEPcActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VpMax = PEPcActivity25 - Sunlit.At25C.VpMax;

            var MesophyllCO2Conductance25 = CalcMaximumRate(MesophyllCO2ConductanceSLNRatio, coefficient);
            Sunlit.At25C.Gm = CalcMaximumRate(MesophyllCO2ConductanceSLNRatio, sunlitCoefficient);
            Shaded.At25C.Gm = MesophyllCO2Conductance25 - Sunlit.At25C.Gm;
        }

        /// <summary>
        /// 
        /// </summary>
        private double CalcMaximumRate(double psi, double coefficient)
        {
            var factor = LAI * (LeafNTopCanopy - MinimumN) * psi;
            var exp = Absorbed.CalcExp(coefficient / LAI);

            return factor * exp / coefficient;
        }

        /// <summary>
        /// Find the total heat conductance across the boundary of the canopy
        /// </summary>
        public double CalcBoundaryHeatConductance()
        {
            var a = 0.5 * WindSpeedExtinction;
            var b = 0.01 * Math.Pow(WindSpeed / LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * LAI);

            return b * c / a;
        }

        /// <summary>
        /// Find the heat conductance across the boundary of the sunlit area of the canopy
        /// </summary>
        public double CalcSunlitBoundaryHeatConductance()
        {
            var a = 0.5 * WindSpeedExtinction + Absorbed.DirectExtinction;
            var b = 0.01 * Math.Pow(WindSpeed / LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * LAI);            

            return b * c / a;
        }
                
        /// <summary>
        /// Calculates how the movement of the sun affects the absorbed radiation
        /// </summary>
        public void DoSolarAdjustment(double sunAngle)
        {        
            // Beam Extinction Coefficient
            if (sunAngle > 0)
                Absorbed.DirectExtinction = CalcShadowProjection(sunAngle) / Math.Sin(sunAngle);
            else
                Absorbed.DirectExtinction = 0;            
        }

        /// <summary>
        /// Calculates the radiation intercepted by the current layer of the canopy
        /// </summary>
        public double GetInterceptedRadiation()
        {
            // Intercepted radiation
            return Absorbed.CalcInterceptedRadiation();

            // TODO: Make this work with multiple layers 
            // (by subtracting the accumulated intercepted radiation of the previous layers) e.g:
            // InterceptedRadiation_1 = Absorbed.CalcInterceptedRadiation() - InterceptedRadiation_0;
        }

        /// <summary>
        /// Calculates the geometry of the shadows across the canopy
        /// </summary>
        private double CalcShadowProjection(double sunAngle)
        {
            var leafAngle = LeafAngle.ToRadians();

            if (leafAngle <= sunAngle)
            {
                return Math.Cos(leafAngle) * Math.Sin(sunAngle);
            }
            else
            {
                double theta = Math.Acos(1 / Math.Tan(leafAngle) * Math.Tan(sunAngle));

                var a = 2 / Math.PI * Math.Sin(leafAngle) * Math.Cos(sunAngle) * Math.Sin(theta);
                var b = (1 - theta * 2 / Math.PI) * Math.Cos(leafAngle) * Math.Sin(sunAngle);
                return a + b;
            }
        }
    }
}
