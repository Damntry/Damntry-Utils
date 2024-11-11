using System;
using System.CodeDom;
using System.Reflection;

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
			R ret;
			MethodInfo dynMethod = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

			try {
				ret = (R)dynMethod.Invoke(classInstance, args);
			} catch (TargetParameterCountException e) {
				GlobalConfig.Logger.LogTimeExceptionWithMessage($"Parameter count error while calling method {methodName}", e, Logging.TimeLoggerBase.LogCategories.Reflect);
				throw;
			}
			return ret;
		}



	}

}

