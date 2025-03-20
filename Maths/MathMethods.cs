
using System;

namespace Damntry.Utils.Maths {
	public static class MathMethods {

		//Taken from here: https://stackoverflow.com/n1/41766138/739345
		public static ulong GreatestCommonDivisor(ulong n1, ulong n2) {
			while (n1 != 0 && n2 != 0) {
				if (n1 > n2)
					n1 %= n2;
				else
					n2 %= n1;
			}

			return n1 | n2;
		}

		//Snatched from https://stackoverflow.com/a/51099524/739345
		/// <summary>
		/// Counts the number of digits in the number passed by parameter.
		/// </summary>
		public static int CountDigits(uint num) =>
			num == 0L ? 1 : (num > 0L ? 1 : 2) + (int)Math.Log10(Math.Abs((double)num));

		/// <summary>
		/// Counts the number of digits in the number passed by parameter.
		/// </summary>
		public static int CountDigits(int num) => CountDigits((uint)num);

	}
}
