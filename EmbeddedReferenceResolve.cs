using System;
using System.IO;
using System.Reflection;

namespace Damntry.Utils {
    public static class EmbeddedReferenceResolve {


        public static Assembly LoadEmbeddedResource(byte[] resourceBytes) {
            if (resourceBytes == null || resourceBytes.Length == 0) {
                throw new ArgumentException($"Argument {nameof(resourceBytes)} cant be null or empty");
            }

            Assembly loadedAssembly = Assembly.Load(resourceBytes);
            if (loadedAssembly == null) {
                throw new InvalidOperationException($"A dll resource could not be loaded.");
            }

            return loadedAssembly;
        }


        public static void LoadAssemblyFromResources(params string[] resourceNames) {
            if (resourceNames == null || resourceNames.Length == 0) {
                throw new ArgumentException($"Argument {nameof(resourceNames)} cant be null or empty");
            }

            foreach (string resName in resourceNames) {
                Assembly asm = ResolveFromResourceName(Assembly.GetCallingAssembly(), resName);
                if (asm == null) {
                    throw new InvalidOperationException($"The dll resource '{resName}' could not be loaded. " +
                        $"Make sure it is added to the resources of the assembly you are calling from.");
                }
            }
        }

        private static Assembly ResolveFromResourceName(Assembly assembly, string resourceName) {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                byte[] resBytes = new byte[(int)stream.Length];
                stream.Read(resBytes, 0, (int)stream.Length);

                return Assembly.Load(resBytes);
            }
        }
    }
}
