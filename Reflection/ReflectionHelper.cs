using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Damntry.Utils.Logging;

namespace Damntry.Utils.Reflection {

	public static class ReflectionHelper {

		public static BindingFlags AllBindings { get; } = BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField |
			BindingFlags.GetProperty | BindingFlags.SetProperty;


		/// <summary>Void method call</summary>
		/// <param name="classInstance">Living instance of the class where the method resides. Can be null if the method is static.</param>
		/// <param name="methodName">Method name</param>
		public static void CallMethod(object classInstance, string methodName, object[] args = null) {
			CallMethod<object>(classInstance, methodName, args);
		}

		/// <summary>Function call</summary>
		/// <typeparam name="R">Return type</typeparam>
		/// <param name="classType">Type of the class where the method resides.</param>
		/// <param name="methodName">Method name</param>
		public static R CallStaticMethod<R>(Type classType, string methodName, object[] args = null) {
			return CallMethod<R>(null, classType, methodName, args);
		}

		/// <summary>Function call</summary>
		/// <typeparam name="R">Return type</typeparam>
		/// <param name="classInstance">Living instance of the class where the method resides.</param>
		/// <param name="methodName">Method name</param>
		public static R CallMethod<R>(object classInstance, string methodName, object[] args = null) {
			return CallMethod<R>(classInstance, classInstance.GetType(), methodName, args);
		}

		/// <summary>Function call</summary>
		/// <typeparam name="R">Return type</typeparam>
		/// <param name="classInstance">Living instance of the class where the method resides. Can be null if the method is static.</param>
		/// <param name="classType">Type of the class where the method resides.</param>
		/// <param name="methodName">Method name</param>
		private static R CallMethod<R>(object classInstance, Type classType, string methodName, object[] args = null) {
			MethodInfo methodInfo = classType.GetMethod(methodName, BindingFlags.NonPublic | 
				BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

			return CallMethod<R>(classInstance, methodInfo, args);
		}

		/// <summary>Function call</summary>
		/// <typeparam name="R">Return type</typeparam>
		/// <param name="classInstance">Living instance of the class where the method resides. Can be null if the method is static.</param>
		/// <param name="methodInfo"><see cref="MethodInfo"/> object.</param>
		public static R CallMethod<R>(object classInstance, MethodInfo methodInfo, object[] args = null) {
			try {
				return (R)methodInfo.Invoke(classInstance, args);
			} catch (TargetParameterCountException e) {
				TimeLogger.Logger.LogTimeExceptionWithMessage($"Parameter count error while calling method {methodInfo.Name}", e, Logging.LogCategories.Reflect);
				throw;
			}
		}

		public enum MemberType {
			Property,
			Field,
			Both
		}

		/// <summary>Gets all property and/or field values (.ToString()) of an object through reflection.</summary>
		/// <param name="target">The object from which we ll get all properties</param>
		/// <param name="showPrivate">If private properties will also show.</param>
		/// <returns>A multi line string where each line is a property or field with its value.</returns>
		public static string ListMemberValues(object target, MemberType memberType, bool showPrivate = true) {
			BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;
			if (showPrivate) {
				bindingAttr |= BindingFlags.NonPublic;
			}

			StringBuilder sbMembers = new StringBuilder();
			MemberInfo[] memberList = [];

			if (memberType == MemberType.Both || memberType == MemberType.Property) {
				memberList = target.GetType().GetProperties(bindingAttr);
			}

			if (memberType == MemberType.Both || memberType == MemberType.Field) {
				if (memberList.Length > 0) {
					memberList = memberList.Concat(target.GetType().GetFields(bindingAttr)).ToArray();
				} else {
					memberList = target.GetType().GetFields(bindingAttr);
				}
			}

			Array.ForEach(memberList, minfo => {
				sbMembers.Append(minfo.Name);
				sbMembers.Append(" = ");
				if (minfo is PropertyInfo) {
					sbMembers.Append(((PropertyInfo)minfo).GetValue(target, null));
				} else if (minfo is FieldInfo) {
					sbMembers.Append(((FieldInfo)minfo).GetValue(target));
				}
				sbMembers.AppendLine();
			});
			
			return sbMembers.ToString();
		}

		public static string ConvertFullTypeToNormal(string type) {
			//If it contains a period, it is a full type name. Remove chars until the last period.
			return !string.IsNullOrEmpty(type) && type.Contains(".") ?
				type.Substring(type.LastIndexOf(".") + 1) : type;

		}

		public static string[] ConvertFullTypesToNormal(params string[] types) {
			string[] typesNormal = null;

			if (types?.Any() != true) {
				typesNormal = types.Select(s =>
						ConvertFullTypeToNormal(s)
					).ToArray();
			}

			return typesNormal;
		}

		/// <summary>
		/// Performs the same operation as default(T), when T is not known at compile time.
		/// </summary>
		/// <param name="type">The type from which to get the default value.</param>
		public static object GetDefaultValue(this Type type) {
			return FormatterServices.GetUninitializedObject(type);
		}

	}

}

