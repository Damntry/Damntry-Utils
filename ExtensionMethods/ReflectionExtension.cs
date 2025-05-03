using System;
using System.Linq;
using System.Reflection;

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

		public static bool HasCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute {
			return (T)Attribute.GetCustomAttribute(memberInfo, typeof(T)) != null;
		}


		/// <summary>
		/// From https://stackoverflow.com/a/51441889/739345
		/// </summary>
		public static bool IsStatic(this PropertyInfo source, bool nonPublic = true)
				=> source.GetAccessors(nonPublic).Any(x => x.IsStatic);

	}
}
