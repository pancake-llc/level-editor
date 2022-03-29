namespace Snorlax.LevelEditor
{
    using System.IO;
    using UnityEditor;
    using System;
    using System.Reflection;
    using UnityEngine;

    [InitializeOnLoad]
    public static class ImportPackage
    {
#if !SNORLAX_LEVEL_EDITOR
        private const string LEVEL_EDITOR_PACKAGE_PATH = "Assets/_Root/UnityPackage/level-editor.unitypackage";
        private const string PACKAGE_PATH = "Packages/com.snorlax.level-editor/level-editor.unitypackage";

        [MenuItem("Package/Import LevelEditor")]
        [InitializeOnLoadMethod]
        public static void Import() { ImportImpl(true); }

        private static void ImportImpl(bool interactive)
        {
            EditorPrefs.SetBool(Application.identifier + ".leveleditor", true);
            string path = LEVEL_EDITOR_PACKAGE_PATH;
            if (!File.Exists(path)) path = !File.Exists(Path.GetFullPath(PACKAGE_PATH)) ? LEVEL_EDITOR_PACKAGE_PATH : PACKAGE_PATH;
            AssetDatabase.ImportPackage(path, interactive);
        }

        static ImportPackage() { EditorApplication.update += AutoImported; }

        public static void AutoImported()
        {
            EditorApplication.update -= AutoImported;

            if (!IsImported()) ImportImpl(false);
        }

        public static bool IsImported()
        {
            var imported = EditorPrefs.GetBool(Application.identifier + ".leveleditor", false);
            var locale = FindClass("LevelEditor", "Snorlax.Editor");

            return locale != null || imported;
        }

        /// <summary>
        /// Returns the first class found with the specified class name and (optional) namespace and assembly name.
        /// Returns null if no class found.
        /// </summary>
        /// <returns>The class.</returns>
        /// <param name="className">Class name.</param>
        /// <param name="nameSpace">Optional namespace of the class to find.</param>
        /// <param name="assemblyName">Optional simple name of the assembly.</param>
        public static Type FindClass(string className, string nameSpace = null, string assemblyName = null)
        {
            string typeName = string.IsNullOrEmpty(nameSpace) ? className : nameSpace + "." + className;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly asm in assemblies)
            {
                // The assembly must match the given one if any.
                if (!string.IsNullOrEmpty(assemblyName) && !asm.GetName().Name.Equals(assemblyName))
                {
                    continue;
                }

                try
                {
                    Type t = asm.GetType(typeName);

                    if (t != null && t.IsClass)
                        return t;
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var le in e.LoaderExceptions)
                        Debug.LogException(le);
                }
            }

            return null;
        }

#endif
    }
}