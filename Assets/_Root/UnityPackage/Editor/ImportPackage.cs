#if UNITY_EDITOR
using System.IO;
using UnityEditor;


public static class ImportPackage
{
#if !SNORLAX_LEVEL_EDITOR
    private const string LEVEL_EDITOR_PACKAGE_PATH = "Assets/_Root/UnityPackage/level-editor.unitypackage";
    private const string PATH_INSTALL = "Assets/Plugins/Demigiant/DOTween";
    private const string PACKAGE_PATH = "Packages/com.snorlax.level-editor/UnityPackage/level-editor.unitypackage";

    [MenuItem("Package/Import LevelEditor")]
    [InitializeOnLoadMethod]
    public static void ImportDoTween()
    {
        if (Directory.Exists(PATH_INSTALL)) return;
        string path = LEVEL_EDITOR_PACKAGE_PATH;
        if (!File.Exists(path)) path = !File.Exists(Path.GetFullPath(PACKAGE_PATH)) ? LEVEL_EDITOR_PACKAGE_PATH : PACKAGE_PATH;
        AssetDatabase.ImportPackage(path, true);
    }
#endif
}

#endif