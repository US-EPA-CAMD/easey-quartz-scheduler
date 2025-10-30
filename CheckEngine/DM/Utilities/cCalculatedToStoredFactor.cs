namespace ECMPS.DM.Utilities
{

    /// <summary>
    /// This class returns the factors used in formulas.
    /// </summary>
    public static class cCalculatedToStoredFactor
    {
        /// <summary>
        /// The factor to convert the calculated HCl Electrical Output (EO) Rate (lb/MWh) to lb/MWh.
        /// </summary>
        public static int Hclreo { get { return 1; } }

        /// <summary>
        /// The factor to convert the calculated HCl Heat Input (HI) Rate (lb/mmBtu) to lb/mmBtu.
        /// </summary>
        public static int Hclrhi { get { return 1; } }

        /// <summary>
        /// The factor to convert the calculated HF Electrical Output (EO) Rate (lb/MWh) to lb/MWh.
        /// </summary>
        public static int Hfreo { get { return 1; } }

        /// <summary>
        /// The factor to convert the calculated HF Heat Input (HI) Rate (lb/mmBtu) to lb/mmBtu.
        /// </summary>
        public static int Hfrhi { get { return 1; } }

        /// <summary>
        /// The factor to convert the calculated Hg Electrical Output (EO) Rate (lb/MWh) to lb/GWh.
        /// </summary>
        public static int Hgreo { get { return 1000; } }

        /// <summary>
        /// The factor to convert the calculated Hg Heat Input (HI) Rate (lb/mmBtu) to lb/TBtu.
        /// </summary>
        public static int Hgrhi { get { return 1000000; } }

    }
}
