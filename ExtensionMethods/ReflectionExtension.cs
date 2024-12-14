using System;
using System.ComponentModel;

namespace Damntry.Utils.ExtensionMethods {

	public static class ReflectionExtension {


		/// <summary>
		/// From http://stackoverflow.com/questions/457676/c-reflection-check-if-a-class-is-derived-from-a-generic-class.
		/// Checks if the type is a subclass of the base type passed through parameter.
		/// Takes into account if the type is an unbound generic, to get its typed version to compare against the base type.
		/// </summary>
		public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType) {
			while (toCheck != typeof(object)) {
				Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (baseType == cur) {
					return true;
				}

				toCheck = toCheck.BaseType;
			}

			return false;
		}

		public static bool HasCustomAttribute<T>(this Type type) where T : Attribute {
			return (T)Attribute.GetCustomAttribute(type, typeof(T)) != null;
		}

		//TODO Global 4 - Add an optional cache option, but then I might want to move this to its own class.

		//Taken from https://medium.com/engineered-publicis-sapient/human-friendly-enums-in-c-9a6c2291111
		public static string GetDescription(this Enum enumValue) {
			var field = enumValue.GetType().GetField(enumValue.ToString());
			if (field == null)
				return enumValue.ToString();

			var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
				return attribute.Description;
			}

			return enumValue.ToString();
		}

	}
}
