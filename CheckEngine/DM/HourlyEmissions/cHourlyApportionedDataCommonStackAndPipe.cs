﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ECMPS.DM.Definitions;
using ECMPS.DM.Utilities;

namespace ECMPS.DM.HourlyEmissions
{

  /// <summary>
  /// Handles apportionment for common stack(s) and pipe(s) emissions reports.
  /// </summary>
  public class cHourlyApportionedDataCommonStackAndPipe : cHourlyApportionedData
  {

    #region Public Constructors

    /// <summary>
    /// Creates an instance of the Unit Hourly Apportioned Data class.
    /// </summary>
    /// <param name="hourlyRawData">The raw data on which the apportionment will occur.</param>
    public cHourlyApportionedDataCommonStackAndPipe(cHourlyRawData hourlyRawData)
      : base(hourlyRawData)
    {
    }

    #endregion


    #region Public Override Properties

    /// <summary>
    /// The apportionment type of the class.
    /// </summary>
    public override eApportionmentType ApportionmentType { get { return eApportionmentType.CommonStackAndPipe; } }

    #endregion


    #region Public Override Methods

    /// <summary>
    /// The method that apportions an individual hour.
    /// </summary>
    /// <param name="date">The date of the hour to apportion.</param>
    /// <param name="hour">The hour to apportion.</param>
    /// <returns></returns>
    public override bool Apportion(DateTime date, int hour)
    {
      bool result;

      int? rawHourOffset, apportionHourOffset;

      if (GetHourOffset(date, hour, out apportionHourOffset) &&
          HourlyRawData.GetHourOffset(date, hour, out rawHourOffset))
      {
        decimal?[] pipeMassFactors, stackMassFactors;

        bool[] noxmApportionValue;
        bool[] noxrCalculateValue;

        decimal? noxmSubtract;

        decimal? so2mCommon; cCombineMeasure so2mCommonMeasure;
        decimal? co2mCommon; cCombineMeasure co2mCommonMeasure;
        decimal? noxmCommon; string noxmCommonMeasure;
        decimal? noxrCommon; string noxrCommonMeasure;

        bool[] hgmApportionValue;
        bool[] hclmApportionValue;
        bool[] hfmApportionValue;

        decimal? hgmSubtract;
        decimal? hclmSubtract;
        decimal? hfmSubtract;

        decimal? hgmCommon; string hgmCommonMeasure;
        decimal? hclmCommon; string hclmCommonMeasure;
        decimal? hfmCommon; string hfmCommonMeasure;

        bool pipesHaveValues, stacksHaveValues;

        GetCombinedPipeValues(rawHourOffset.Value,
                              out pipesHaveValues,
                              out so2mCommon, 
                              out so2mCommonMeasure,
                              out co2mCommon, 
                              out co2mCommonMeasure);

        GetCombinedStackValues(rawHourOffset.Value,
                               out stacksHaveValues,
                               out noxmCommon, 
                               out noxmCommonMeasure,
                               out noxrCommon, 
                               out noxrCommonMeasure,
                               out hgmCommon,
                               out hgmCommonMeasure,
                               out hclmCommon,
                               out hclmCommonMeasure,
                               out hfmCommon,
                               out hfmCommonMeasure);

        if (pipesHaveValues || stacksHaveValues)
        {
          // Get the pipe apportionment factors
          GetApportionPipeFactors(rawHourOffset.Value, out pipeMassFactors);

          // Get the stack apportionment factors
          GetApportionStackFactors(rawHourOffset.Value,
                                   out stackMassFactors,
                                   out noxmApportionValue,
                                   out noxmSubtract,
                                   out noxrCalculateValue,
                                   out hgmApportionValue,
                                   out hgmSubtract,
                                   out hclmApportionValue,
                                   out hclmSubtract,
                                   out hfmApportionValue,
                                   out hfmSubtract);

          // Op Time, GLoad, SLoad and TLoad
          CopyOpData(apportionHourOffset.Value, rawHourOffset.Value);

          // HIT Value, Method and Measure
          CopyHit(apportionHourOffset.Value, rawHourOffset.Value);

          // SO2M Value and Measure Flag
          ApportionPipeMass(So2mArray,
                            So2mMeasureArray,
                            HourlyRawData.So2mArray,
                            HourlyRawData.So2mMeasureArray,
                            so2mCommon,
                            so2mCommonMeasure,
                            cDecimalPlaces.So2m,
                            pipeMassFactors,
                            apportionHourOffset.Value,
                            rawHourOffset.Value);

          // SO2R Value and Measure Flag - Always after apportioning SO2M and setting HIT
          ApportionRate(So2rArray,
                        So2rMeasureArray,
                        So2mArray,
                        HitArray,
                        cDecimalPlaces.So2r,
                        apportionHourOffset.Value);

          // CO2M Value and Measure Flag
          ApportionPipeMass(Co2mArray,
                            Co2mMeasureArray,
                            HourlyRawData.Co2mArray,
                            HourlyRawData.Co2mMeasureArray,
                            co2mCommon,
                            co2mCommonMeasure,
                            cDecimalPlaces.Co2m,
                            pipeMassFactors,
                            apportionHourOffset.Value,
                            rawHourOffset.Value);

          // CO2R Value and Measure Flag - Always after apportioning CO2M and setting HIT
          ApportionRate(Co2rArray,
                        Co2rMeasureArray,
                        Co2mArray,
                        HitArray,
                        cDecimalPlaces.Co2r,
                        apportionHourOffset.Value);

          // NoxM Value and Measure Flag
          ApportionStackMass(NoxmArray,
                             NoxmMeasureArray,
                             HourlyRawData.NoxmArray,
                             HourlyRawData.NoxmMeasureArray,
                             noxmApportionValue,
                             noxmSubtract,
                             noxmCommon,
                             noxmCommonMeasure,
                             cDecimalPlaces.Noxm,
                             stackMassFactors,
                             apportionHourOffset.Value,
                             rawHourOffset.Value);

          // NOXR Value and Measure Flag - Always after apportioning NOXM and setting HIT
          ApportionStackNoxr(noxrCalculateValue,
                             noxrCommon,
                             noxrCommonMeasure,
                             apportionHourOffset.Value,
                             rawHourOffset.Value);

          // Try to calculate missing NOxM using calculated NOxR - Always after calcualting NOxR 
          RecheckNoxm(apportionHourOffset.Value);

          // Hg Mass Value and Measure Code
          ApportionStackMass(HgMassArray,
                             HgMeasureArray,
                             HourlyRawData.HgMassArray,
                             HourlyRawData.HgMeasureArray,
                             hgmApportionValue,
                             hgmSubtract,
                             hgmCommon,
                             hgmCommonMeasure,
                             cDecimalPlaces.Hgm,
                             stackMassFactors,
                             apportionHourOffset.Value,
                             rawHourOffset.Value);

          // Hcl Mass Value and Measure Code
          ApportionStackMass(HclMassArray,
                             HclMeasureArray,
                             HourlyRawData.HclMassArray,
                             HourlyRawData.HclMeasureArray,
                             hclmApportionValue,
                             hclmSubtract,
                             hclmCommon,
                             hclmCommonMeasure,
                             cDecimalPlaces.Hclm,
                             stackMassFactors,
                             apportionHourOffset.Value,
                             rawHourOffset.Value);

          // HF Mass Value and Measure Code
          ApportionStackMass(HfMassArray,
                             HfMeasureArray,
                             HourlyRawData.HfMassArray,
                             HourlyRawData.HfMeasureArray,
                             hfmApportionValue,
                             hfmSubtract,
                             hfmCommon,
                             hfmCommonMeasure,
                             cDecimalPlaces.Hfm,
                             stackMassFactors,
                             apportionHourOffset.Value,
                             rawHourOffset.Value);

          // Calculate and fill missing electrical output and heat input based rates.
          FillMissingMatsRates(apportionHourOffset.Value);
        }
        else
        {
          CopyHour(apportionHourOffset.Value, rawHourOffset.Value);

          // Calculate and fill missing electrical output and heat input based rates.
          FillMissingMatsRates(apportionHourOffset.Value);
        }

        result = true;
      }
      else
        result = false;

      return result;
    }

    #endregion


    #region Private Methods

    /// <summary>
    /// Apportions (combined) common pipe mass among the units.
    /// </summary>
    /// <param name="apportionValueArray">The array for the mass's apportioned values.</param>
    /// <param name="apportionMeasureArray">The array for the mass's apporioned measure code.</param>
    /// <param name="rawValueArray">The array for the mass's raw values.</param>
    /// <param name="rawMeasureArray">The array for the mass's raw measure codes.</param>
    /// <param name="combinedValue">The (combined) common stack mass value.</param>
    /// <param name="combinedMeasure">The (combined) common stack mass measure code.</param>
    /// <param name="decimalPlaces">The rounding decimal places for the mass.</param>
    /// <param name="massFactors">An array of the unit factors to use in apportionment.</param>
    /// <param name="apportionHourOffset">Hour offset into the apportionment data arrays.</param>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    private void ApportionPipeMass(decimal?[] apportionValueArray,
                                   string[] apportionMeasureArray,
                                   decimal?[] rawValueArray,
                                   string[] rawMeasureArray,
                                   decimal? combinedValue,
                                   cCombineMeasure combinedMeasure,
                                   int decimalPlaces,
                                   decimal?[] massFactors,
                                   int apportionHourOffset,
                                   int rawHourOffset)
    {
      decimal factorTotal = 0;
      {
        if (combinedValue.HasValue)
        {
          for (int unitDex = 0; unitDex < UnitCount; unitDex++)
            if (massFactors[unitDex].HasValue)
              factorTotal += massFactors[unitDex].Value;
        }
      }

      for (int unitDex = 0; unitDex < UnitCount; unitDex++)
      {
        int apportionPos = unitDex + apportionHourOffset;
        int rawPos = UnitInfo[unitDex].LocationPosition.Value + rawHourOffset;

        if (combinedValue.HasValue && (factorTotal != 0))
        {
          if (rawValueArray[rawPos].HasValue)
          {
            apportionValueArray[apportionPos]
              = Math.Round(rawValueArray[rawPos].Value
                             + (combinedValue.Value * (massFactors[unitDex].Value / factorTotal)),
                           decimalPlaces,
                           MidpointRounding.AwayFromZero);

            apportionMeasureArray[apportionPos] = combinedMeasure.Preview(rawMeasureArray[rawPos]);
          }
          else if (massFactors[unitDex].HasValue)
          {
            apportionValueArray[apportionPos]
              = Math.Round(combinedValue.Value * (massFactors[unitDex].Value / factorTotal),
                           decimalPlaces,
                           MidpointRounding.AwayFromZero);

            apportionMeasureArray[apportionPos] = combinedMeasure.Result;
          }
          else
          {
            apportionValueArray[apportionPos] = rawValueArray[rawPos];
            apportionMeasureArray[apportionPos] = rawMeasureArray[rawPos];
          }
        }
        else
        {
          apportionValueArray[apportionPos] = rawValueArray[rawPos];
          apportionMeasureArray[apportionPos] = rawMeasureArray[rawPos];
        }
      }
    }

    /// <summary>
    /// Apportions (combined) common stack mass among the units.
    /// </summary>
    /// <param name="apportionValueArray">The array for the mass's apportioned values.</param>
    /// <param name="apportionMeasureArray">The array for the mass's apporioned measure code.</param>
    /// <param name="rawValueArray">The array for the mass's raw values.</param>
    /// <param name="rawMeasureArray">The array for the mass's raw measure codes.</param>
    /// <param name="apportionValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="subtractValue">The sum of the mass reported at units.</param>
    /// <param name="combinedValue">The (combined) common stack mass value.</param>
    /// <param name="combinedMeasure">The (combined) common stack mass measure code.</param>
    /// <param name="decimalPlaces">The rounding decimal places for the mass.</param>
    /// <param name="massFactors">An array of the unit factors to use in apportionment.</param>
    /// <param name="apportionHourOffset">Hour offset into the apportionment data arrays.</param>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    private void ApportionStackMass(decimal?[] apportionValueArray,
                                    string[] apportionMeasureArray,
                                    decimal?[] rawValueArray,
                                    string[] rawMeasureArray,
                                    bool[] apportionValue,
                                    decimal? subtractValue,
                                    decimal? combinedValue,
                                    string combinedMeasure,
                                    int decimalPlaces,
                                    decimal?[] massFactors,
                                    int apportionHourOffset,
                                    int rawHourOffset)
    {
      decimal factorTotal = 0;
      {
        if (combinedValue.HasValue)
        {
          for (int unitDex = 0; unitDex < UnitCount; unitDex++)
          {
            if (apportionValue[unitDex])
              factorTotal += massFactors[unitDex].Value;
          }
        }
      }

      for (int unitDex = 0; unitDex < UnitCount; unitDex++)
      {
        int apportionPos = unitDex + apportionHourOffset;
        int rawPos = UnitInfo[unitDex].LocationPosition.Value + rawHourOffset;

        if (combinedValue.HasValue && apportionValue[unitDex] && (factorTotal != 0))
        {
          decimal adjustedValue = (subtractValue.HasValue)
                                ? combinedValue.Value - subtractValue.Value
                                : combinedValue.Value;

          apportionValueArray[apportionPos]
            = Math.Round(adjustedValue * (massFactors[unitDex].Value / factorTotal),
                         decimalPlaces,
                         MidpointRounding.AwayFromZero);

          apportionMeasureArray[apportionPos] = combinedMeasure;
        }
        else
        {
          apportionValueArray[apportionPos] = rawValueArray[rawPos];
          apportionMeasureArray[apportionPos] = rawMeasureArray[rawPos];
        }
      }
    }

    /// <summary>
    /// Apportions (combined) common stack NOXR among the units.
    /// </summary>
    /// <param name="calculateValue">A boolean array indicating whether a particular unit should receive calculated values.</param>
    /// <param name="combinedValue">The (combined) common stack NOXR value.</param>
    /// <param name="combinedMeasure">The (combined) common stack NOXR measure code.</param>
    /// <param name="apportionHourOffset">Hour offset into the apportionment data arrays.</param>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    private void ApportionStackNoxr(bool[] calculateValue,
                                    decimal? combinedValue,
                                    string combinedMeasure,
                                    int apportionHourOffset,
                                    int rawHourOffset)
    {
      for (int unitDex = 0; unitDex < UnitCount; unitDex++)
      {
        int apportionPos = unitDex + apportionHourOffset;
        int rawPos = UnitInfo[unitDex].LocationPosition.Value + rawHourOffset;

        if (calculateValue[unitDex])
        {
          if (HitArray[apportionPos].HasValue && (HitArray[apportionPos].Value > 0) &&
              NoxmArray[apportionPos].HasValue)
          {
            NoxrArray[apportionPos]
              = Math.Round(NoxmArray[apportionPos].Value / HitArray[apportionPos].Value,
                           cDecimalPlaces.Noxr,
                           MidpointRounding.AwayFromZero);

            NoxrMeasureArray[apportionPos] = "CALC";
          }
          else if (combinedValue.HasValue)
          {
            NoxrArray[apportionPos]
              = Math.Round(combinedValue.Value,
                           cDecimalPlaces.Noxr,
                           MidpointRounding.AwayFromZero);

            NoxrMeasureArray[apportionPos] = combinedMeasure;
          }
          else
          {
            NoxrArray[apportionPos] = HourlyRawData.NoxrArray[rawPos];
            NoxrMeasureArray[apportionPos] = HourlyRawData.NoxrMeasureArray[rawPos];
          }
        }
        else
        {
          NoxrArray[apportionPos] = HourlyRawData.NoxrArray[rawPos];
          NoxrMeasureArray[apportionPos] = HourlyRawData.NoxrMeasureArray[rawPos];
        }
      }
    }

    /// <summary>
    /// Returns the pipe apportionment factors, apportion indicators and subtractive values for an hour.
    /// </summary>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    /// <param name="massFactors">The apportionment factors for the hour.</param>
    private void GetApportionPipeFactors(int rawHourOffset,
                                         out decimal?[] massFactors)
    {
      decimal?[] hitFactors = new decimal?[UnitCount];
      decimal?[] gLoadFactors = new decimal?[UnitCount];
      decimal?[] sLoadFactors = new decimal?[UnitCount];
      decimal?[] tLoadFactors = new decimal?[UnitCount];
      decimal?[] opTimeFactors = new decimal?[UnitCount];

      bool canUseHit = true;
      bool canUseGLoad = true;
      bool canUseSLoad = true;
      bool canUseTLoad = true;

      decimal sumHit = 0;
      decimal sumGLoad = 0;
      decimal sumSLoad = 0;
      decimal sumTLoad = 0;

      for (int unitDex = 0; unitDex < UnitCount; unitDex++)
      {
        int rawPos = UnitInfo[unitDex].LocationPosition.Value + rawHourOffset;
        int locationPos = UnitInfo[unitDex].LocationPosition.Value;

        if (HourlyRawData.OpTimeArray[rawPos] > 0)
        {
          if (HourlyRawData.HitArray[rawPos].HasValue &&
              HourlyRawData.HitffArray[rawPos].HasValue)
            hitFactors[unitDex] = HourlyRawData.HitArray[rawPos].Value
                                - HourlyRawData.HitffArray[rawPos].Value;
          else
            hitFactors[unitDex] = HourlyRawData.HitArray[rawPos];

          gLoadFactors[unitDex] = HourlyRawData.GLoadArray[rawPos];
          sLoadFactors[unitDex] = HourlyRawData.SLoadArray[rawPos];
          tLoadFactors[unitDex] = HourlyRawData.TLoadArray[rawPos];
          opTimeFactors[unitDex] = HourlyRawData.OpTimeArray[rawPos];

          sumHit += hitFactors[unitDex].GetValueOrDefault(0);
          sumGLoad += gLoadFactors[unitDex].GetValueOrDefault(0);
          sumSLoad += sLoadFactors[unitDex].GetValueOrDefault(0);
          sumTLoad += tLoadFactors[unitDex].GetValueOrDefault(0);

          if (!hitFactors[unitDex].HasValue) canUseHit = false;
          if (!gLoadFactors[unitDex].HasValue) canUseGLoad = false;
          if (!sLoadFactors[unitDex].HasValue) canUseSLoad = false;
          if (!tLoadFactors[unitDex].HasValue) canUseTLoad = false;
        }
        else
        {
          opTimeFactors[unitDex] = 0; // Since this is the default, explicitly set all elements
        }
      }

      if (canUseHit && (sumHit > 0))
        massFactors = hitFactors;
      else if (canUseGLoad && (sumGLoad > 0))
        massFactors = gLoadFactors;
      else if (canUseSLoad && (sumSLoad > 0))
        massFactors = sLoadFactors;
      else if (canUseTLoad && (sumTLoad > 0))
        massFactors = tLoadFactors;
      else
        massFactors = opTimeFactors;
    }

    /// <summary>
    /// Returns the stack apportionment factors, apportion indicators and subtractive values for an hour.
    /// </summary>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    /// <param name="massFactors">The apportionment factors for the hour.</param>
    /// <param name="noxmApportionValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="noxmSubtract">The sum of the value reported at units.</param>
    /// <param name="noxrCalculateValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="hgmApportionValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="hgmSubtract">The sum of the value reported at units.</param>
    /// <param name="hclmApportionValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="hclmSubtract">The sum of the value reported at units.</param>
    /// <param name="hfmApportionValue">A boolean array indicating whether a particular unit should receive apportioned values.</param>
    /// <param name="hfmSubtract">The sum of the value reported at units.</param>
    private void GetApportionStackFactors(int rawHourOffset,
                                          out decimal?[] massFactors,
                                          out bool[] noxmApportionValue,
                                          out decimal? noxmSubtract,
                                          out bool[] noxrCalculateValue,
                                          out bool[] hgmApportionValue,
                                          out decimal? hgmSubtract,
                                          out bool[] hclmApportionValue,
                                          out decimal? hclmSubtract,
                                          out bool[] hfmApportionValue,
                                          out decimal? hfmSubtract)
    {
      noxmApportionValue = new bool[UnitCount];
      noxmSubtract = 0;
      noxrCalculateValue = new bool[UnitCount];

      hgmApportionValue = new bool[UnitCount];
      hgmSubtract = 0;
      hclmApportionValue = new bool[UnitCount];
      hclmSubtract = 0;
      hfmApportionValue = new bool[UnitCount];
      hfmSubtract = 0;

      decimal?[] hitFactors = new decimal?[UnitCount];
      decimal?[] gLoadFactors = new decimal?[UnitCount];
      decimal?[] sLoadFactors = new decimal?[UnitCount];
      decimal?[] tLoadFactors = new decimal?[UnitCount];
      decimal?[] opTimeFactors = new decimal?[UnitCount];

      bool canUseHit = true;
      bool canUseGLoad = true;
      bool canUseSLoad = true;
      bool canUseTLoad = true;

      for (int unitDex = 0; unitDex < UnitCount; unitDex++)
      {
        int rawPos = UnitInfo[unitDex].LocationPosition.Value + rawHourOffset;
        int locationPos = UnitInfo[unitDex].LocationPosition.Value;

        if (HourlyRawData.OpTimeArray[rawPos] > 0)
        {
          hitFactors[unitDex] = HourlyRawData.HitArray[rawPos];
          gLoadFactors[unitDex] = HourlyRawData.GLoadArray[rawPos];
          sLoadFactors[unitDex] = HourlyRawData.SLoadArray[rawPos];
          tLoadFactors[unitDex] = HourlyRawData.TLoadArray[rawPos];
          opTimeFactors[unitDex] = HourlyRawData.OpTimeArray[rawPos];

          bool mustUseUnit = false;

          if (HourlyRawData.NoxmArray[rawPos].HasValue)
          {
            noxmSubtract += HourlyRawData.NoxmArray[rawPos].Value;
            noxmApportionValue[unitDex] = false;
          }
          else if (HourlyRawData.HitArray[rawPos].HasValue &&
                   HourlyRawData.NoxrArray[rawPos].HasValue)
          {
            noxmApportionValue[unitDex] = false;
            noxmSubtract += Math.Round(HourlyRawData.HitArray[rawPos].Value * HourlyRawData.NoxrArray[rawPos].Value,
                                       cDecimalPlaces.Noxm,
                                       MidpointRounding.AwayFromZero);
          }
          else
          {
            noxmApportionValue[unitDex] = true;
            mustUseUnit = true;
          }

          noxrCalculateValue[unitDex] = !HourlyRawData.NoxrArray[rawPos].HasValue;

          /* Hg */
          if (HourlyRawData.HgMassArray[rawPos].HasValue)
          {
            hgmSubtract += HourlyRawData.HgMassArray[rawPos].Value;
            hgmApportionValue[unitDex] = false;
          }
          else
          {
            hgmApportionValue[unitDex] = true;
            mustUseUnit = true;
          }

          /* HCl */
          if (HourlyRawData.HclMassArray[rawPos].HasValue)
          {
            hclmSubtract += HourlyRawData.HclMassArray[rawPos].Value;
            hclmApportionValue[unitDex] = false;
          }
          else
          {
            hclmApportionValue[unitDex] = true;
            mustUseUnit = true;
          }

          /* HF */
          if (HourlyRawData.HfMassArray[rawPos].HasValue)
          {
            hfmSubtract += HourlyRawData.HfMassArray[rawPos].Value;
            hfmApportionValue[unitDex] = false;
          }
          else
          {
            hfmApportionValue[unitDex] = true;
            mustUseUnit = true;
          }

          if (mustUseUnit)
          {
            if (!hitFactors[unitDex].HasValue) canUseHit = false;
            if (!gLoadFactors[unitDex].HasValue) canUseGLoad = false;
            if (!sLoadFactors[unitDex].HasValue) canUseSLoad = false;
            if (!tLoadFactors[unitDex].HasValue) canUseTLoad = false;
          }
        }
        else
        {
          opTimeFactors[unitDex] = 0; // Since this is the default, explicitly set all elements
          noxmApportionValue[unitDex] = false;
          noxrCalculateValue[unitDex] = false;
          hgmApportionValue[unitDex] = false;
          hclmApportionValue[unitDex] = false;
          hfmApportionValue[unitDex] = false;
        }
      }

      if (canUseHit)
        massFactors = hitFactors;
      else if (canUseGLoad)
        massFactors = gLoadFactors;
      else if (canUseSLoad)
        massFactors = sLoadFactors;
      else if (canUseTLoad)
        massFactors = tLoadFactors;
      else
        massFactors = opTimeFactors;
    }

    /// <summary>
    /// Gets the (combined) common pipe values and measure flags.
    /// </summary>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    /// <param name="hasValues">Indicates that combined values exist.</param>
    /// <param name="so2mCombinedValue">The (combined) common value.</param>
    /// <param name="so2mCombinedMeasure">The (combined) common measure flag.</param>
    /// <param name="co2mCombinedValue">The (combined) common value.</param>
    /// <param name="co2mCombinedMeasure">The (combined) common measure flag.</param>
    private void GetCombinedPipeValues(int rawHourOffset,
                                       out bool hasValues,
                                       out decimal? so2mCombinedValue,
                                       out cCombineMeasure so2mCombinedMeasure,
                                       out decimal? co2mCombinedValue,
                                       out cCombineMeasure co2mCombinedMeasure)
    {
      hasValues = false;

      so2mCombinedValue = null;
      so2mCombinedMeasure = new cCombineMeasure();

      co2mCombinedValue = null;
      co2mCombinedMeasure = new cCombineMeasure();


      for (int locationDex = 0; locationDex < HourlyRawData.LocationCount; locationDex++)
      {
        if (!HourlyRawData.LocationInfo[locationDex].UnitKey.HasValue) //not unit
        {
          int dataPos = locationDex + rawHourOffset;

          // SO2M
          if (HourlyRawData.So2mArray[dataPos].HasValue)
          {
            if (so2mCombinedValue.HasValue)
              so2mCombinedValue += HourlyRawData.So2mArray[dataPos].Value;
            else
              so2mCombinedValue = HourlyRawData.So2mArray[dataPos].Value;

            so2mCombinedMeasure.Combine(HourlyRawData.So2mMeasureArray[dataPos]);

            hasValues = true;
          }

          // CO2M
          if (HourlyRawData.Co2mArray[dataPos].HasValue)
          {
            if (co2mCombinedValue.HasValue)
              co2mCombinedValue += HourlyRawData.Co2mArray[dataPos].Value;
            else
              co2mCombinedValue = HourlyRawData.Co2mArray[dataPos].Value;

            co2mCombinedMeasure.Combine(HourlyRawData.Co2mMeasureArray[dataPos]);

            hasValues = true;
          }
        }
      }
    }

    /// <summary>
    /// Gets the (combined) common stack values and measure flags.
    /// </summary>
    /// <param name="rawHourOffset">Hour offset into the raw daa arrays.</param>
    /// <param name="hasValues">Indicates that combined values exist.</param>
    /// <param name="noxmCombinedValue">The (combined) common value.</param>
    /// <param name="noxmCombinedMeasure">The (combined) common measure flag.</param>
    /// <param name="noxrCombinedValue">The (combined) common value.</param>
    /// <param name="noxrCombinedMeasure">The (combined) common measure flag.</param>
    /// <param name="hgmCombinedValue">The (combined) common value.</param>
    /// <param name="hgmCombinedMeasure">The (combined) common measure flag.</param>
    /// <param name="hclmCombinedValue">The (combined) common value.</param>
    /// <param name="hclmCombinedMeasure">The (combined) common measure flag.</param>
    /// <param name="hfmCombinedValue">The (combined) common value.</param>
    /// <param name="hfmCombinedMeasure">The (combined) common measure flag.</param>
    private void GetCombinedStackValues(int rawHourOffset,
                                        out bool hasValues,
                                        out decimal? noxmCombinedValue,
                                        out string noxmCombinedMeasure,
                                        out decimal? noxrCombinedValue,
                                        out string noxrCombinedMeasure,
                                        out decimal? hgmCombinedValue,
                                        out string hgmCombinedMeasure,
                                        out decimal? hclmCombinedValue,
                                        out string hclmCombinedMeasure,
                                        out decimal? hfmCombinedValue,
                                        out string hfmCombinedMeasure)
    {
      hasValues = false;

      noxmCombinedValue = null;
      noxmCombinedMeasure = null;
      noxrCombinedValue = null;
      noxrCombinedMeasure = null;

      hgmCombinedValue = null;
      hgmCombinedMeasure = null;
      hclmCombinedValue = null;
      hclmCombinedMeasure = null;
      hfmCombinedValue = null;
      hfmCombinedMeasure = null;

      cCombineMeasure noxmMeasureObject = new cCombineMeasure();
      cCombineMeasure noxrMeasureObject = new cCombineMeasure();


      int noxrCount = 0;
      decimal noxrSum = 0;
      decimal noxrWeighted = 0;
      decimal noxrHit = 0;
      bool noxrCanWeight = true;

      for (int locationDex = 0; locationDex < HourlyRawData.LocationCount; locationDex++)
      {
        if (!HourlyRawData.LocationInfo[locationDex].UnitKey.HasValue) //not unit
        {
          int dataPos = locationDex + rawHourOffset;

          // NOxM
          if (HourlyRawData.NoxmArray[dataPos].HasValue)
          {
            if (noxmCombinedValue.HasValue)
              noxmCombinedValue += HourlyRawData.NoxmArray[dataPos].Value;
            else
              noxmCombinedValue = HourlyRawData.NoxmArray[dataPos].Value;

            noxmMeasureObject.CombineAndSet(HourlyRawData.NoxmMeasureArray[dataPos], out noxmCombinedMeasure);

            hasValues = true;
          }
          else if (HourlyRawData.HitArray[dataPos].HasValue &&
                   HourlyRawData.NoxrArray[dataPos].HasValue)
          {
            decimal noxmCalc = Math.Round(HourlyRawData.HitArray[dataPos].Value * HourlyRawData.NoxrArray[dataPos].Value,
                                          cDecimalPlaces.Noxm,
                                          MidpointRounding.AwayFromZero);

            if (noxmCombinedValue.HasValue)
              noxmCombinedValue += noxmCalc;
            else
              noxmCombinedValue = noxmCalc;

            noxmMeasureObject.CombineAndSet("CALC", out noxmCombinedMeasure);

            hasValues = true;
          }

          // NOxR
          if (HourlyRawData.NoxrArray[dataPos].HasValue)
          {
            noxrCount += 1;
            noxrSum += HourlyRawData.NoxrArray[dataPos].Value;

            if (noxrCanWeight)
            {
              if (HourlyRawData.HitArray[dataPos].HasValue)
              {
                noxrWeighted += Math.Round(HourlyRawData.HitArray[dataPos].Value * HourlyRawData.NoxrArray[dataPos].Value,
                                           cDecimalPlaces.Noxm,
                                           MidpointRounding.AwayFromZero);
                noxrHit += HourlyRawData.HitArray[dataPos].Value;
              }
              else
              {
                noxrCanWeight = false;
              }
            }

            noxrMeasureObject.CombineAndSet(HourlyRawData.NoxrMeasureArray[dataPos], out noxrCombinedMeasure);

            hasValues = true;
          }

          /* Combine Hg, HCl and HF mass values and measure codes */
          cCombineMeasure hgmMeasureObject = new cCombineMeasure();
          cCombineMeasure hclmMeasureObject = new cCombineMeasure();
          cCombineMeasure hfmMeasureObject = new cCombineMeasure();

          CombineMatsValue(ref hgmCombinedValue, ref hgmMeasureObject, ref hasValues, dataPos, HourlyRawData.HgMassArray, HourlyRawData.HgMeasureArray);
          CombineMatsValue(ref hclmCombinedValue, ref hclmMeasureObject, ref hasValues, dataPos, HourlyRawData.HclMassArray, HourlyRawData.HclMeasureArray);
          CombineMatsValue(ref hfmCombinedValue, ref hfmMeasureObject, ref hasValues, dataPos, HourlyRawData.HfMassArray, HourlyRawData.HfMeasureArray);

          hgmCombinedMeasure = hgmMeasureObject.GetCombined();
          hclmCombinedMeasure = hclmMeasureObject.GetCombined();
          hfmCombinedMeasure = hfmMeasureObject.GetCombined();
        }
      }

      // Finish NOxR
      {
        switch (noxrCount)
        {
          case 0:
            {
              noxrCombinedValue = null;
            }
            break;

          case 1:
            {
              noxrCombinedValue = noxrSum;
            }
            break;

          default:
            {
              if (noxrCanWeight)
              {
                noxrCombinedValue = Math.Round(noxrWeighted / noxrHit,
                                        cDecimalPlaces.Noxr,
                                        MidpointRounding.AwayFromZero);
              }
              else
              {
                noxrCombinedValue = Math.Round(noxrSum / noxrCount,
                                        cDecimalPlaces.Noxr,
                                        MidpointRounding.AwayFromZero);
              }
            }
            break;
        }
      }
    }

    #endregion

  }

}
