using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using System.Text;
using Damntry.Utils.Logging;

namespace Damntry.Utils.Reflection {

	public static class ReflectionHelper {

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
			MethodInfo methodInfo = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

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
				TimeLogger.Logger.LogTimeExceptionWithMessage($"Parameter count error while calling method {methodInfo.Name}", e, Logging.TimeLogger.LogCategories.Reflect);
				throw;
			}
		}

		/// <summary>Gets all property and values (.ToString()) of an object through reflection.</summary>
		/// <param name="target">The object from which we ll get all properties</param>
		/// <returns>A string where each line is a property and its values.</returns>
		public static string ListPropertyAndValues(object target) {
			var properties =
				from property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				select new {
					property.Name,
					Value = property.GetValue(target, null)
				};

			var builder = new StringBuilder(12 * properties.Count());

			foreach (var property in properties) {
				builder
					.Append(property.Name)
					.Append(" = ")
					.Append(property.Value)
					.AppendLine();
			}

			return builder.ToString();
		}

	}

}

