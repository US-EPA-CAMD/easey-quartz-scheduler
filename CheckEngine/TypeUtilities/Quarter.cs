using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ECMPS.Checks.TypeUtilities
{
    
	/// <summary>
	/// 
	/// </summary>
	public struct Quarter : IComparable, IComparable<Quarter>, IEquatable<Quarter>
	{

		#region Private Constructors

		/// <summary>
		/// Quarter object constructor.
		/// </summary>
		/// <param name="quarterKey"></param>
		/// <exception cref="System.ArgumentException">Thrown when a Quarter object was previously created for the quarterKey.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown when quarterKey represents a quarter before year 1 AD, quarter 1.</exception>
		private Quarter(int quarterKey)
		{
			if (QuarterDictionary.ContainsKey(quarterKey))
				throw new ArgumentException( $"A Quarter object was previously created for quarterKey {quarterKey}.", "quarterKey");
			if (quarterKey < 5)
				throw new ArgumentOutOfRangeException("quarterKey", quarterKey, "quarterKey must be greater than or equal to 5 to represent the minimum of year 1 AD, quarter 1.");

			_BeginHour = null;
			_EndHour = null;
			
			YearValue = GetYearValue(quarterKey);
			QuarterValue = GetQuarterValue(quarterKey);

			QuarterDictionary.Add(quarterKey, this);
		}

		#endregion


		#region Public Static Methods: Get Quarter Object

		/// <summary>
		/// Returns the Quarter object for the passed date, creating it if it does not already exist.
		/// </summary>
		/// <param name="quarterKey"></param>
		/// <returns>The quarter object for the quarterKey.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown when quarterKey represents a quarter before year 1 AD, quarter 1.</exception>
		public static Quarter FetchQuarter(int quarterKey)
		{
			if (quarterKey < 5)
			{
				throw new ArgumentOutOfRangeException("quarterKey", quarterKey, "quarterKey must be greater than or equal to 5 to represent the minimum of year 1 AD, quarter 1.");
			}

			if (!QuarterDictionary.ContainsKey(quarterKey))
			{
				new Quarter(quarterKey); // Automatically added to QuarterDictionary
			}

			return QuarterDictionary[quarterKey];
		}


		/// <summary>
		/// Returns the Quarter object for the passed date, creating it if it does not already exist.
		/// </summary>
		/// <param name="yearValue">The year value for the quarter.</param>
		/// <param name="quarterValue">The quarter value for the quarter.</param>
		/// <returns>The quarter object in which the year and quarter values falls.</returns>
		public static Quarter FetchQuarter(int yearValue, int quarterValue)
		{
			int quarterKey = GetQuarterKey(yearValue, quarterValue);

			return FetchQuarter(quarterKey);
		}


		/// <summary>
		/// Returns the Quarter object for the passed date, creating it if it does not already exist.
		/// </summary>
		/// <param name="dateTime">The DateTime object for which the corrsponding Quarter object should be returned.</param>
		/// <returns>The quarter object in which the DateTime falls.</returns>
		public static Quarter FetchQuarter(DateTime dateTime)
		{
			int quarterKey = GetQuarterKey(dateTime);

			return FetchQuarter(quarterKey);
		}


		/// <summary>
		/// Returns the Quarter object for the passed date, creating it if it does not already exist.
		/// </summary>
		/// <param name="quarterText">The quarter code or description with the year and then quarter separated by 'Q' or 'q', with spaces allowed.</param>
		/// <returns>The quarter object that the quarter code or description represents.</returns>
		/// <exception cref="System.ArgumentException">Thrown when quarterText does not match the year and quarter deliminted by a 'Q' or 'q' format.</exception>
		public static Quarter FetchQuarter(string quarterText)
		{
			int? quarterKey = GetQuarterKey(quarterText);

			return FetchQuarter(quarterKey.Value);
		}

        #endregion


        #region Private Statuc Fields

        private static Dictionary<int, Quarter> QuarterDictionary = new Dictionary<int, Quarter>();

		#endregion


		#region Public Properties

		/// <summary>
		/// The Begin Date for the quarter.
		/// </summary>
		public DateTime BeginDate { get { return BeginHour.Date; } }

		/// <summary>
		/// The Begin Hour for the quarter.
		/// </summary>
		public DateTime BeginHour 
		{ 
			get 
			{ 
				if (_BeginHour == null)
                {
					_BeginHour = new DateTime(YearValue, 3 * (QuarterValue - 1) + 1, 1);
				}

				return _BeginHour.Value; 
			}
		}

		/// <summary>
		/// The End Date for the quarter.
		/// </summary>
		public DateTime EndDate { get { return EndHour.Date; } }

		/// <summary>
		/// The End Hour for the quarter.
		/// </summary>
		public DateTime EndHour
		{
			get
			{
				if (_EndHour == null)
				{
					_EndHour = BeginDate.AddMonths(3).AddHours(-1);
				}

				return _EndHour.Value;
			}
		}

		/// <summary>
		/// The integer quarter value for the quarter.
		/// </summary>
		public int QuarterKey { get { return GetQuarterKey(YearValue, QuarterValue); } }

		/// <summary>
		/// The integer quarter value for the quarter.
		/// </summary>
		public int QuarterValue { get; private set; }

		/// <summary>
		/// The integer year value for the quarter.
		/// </summary>
		public int YearValue { get; private set; }


		#region Helper Fields

		private DateTime? _BeginHour;
		private DateTime? _EndHour;

		#endregion

		#endregion


		#region Public Methods

		/// <summary>
		/// Adds the specified number of quarters to the existing quarter and returns a new Quarter object.
		/// </summary>
		/// <param name="value">The number of quarters to add and can be negative.</param>
		/// <returns></returns>
		public Quarter AddQuarters(int value)
        {
			if (QuarterKey + value < 5)
				throw new ArgumentOutOfRangeException("value", "Value would result in a quarter that is not anno Domini (AD).");

			return FetchQuarter(QuarterKey + value);
		}

		#endregion


		#region Public Static General Methods

		/// <summary>
		/// Returns an integer that indicates the order relationship between the first and second Quarter. 
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns 1 if the first is greater, -1 if the second is greater, and 0 if they are the same.</returns>
		public static int Compare(Quarter one, Quarter two)
        {
			if (one.QuarterKey > two.QuarterKey) return 1;
			if (one.QuarterKey < two.QuarterKey) return -1;
			return 0;
		}


		/// <summary>
		/// Compares two Quarter values for equality, return true if the values are equal and false if they are not.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns></returns>
		public static bool Equal(Quarter one, Quarter two)
        {
			return (one.QuarterKey == two.QuarterKey);
        }


		/// <summary>
		/// Returns the quarter of the given date.
		/// </summary>
		/// <param name="dateTime">The date to inspect.</param>
		/// <returns>The quarter of the date.</returns>
		public static int GetQuarterValue(DateTime dateTime)
		{
			int result = ((dateTime.Month - 1) / 3) + 1;

			return result;
		}


		/// <summary>
		/// Returns the Quarter Value for the quarterkey.
		/// </summary>
		/// <param name="quarterKey"></param>
		/// <returns></returns>
		public static int GetQuarterValue(int quarterKey)
        {
			int result;

			result = ((quarterKey - 1) % 4) + 1;

			return result;
		}


		/// <summary>
		/// Returns the 'quarter key' for a year and quarter using the formula [ordinal = 4 * year + quarter].
		/// </summary>
		/// <param name="yearValue">The year value for the quarter.</param>
		/// <param name="quarterValue">The quarter value for the quarter.</param>
		/// <returns></returns>
		public static int GetQuarterKey(int yearValue, int quarterValue)
		{
			int result;

			if ((yearValue >= 1) && (quarterValue >= 1) && (quarterValue <= 4))
			{
				result = 4 * yearValue + quarterValue;
			}
			else
			{
				throw new ArgumentException($"'{yearValue} Q{quarterValue}' is not a valid Quarter. A Quarter must have a year greater than or equal to 1, and a quarter of 1, 2, 3 or 4.");
			}

			return result;
		}


		/// <summary>
		/// Returns the 'quarter key' for a date.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static int GetQuarterKey(DateTime dateTime)
		{
			int result = GetQuarterKey(dateTime.Year, GetQuarterValue(dateTime));

			return result;
		}


		/// <summary>
		/// Returns the 'quarter ordinal' for a quarter code or description that contains a 'Q' or 'q' between the year and quarter.
		/// </summary>
		/// <param name="quarterText"></param>
		/// <returns></returns>
		public static int GetQuarterKey(string quarterText)
		{
			int result;

			try
			{
				string[] quarterParts;
				int quarterValue, yearValue;

				if ((quarterText != null) && ((quarterParts = quarterText.Split('Q', 'q')).Length == 2)
					&& int.TryParse(quarterParts[0].Trim(), out yearValue)
					&& int.TryParse(quarterParts[1].Trim(), out quarterValue)
					&& (yearValue >= 1) && (quarterValue >= 1) && (quarterValue <= 4))
				{
					result = 4 * yearValue + quarterValue;
				}
				else
				{
					throw new ArgumentException($"'{quarterText}' is not a Text Quarter. A Text Quarter must contain a year greater than or equal to 1, followed by 'Q' or 'q', and ended by a quarter of 1, 2, 3 or 4.  Spaces can occur before and after the year and quarter.");
				}
			}
			catch
			{
				throw new ArgumentException($"'{quarterText}' is not a Text Quarter. A Text Quarter must contain a year greater than or equal to 1, followed by 'Q' or 'q', and ended by a quarter of 1, 2, 3 or 4.  Spaces can occur before and after the year and quarter.");
			}

			return result;
		}


		/// <summary>
		/// Returns the Year Value for the quarterkey.
		/// </summary>
		/// <param name="quarterKey"></param>
		/// <returns></returns>
		public static int GetYearValue(int quarterKey)
		{
			int result;

			result = (quarterKey - 1) / 4;

			return result;
		}


		/// <summary>
		/// Returns the latest Quarter in the provided list of quarters.
		/// </summary>
		/// <param name="quarterList">The list of quarters to evaluate.</param>
		/// <returns>The lates quarter in the list of quarters.</returns>
		public static Quarter Max(params Quarter[] quarterList)
        {
			Quarter result = quarterList[0];

			foreach (Quarter item in quarterList)
            {
				if (item > result)
					result = item;
            }

			return result;
        }


		/// <summary>
		/// Returns the earliest Quarter in the provided list of quarters.
		/// </summary>
		/// <param name="quarterList">The list of quarters to evaluate.</param>
		/// <returns>The lates quarter in the list of quarters.</returns>
		public static Quarter Min(params Quarter[] quarterList)
		{
			Quarter result = quarterList[0];

			foreach (Quarter item in quarterList)
			{
				if (item < result)
					result = item;
			}

			return result;
		}

		#endregion


		#region Compare, Equality, Hash and ToSting implementations and overrides

		/// <summary>
		/// Compares this Quarter to the given object. This method provides an implementation of the IComparable interface. The object
		/// argument must be another Quarter, or otherwise an exception occurs.  Null is considered less than any instance.
		/// </summary>
		/// <param name="that">The Quarter value to compare as an object.</param>
		/// <returns></returns>
		public int CompareTo(Object that)
		{
			if (that == null) return 1;

			if (!(that is Quarter))
			{
				throw new ArgumentException("Argument must be a Quarter object.", "value");
			}

			return Quarter.Compare(this, (Quarter)that);
		}


		/// <summary>
		/// Compares this Quarter to the given Quarter.
		/// </summary>
		/// <param name="that"></param>
		/// <returns></returns>
		public int CompareTo(Quarter that)
		{
			return Quarter.Compare(this, that);
		}


		/// <summary>
		/// Base object Equals override that checks for comparison to null, the same object, different types,
		/// and finally uses the type specific Equals.
		/// </summary>
		/// <param name="that">The object instance to compare against this instance.</param>
		/// <returns>Returns true of the objects are of the same type and have the same key contents.</returns>
		public override bool Equals(object that)
		{
			if (that is Quarter)
            {
				return Equal(this, (Quarter)that);
			}

			return false;
		}


		/// <summary>
		/// Type specific Equals implementation that uses YearValue and QuarterValue.
		/// </summary>
		/// <param name="that">The Quarter instance to compare against this instance.</param>
		/// <returns>Returns true if the YearValue and QuarterValue values are the same for the two instances.</returns>
		public bool Equals(Quarter that)
		{
			return Equal(this, that);
		}


		/// <summary>
		/// GetHashCode override that uses RptPeriodId, HourlyType, ParameterCd and MonSysId.
		/// </summary>
		/// <returns>Returns the hash based on RptPeriodId, HourlyType, ParameterCd and MonSysId.</returns>
		public override int GetHashCode()
		{

			return YearValue.GetHashCode() ^ QuarterValue.GetHashCode();
		}


		/// <summary>
		/// Returns the Hourly Type, Parameter, MON_SYS_ID, RPT_PERIOD_ID and Value.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{YearValue} Q{QuarterValue}";
		}

        #endregion


        #region Public Operators

		/// <summary>
		/// Creates a new Quarter instance that is the specified number of quarters after the
		/// the provided quarter if the addition is positive, otherwise it is before the quarter.
		/// </summary>
		/// <param name="quarter">The starting quarter.</param>
		/// <param name="addition">The number of quarters to shift, into the future for positive and the past for negative.</param>
		/// <returns>The Quarter object representing the new quarter.</returns>
		public static Quarter operator + (Quarter quarter, int addition)
        {
			return quarter.AddQuarters(addition);
		}


		/// <summary>
		/// Creates a new Quarter instance that is the specified number of quarters before the
		/// the provided quarter if the addition is positive, otherwise it is before the quarter.
		/// </summary>
		/// <param name="quarter">The starting quarter.</param>
		/// <param name="subtraction">The number of quarters to shift, into the past for positive and the future for negative.</param>
		/// <returns>The Quarter object representing the new quarter.</returns>
		public static Quarter operator - (Quarter quarter, int subtraction)
		{
			return quarter.AddQuarters(-subtraction);
		}


		/// <summary>
		/// Creates a new Quarter instance that is one quarter after the
		/// the provided quarter if the addition is positive, otherwise it is before the quarter.
		/// </summary>
		/// <param name="quarter">The starting quarter.</param>
		/// <returns>The Quarter object representing the new quarter.</returns>
		public static Quarter operator ++ (Quarter quarter)
		{
			return quarter.AddQuarters(1);
		}


		/// <summary>
		/// Creates a new Quarter instance that is one quarter before the
		/// the provided quarter if the addition is positive, otherwise it is before the quarter.
		/// </summary>
		/// <param name="quarter">The starting quarter.</param>
		/// <returns>The Quarter object representing the new quarter.</returns>
		public static Quarter operator -- (Quarter quarter)
		{
			return quarter.AddQuarters(-1);
		}


		/// <summary>
		/// Implements the equals operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if two Quarters are equal.</returns>
		public static bool operator == (Quarter one, Quarter two)
        {
			return Equal(one, two);
        }


		/// <summary>
		/// Implements the not-equal operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if two Quarters are not equal.</returns>
		public static bool operator != (Quarter one, Quarter two)
		{
			return !Equal(one, two);
		}


		/// <summary>
		/// Implements the less than operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if the first quarter is less than the second quarter.</returns>
		public static bool operator < (Quarter one, Quarter two)
		{
			return (Compare(one, two) < 0);
		}

		/// <summary>
		/// Implements the less than or equel to operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if the first quarter is less than or equel to the second quarter.</returns>
		public static bool operator <= (Quarter one, Quarter two)
		{
			return (Compare(one, two) <= 0);
		}


		/// <summary>
		/// Implements the greater than operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if the first quarter is greater than the second quarter.</returns>
		public static bool operator > (Quarter one, Quarter two)
		{
			return (Compare(one, two) > 0);
		}

		/// <summary>
		/// Implements the greater than or equel to operator for the Quarter class.
		/// </summary>
		/// <param name="one">The first Quarter to compare.</param>
		/// <param name="two">The second Quarter to compare.</param>
		/// <returns>Returns true if the first quarter is greater than or equel to the second quarter.</returns>
		public static bool operator >= (Quarter one, Quarter two)
		{
			return (Compare(one, two) >= 0);
		}

		#endregion

	}

}
