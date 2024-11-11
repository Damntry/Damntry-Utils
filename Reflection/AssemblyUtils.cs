using System;
using System.Linq;
using System.Reflection;

namespace Damntry.Utils.Reflection {

	public static class AssemblyUtils {

		private static Assembly[] assemblyCache;

		/// <summary>
		/// Searches through the loaded assemblies to get a type with the full type name specified.
		/// Avoids the need to reference a dll to use its functionality.
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
		public static Type GetTypeFromAssembly(string fullTypeName, bool refreshCache = true) {
			if (refreshCache || assemblyCache == null) {
				assemblyCache = AppDomain.CurrentDomain.GetAssemblies();
			}

			return assemblyCache.Select(a => a.GetType(fullTypeName, false)).Where(t => t != null).FirstOrDefault();
		}

	}
}
