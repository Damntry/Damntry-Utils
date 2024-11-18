using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Damntry.Utils.Reflection {

	public static class AssemblyUtils {

		private static Assembly[] assemblyCache;

		/// <summary>
		/// Searches through all currently loaded assemblies to get a type with the full type name specified.
		/// This avoids the need to reference a dll to access its functionality.
		/// For BepInEx, check Chainloader.PluginInfos.
		/// </summary>
		/// <param name="fullTypeName">
		/// The full name of the type. That is, the complete namespace and its name.
		/// For example: "System.Reflection.Assembly"
		/// Its a bit different if the type is generic. Usually you need to add an extra "`1" string 
		/// to get the generic, and once you have it, you use Type.MakeGenericType to get the typed 
		/// version you need.
		/// If you have access to the type, you can get its full name with: typeof(SomeType).FullName
		/// </param>
		/// <param name="refreshCache">Refreshes the assembly cache.</param>
		/// <returns>The type, or null if not found.</returns>
		public static Type GetTypeFromLoadedAssemblies(string fullTypeName, bool refreshCache = true) {
			if (refreshCache || assemblyCache == null) {
				//TODO Global 5 - Should probably make a timer since last refresh, and the refreshCache would
				//	now be an enum with a 3º option being "Default", which would mean "refresh if more than X
				//	ms since last time"
				assemblyCache = AppDomain.CurrentDomain.GetAssemblies();
			}

			return assemblyCache.Select(a => a.GetType(fullTypeName, false)).Where(t => t != null).FirstOrDefault();
		}


		public static Type[] GetTypesFromLoadedAssemblies(bool refreshCache, params string[] argumentFullTypeNames) {
			if (argumentFullTypeNames == null) {
				return null;
			}

			bool firstRun = true;
			List<Type> argumentTypes = new(argumentFullTypeNames.Length);

			foreach (string argString in argumentFullTypeNames) {
				Type argType = AssemblyUtils.GetTypeFromLoadedAssemblies(argString, firstRun ? refreshCache : false);
				if (argType == null) {
					throw new ArgumentException($"The type with value \"{argString}\" couldnt be found in the assembly.");
				}

				argumentTypes.Add(argType);
				firstRun = false;
			}

			return argumentTypes?.ToArray();
		}

		public static string GetAssemblyDllFilePath(Type assemblyType) {
			return Assembly.GetAssembly(assemblyType).Location;
		}

		public static string GetAssemblyDllFolderPath(Type assemblyType) {
			return Path.GetDirectoryName(GetAssemblyDllFilePath(assemblyType));
		}

	}

}
