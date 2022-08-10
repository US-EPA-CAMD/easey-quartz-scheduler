using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECMPS.Checks.CheckEngine.SpecialParameterClasses
{

    /// <summary>
    /// Tracks the general information used to validate Fc values for a location, including the range to use for valiation and whether the range is based on fuel values or is the generaic range.
    /// </summary>
    public class FcValidationInfo
    {

        /// <summary>
        /// Initialized the class including setting MaxValue and MinValue to the non-fuel-specific Fc values and setting IsFuelSpecific to false.
        /// </summary>
        /// <param name="nonFuelSpecificMinValue">The Non Fuel Specific minimum value.</param>
        /// <param name="nonFuelSpecificMaxValue">The Non Fuel Specific maximum value.</param>
        /// <param name="isFuelSpecific">Indicates whether the range is fuel specific.  Defaults to false.</param>
        public FcValidationInfo(decimal nonFuelSpecificMinValue, decimal nonFuelSpecificMaxValue, bool isFuelSpecific = false)
        {
            IsFuelSpecific = isFuelSpecific;
            MaxValue = nonFuelSpecificMaxValue;
            MinValue = nonFuelSpecificMinValue;
        }


        /// <summary>
        /// Contains the minimum non-fuel-specific Fc value for the location.
        /// </summary>
        static public decimal? NonFuelSpecificMaxValue { get; set; }

        /// <summary>
        /// Contains the maximum non-fuel-specific Fc value for the location.
        /// </summary>
        static public decimal? NonFuelSpecificMinValue { get; set; }


        /// <summary>
        /// Indicates whether the Fc value range is fueld specific.
        /// </summary>
        public bool IsFuelSpecific { get; private set; }

        /// <summary>
        /// Contains the maximum Fc value for the location.
        /// </summary>
        public decimal? MaxValue { get; private set; }

        /// <summary>
        /// Contains the minimum Fc value for the location.
        /// </summary>
        public decimal? MinValue { get; private set; }


        /// <summary>
        /// If the current range is fuel-specific, updates the range setting the minimum bound to the lower of the new and existing range and the maximum to the higher of the new and existing.
        /// If the current range is not fuel-specific, updates the range to the new max and min.
        /// Updates IsFuleSpecific to true.
        /// </summary>
        /// <param name="minValue">The new minimum fuel-specific value.</param>
        /// <param name="maxValue">The new maximum fuel-specific value.</param>
        public void UpdateToFuelSpecificRange(decimal minValue, decimal maxValue)
        {
            if (IsFuelSpecific)
            {
                if (minValue < MinValue.Value)
                    MinValue = minValue;

                if (maxValue > MaxValue.Value)
                    MaxValue = maxValue;
            }
            else
            {
                MinValue = minValue;
                MaxValue = maxValue;
                IsFuelSpecific = true;
            }
        }

    }

}
