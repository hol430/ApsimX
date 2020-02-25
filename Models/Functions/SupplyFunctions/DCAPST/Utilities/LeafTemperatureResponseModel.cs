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
    [ValidParent(ParentType = typeof(AssimilationPathway))]
    public class LeafTemperatureResponseModel : Model
    {
        /// <summary>
        /// The canopy which is calculating leaf temperature properties
        /// </summary>
        [Link(Type = LinkType.Ancestor)]
        IPartialCanopy Partial = null;

        /// <summary>
        /// The parameters describing the pathways
        /// </summary>
        [Link]
        IPathwayParameters Pathway = null;

        /// <summary>
        /// The current leaf temperature
        /// </summary>
        public double Temperature { get; set; } = 0;

        /// <summary>
        /// Maximum rate of rubisco carboxylation at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VcMaxT => Value(Temperature, Partial.At25C.VcMax, Pathway.RubiscoActivity.Factor);

        /// <summary>
        /// Leaf respiration at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double RdT => Value(Temperature, Partial.At25C.Rd, Pathway.Respiration.Factor);

        /// <summary>
        /// Maximum rate of electron transport at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double JMaxT => ValueOptimum(Temperature, Partial.At25C.JMax, Pathway.ElectronTransportRateParams);

        /// <summary>
        /// Maximum PEP carboxylase activity at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VpMaxT => Value(Temperature, Partial.At25C.VpMax, Pathway.PEPcActivity.Factor);

        /// <summary>
        /// Mesophyll conductance at the current leaf temperature (mol CO2 m^-2 ground s^-1 bar^-1)
        /// </summary>
        public double GmT => ValueOptimum(Temperature, Partial.At25C.Gm, Pathway.MesophyllCO2ConductanceParams);

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for CO2 (microbar)
        /// </summary>
        public double Kc => Value(Temperature, Pathway.RubiscoCarboxylation.At25, Pathway.RubiscoCarboxylation.Factor);

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for O2 (microbar)
        /// </summary>
        public double Ko => Value(Temperature, Pathway.RubiscoOxygenation.At25, Pathway.RubiscoOxygenation.Factor);

        /// <summary>
        /// Ratio of Rubisco carboxylation to Rubisco oxygenation
        /// </summary>
        public double VcVo => Value(Temperature, Pathway.RubiscoCarboxylationToOxygenation.At25, Pathway.RubiscoCarboxylationToOxygenation.Factor);

        /// <summary>
        /// Michaelis-Menten constant of PEP carboxylase for CO2 (micro bar)
        /// </summary>
        public double Kp => Value(Temperature, Pathway.PEPc.At25, Pathway.PEPc.Factor);

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
        private double ValueOptimum(double temp, double P25, LeafTemperatureParameters p)
        {
            double alpha = Math.Log(2) / (Math.Log((p.TMax - p.TMin) / (p.TOpt - p.TMin)));
            double numerator = 2 * Math.Pow((temp - p.TMin), alpha) * Math.Pow((p.TOpt - p.TMin), alpha) - Math.Pow((temp - p.TMin), 2 * alpha);
            double denominator = Math.Pow((p.TOpt - p.TMin), 2 * alpha);
            double funcT = P25 * Math.Pow(numerator / denominator, p.Beta) / p.C;

            return funcT;
        }

        /// <summary>
        /// Calculates the electron transport rate of the leaf
        /// </summary>
        private double CalcElectronTransportRate()
        {
            var factor = Partial.PhotonCount * (1.0 - Pathway.SpectralCorrectionFactor) / 2.0;
            return (factor + JMaxT - Math.Pow(Math.Pow(factor + JMaxT, 2) - 4 * Pathway.CurvatureFactor * JMaxT * factor, 0.5))
            / (2 * Pathway.CurvatureFactor);
        }
    }

    /// <summary>
    /// Describes parameters used in leaf temperature calculations
    /// </summary>
    public struct LeafTemperatureParameters
    {
        /// <summary>
        /// 
        /// </summary>
        public double C;

        /// <summary>
        /// The maximum temperature
        /// </summary>
        public double TMax;
        
        /// <summary>
        /// The minimum temperature
        /// </summary>
        public double TMin;
        
        /// <summary>
        /// The optimum temperature
        /// </summary>
        public double TOpt;

        /// <summary>
        /// 
        /// </summary>
        public double Beta;
    }
}
