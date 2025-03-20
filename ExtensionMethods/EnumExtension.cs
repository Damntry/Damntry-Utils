using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Damntry.Utils.ExtensionMethods {
	public static class EnumExtension {

		//TODO Global 4 - Add an optional cache (interning) functionality.
		//Taken from https://medium.com/engineered-publicis-sapient/human-friendly-enums-in-c-9a6c2291111
		public static string GetDescription(this Enum enumValue) {
			var field = enumValue.GetType().GetField(enumValue.ToString());
			if (field == null) {
				return enumValue.ToString();
			}

			var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
				return attribute.Description;
			}

			return enumValue.ToString();
		}

		public static long ToLong<TEnum>(this TEnum enumValue)
				where TEnum : struct, Enum {

			return EnumLambdaCompiled<TEnum>.EnumToLongFunc(enumValue);
		}

		public static long EnumToLong<T>(T enumValue) {
			if (typeof(T).IsEnum) {
				return EnumLambdaCompiled<T>.EnumToLongFunc(enumValue);
			} else {
				throw new Exception("The type is not an enum.");
			}
		}

		//Not "this" parameter to avoid adding another member to Long.
		public static TEnum LongToEnum<TEnum>(long value)
				where TEnum : Enum {

			return EnumLambdaCompiled<TEnum>.LongToEnumFunc(value);
		}

		public static T LongToEnumUnconstrained<T>(long value) {

			if (typeof(T).IsEnum) {
				return EnumLambdaCompiled<T>.LongToEnumFunc(value);
			} else {
				throw new Exception("The type is not an enum.");
			}
		}


		//Taken from https://stackoverflow.com/a/72838343/739345
		private static class EnumLambdaCompiled<TEnum> {
			public static Func<TEnum, long> EnumToLongFunc = GenerateEnumToLongFunc();
			public static Func<long, TEnum> LongToEnumFunc = GenerateLongToEnumFunc();

			private static Func<TEnum, long> GenerateEnumToLongFunc() {
				var inputParameter = Expression.Parameter(typeof(TEnum));
				var body = Expression.Convert(inputParameter, typeof(long)); // means: (long)input;
				var lambda = Expression.Lambda<Func<TEnum, long>>(body, inputParameter);
				return lambda.Compile();
			}

			private static Func<long, TEnum> GenerateLongToEnumFunc() {
				var inputParameter = Expression.Parameter(typeof(long));
				var body = Expression.Convert(inputParameter, typeof(TEnum)); // means: (TEnum)input;
				var lambda = Expression.Lambda<Func<long, TEnum>>(body, inputParameter);
				return lambda.Compile();
			}
		}

	}
}
