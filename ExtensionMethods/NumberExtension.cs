using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damntry.Utils.ExtensionMethods {

	public static class NumberExtensionMethods {

		/// <summary>Limits the number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value"></param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>True if it needed to clamp the value. False otherwise.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static bool Clamp(this ref float value, float min, float max) {
			return ClampIComparable(ref value, min, max);
		}

		/// <summary>Limits the number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value"></param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>True if it needed to clamp the value. False otherwise.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static bool Clamp(this ref double value, double min, double max) {
			return ClampIComparable(ref value, min, max);
		}

		/// <summary>Limits the number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value">The value that will be clamped.</param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>True if it needed to clamp the value. False otherwise.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static bool Clamp(this ref int value, int min, int max) {
			return ClampIComparable(ref value, min, max);
		}

		/// <summary>Returns a limited number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value">The value that will be clamped.</param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>The clamped result.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static float ClampReturn(this float value, float min, float max) {
			return ClampIComparable(value, min, max);
		}

		/// <summary>Returns a limited number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value">The value that will be clamped.</param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>The clamped result.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static double ClampReturn(this double value, double min, double max) {
			return ClampIComparable(value, min, max);
		}

		/// <summary>Returns a limited number between a maximum and minimum value passed by parameter, inclusive.</summary>
		/// <param name="value">The value that will be clamped.</param>
		/// <param name="min">Minimum clamping value.</param>
		/// <param name="max">Maximum clamping value.</param>
		/// <returns>The clamped result.</returns>
		/// <exception cref="ArgumentException">When the maximum value is lower than the minimum.</exception>
		public static int ClampReturn(this int value, int min, int max) {
			return ClampIComparable(value, min, max);
		}

		private static T ClampIComparable<T>(T value, T min, T max) where T : IComparable<T> {
			//This method should only be used with non nullable value types. You can declare that in C# 8.0 but not such luck for me.
			if (min.CompareTo(min) > 0) {
				throw new ArgumentException("Clamp: The max value is lower than the min value.");
			}

			if (value.CompareTo(min) < 0) {
				return min;
			}
			if (value.CompareTo(max) > 0) {
				return max;
			}

			return value;
		}

		private static bool ClampIComparable<T>(ref T value, T min, T max) where T : IComparable<T> {
			//This method should only be used with non nullable value types. You can declare that in C# 8.0 but not such luck for me.
			if (min.CompareTo(min) > 0) {
				throw new ArgumentException("Clamp: The max value is lower than the min value.");
			}

			if (value.CompareTo(min) < 0) {
				value = min;
				return false;
			}
			if (value.CompareTo(max) > 0) {
				value = max;
				return false;
			}

			return true;
		}
	}

}
