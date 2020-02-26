using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the parameters of the leaf necessary to calculate photosynthesis
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ICanopyStructure))]
    public class TemperatureResponse : Model
    {
        #region Links
        /// <summary>
        /// Describes how electron transport rate changes with temperature
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        TemperatureResponseParameters ElectronTransportRate { get; set; }

        /// <summary>
        /// Describes how Mesophyll conductance changes with temperature
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        TemperatureResponseParameters MesophyllCO2Conductance { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Values of parameters at 25 Celsius
        /// </summary>
        private ParameterRates At25C;
        
        /// <summary>
        /// Current temperature
        /// </summary>
        private double Temperature;
        
        /// <summary>
        /// Number of photons reaching the canopy
        /// </summary>
        private double photoncount;

        #endregion

        #region Properties

        /// <summary>
        /// Describes how Rubisco activity changes with temperature
        /// </summary>
        [Description("Rubisco activity temperature response factor")]
        [Units("")]
        public double RubiscoActivityFactor { get; set; }

        /// <summary>
        /// Describes how PEPc activity changes with temperature
        /// </summary>
        [Description("PEPc activity temperature response factor")]
        [Units("")]
        public double PEPcActivityFactor { get; set; }

        /// <summary>
        /// Describes how Respiration changes with temperature
        /// </summary>
        [Description("Respiration temperature response factor")]
        [Units("")]
        public double RespirationFactor { get; set; }

        /// <summary>
        /// Rubisco carboxylation temperature response factor
        /// </summary>
        [Description("Rubisco carboxylation temperature response factor")]
        [Units("")]
        public double RubiscoCarboxylationFactor { get; set; }

        /// <summary>
        /// Rubisco carboxylation rate at 25 Celsius
        /// </summary>
        [Description("Rubisco carboxylation rate at 25 Celsius")]
        [Units("")]
        public double RubiscoCarboxylationAt25 { get; set; }

        /// <summary>
        /// Rubisco oxygenation temperature response factor
        /// </summary>
        [Description("Rubisco oxygenation temperature response factor")]
        [Units("")]
        public double RubiscoOxygenationFactor { get; set; }

        /// <summary>
        /// Rubisco oxygenation rate at 25 Celsius
        /// </summary>
        [Description("Rubisco oxygenation rate at 25 Celsius")]
        [Units("")]
        public double RubiscoOxygenationAt25 { get; set; }

        /// <summary>
        /// Rubisco carboxylation to oxygenation temperature response factor
        /// </summary>
        [Description("Rubisco carboxylation to oxygenation temperature response factor")]
        [Units("")]
        public double RubiscoCarboxylationToOxygenationFactor { get; set; }

        /// <summary>
        /// Rubisco carboxylation to oxygenation rate at 25 Celsius
        /// </summary>
        [Description("Rubisco carboxylation to oxygenation rate at 25 Celsius")]
        [Units("")]
        public double RubiscoCarboxylationToOxygenationAt25 { get; set; }

        /// <summary>
        /// Rubisco carboxylation temperature response factor
        /// </summary>
        [Description("PEPc temperature response factor")]
        [Units("")]
        public double PEPcFactor { get; set; }

        /// <summary>
        /// Rubisco carboxylation rate at 25 Celsius
        /// </summary>
        [Description("PEPc rate at 25 Celsius")]
        [Units("")]
        public double PEPcAt25 { get; set; }

        /// <summary>
        /// Empirical curvature factor
        /// </summary>
        [Description("Empirical curvature factor")]
        [Units("")]
        public double CurvatureFactor { get; set; }

        /// <summary>
        /// Spectral correction factor
        /// </summary>
        [Description("Spectral correction factor")]
        [Units("")]
        double SpectralCorrectionFactor { get; set; }

        #endregion

        #region Parameters

        /// <summary>
        /// Maximum rate of rubisco carboxylation at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VcMaxT => Value(Temperature, At25C.VcMax, RubiscoActivityFactor);

        /// <summary>
        /// Leaf respiration at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double RdT => Value(Temperature, At25C.Rd, RespirationFactor);

        /// <summary>
        /// Maximum rate of electron transport at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double JMaxT => ValueOptimum(Temperature, At25C.JMax, ElectronTransportRate);

        /// <summary>
        /// Maximum PEP carboxylase activity at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VpMaxT => Value(Temperature, At25C.VpMax, PEPcActivityFactor);

        /// <summary>
        /// Mesophyll conductance at the current leaf temperature (mol CO2 m^-2 ground s^-1 bar^-1)
        /// </summary>
        public double GmT => ValueOptimum(Temperature, At25C.Gm, MesophyllCO2Conductance);

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for CO2 (microbar)
        /// </summary>
        public double Kc => Value(Temperature, RubiscoCarboxylationAt25, RubiscoCarboxylationFactor);

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for O2 (microbar)
        /// </summary>
        public double Ko => Value(Temperature, RubiscoOxygenationAt25, RubiscoOxygenationFactor);

        /// <summary>
        /// Ratio of Rubisco carboxylation to Rubisco oxygenation
        /// </summary>
        public double VcVo => Value(Temperature, RubiscoCarboxylationToOxygenationAt25, RubiscoCarboxylationToOxygenationFactor);

        /// <summary>
        /// Michaelis-Menten constant of PEP carboxylase for CO2 (micro bar)
        /// </summary>
        public double Kp => Value(Temperature, PEPcAt25, PEPcFactor);

        /// <summary>
        /// Electron transport rate
        /// </summary>
        public double J => CalcElectronTransportRate();

        /// <summary>
        /// Relative CO2/O2 specificity of Rubisco (bar bar^-1)
        /// </summary>
        public double Sco => Ko / Kc * VcVo;

        /// <summary>
        /// Half the reciprocal of the relative rubisco specificity
        /// </summary>
        public double Gamma => 0.5 / Sco;

        /// <summary>
        /// Mesophyll respiration
        /// </summary>
        public double GmRd => RdT * 0.5;

        #endregion

        #region Methods

        /// <summary>
        /// Provide the conditions which dictate the temperature response
        /// </summary>
        public void SetConditions(ParameterRates rates, double temperature, double photons)
        {
            At25C = rates;
            Temperature = temperature;
            photoncount = photons;
        }

        /// <summary>
        /// Uses an exponential function to model temperature response parameters
        /// </summary>
        /// <remarks>
        /// See equation (1), A. Wu et al (2018) for details
        /// </remarks>
        private double Value(double temp, double P25, double tMin)
        {
            return P25 * Math.Exp(tMin * (temp + 273 - 298.15) / (298.15 * 8.314 * (temp + 273)));
        }

        /// <summary>
        /// Uses a normal distribution to model parameters with an apparent optimum in temperature response
        /// </summary>
        /// /// <remarks>
        /// See equation (2), A. Wu et al (2018) for details
        /// </remarks>
        private double ValueOptimum(double temp, double P25, TemperatureResponseParameters p)
        {
            double alpha = Math.Log(2) / (Math.Log((p.TMax - p.TMin) / (p.TOpt - p.TMin)));
            double numerator = 2 * Math.Pow((temp - p.TMin), alpha) * Math.Pow((p.TOpt - p.TMin), alpha) - Math.Pow((temp - p.TMin), 2 * alpha);
            double denominator = Math.Pow((p.TOpt - p.TMin), 2 * alpha);
            double funcT = P25 * Math.Pow(numerator / denominator, p.B) / p.C;

            return funcT;
        }

        /// <summary>
        /// Calculates the electron transport rate of the leaf
        /// </summary>
        private double CalcElectronTransportRate()
        {
            var factor = photoncount * (1.0 - SpectralCorrectionFactor) / 2.0;
            return (factor + JMaxT - Math.Pow(Math.Pow(factor + JMaxT, 2) - 4 * CurvatureFactor * JMaxT * factor, 0.5))
            / (2 * CurvatureFactor);
        }

        #endregion
    }

    /// <summary>
    /// Describes parameters used in leaf temperature calculations
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(TemperatureResponse))]
    public class TemperatureResponseParameters : Model
    {
        /// <summary>
        /// Empirical constant, balanced to give unity at 25 Celsius
        /// </summary>
        [Description("Empirical balance constant B")]
        public double B;

        /// <summary>
        /// Empirical constant, balanced to give unity at 25 Celsius
        /// </summary>
        [Description("Empirical balance constant C")]
        public double C;

        /// <summary>
        /// The maximum temperature
        /// </summary>
        [Description("Maximum temperature")]
        [Units("°C")]
        public double TMax;

        /// <summary>
        /// The minimum temperature
        /// </summary>
        [Description("Minimum temperature")]
        [Units("°C")]
        public double TMin;

        /// <summary>
        /// The optimum temperature
        /// </summary>
        [Description("Optimum temperature")]
        [Units("°C")]
        public double TOpt;        
    }
}
