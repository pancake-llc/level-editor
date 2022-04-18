using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pancake.Common;
using UnityEditor;
using UnityEngine;

namespace Pancake.Editor
{
    public class LevelEditor : EditorWindow
    {
        private const string DEFAULT_LEVEL_SETTING_PATH = "ProjectSettings/LevelEditorSetting.asset";
        private readonly string[] _optionsSpawn = {"Default", "Custom"};

        private Vector3 _prevPosition;
        private Vector2 _pickObjectScrollPosition;
        private PickObject _currentPickObject;
        private List<PickObject> _pickObjects;
        private SerializedObject _pathFolderSerializedObject;

        private SerializedProperty _pathFolderProperty;
        private bool _flagFoldoutPath;
        private GameObject _currentSpawnGameObject;
        private int _selectedSpawn;
        private GameObject _attachSpawnGameObject;

        private static LevelEditorSettings levelEditorSettings;

        private static LevelEditorSettings LevelEditorSettings
        {
            get
            {
                if (levelEditorSettings == null) LoadLevelEditorSetting();

                return levelEditorSettings;
            }
            set
            {
                levelEditorSettings = value;
                SaveLevelEditorSetting();
            }
        }

        private List<PickObject> PickObjects
        {
            get
            {
                if (_pickObjects == null) _pickObjects = new List<PickObject>();
                return _pickObjects;
            }
        }

        public void Init()
        {
            SceneView.duringSceneGui += GridUpdate;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        public static void LoadLevelEditorSetting()
        {
            levelEditorSettings = new LevelEditorSettings();
            if (!DEFAULT_LEVEL_SETTING_PATH.FileExists()) return;
            string json = File.ReadAllText(DEFAULT_LEVEL_SETTING_PATH);
            levelEditorSettings = JsonUtility.FromJson<LevelEditorSettings>(json);
        }

        private static void SaveLevelEditorSetting()
        {
            if (!"ProjectSettings".DirectoryExists()) Directory.CreateDirectory("ProjectSettings");

            try
            {
                File.WriteAllText(DEFAULT_LEVEL_SETTING_PATH, JsonUtility.ToJson(levelEditorSettings, true));
            }
            catch (Exception e)
            {
                Debug.LogError("Unable to save LevelEditorSetting!\n" + e.Message);
            }
        }

        private void OnEnable()
        {
            LoadLevelEditorSetting();
            RefreshPickObject();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= GridUpdate;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void PlayModeStateChanged(PlayModeStateChange obj) { }

        private void OnProjectChange() { TryClose(); }

        private void OnHierarchyChange() { TryClose(); }

        private void GridUpdate(SceneView sceneView) { }

        private bool TryClose() { return false; }

        private void RefreshAll()
        {
            LevelWindow.ClearPreviews();
            RefreshPickObject();
            ClearEditor();
        }

        /// <summary>
        /// display picked object in editor
        /// </summary>
        private void RefreshPickObject()
        {
            _pickObjects = new List<PickObject>();

            foreach (string whitepath in LevelEditorSettings.pickupObjectWhiteList)
            {
                MakeGroupPrefab(whitepath);
            }

            void MakeGroupPrefab(string path)
            {
                if (!Directory.Exists(path) && !File.Exists(path) || !path.StartsWith("Assets"))
                {
                    Debug.LogWarning("[Level Editor]: Can not found folder '" + path + "'");
                    return;
                }

                var levelObjects = new List<GameObject>();
                if (File.Exists(path))
                {
                    levelObjects.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
                else
                {
                    levelObjects = UtilEditor.FindAllAssetsWithPath<GameObject>(path.Replace(Application.dataPath, "").Replace("Assets/", ""))
                        .Where(lo => !(lo is null))
                        .ToList();
                }

                foreach (var obj in levelObjects)
                {
                    string group = path.Split('/').Last();
                    if (File.Exists(path))
                    {
                        var pathInfo = new DirectoryInfo(path);
                        if (pathInfo.Parent != null) group = pathInfo.Parent.Name;
                    }

                    var po = new PickObject {pickedObject = obj.gameObject, group = group};
                    _pickObjects.Add(po);
                }
            }
        }

        private bool CheckEscape()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _currentPickObject = null;
                Repaint();
                SceneView.RepaintAll();
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            Uniform.SpaceTwoLine();
            if (TryClose()) return;
            if (CheckEscape()) return;
            SceneView.RepaintAll();
            InternalDrawDropArea();
            Uniform.SpaceOneLine();
            //Uniform.Button("Refresh all", RefreshAll);
            InternalDrawSetting();
            Uniform.SpaceOneLine();
            InternalDrawPickupArea();
        }

        private void InternalDrawDropArea()
        {
            Uniform.DrawUppercaseSection("LEVEL_EDITOR_DROP_AREA", "DROP AREA", DrawDropArea);

            void DrawDropArea()
            {
                GUILayout.Space(2);
                float width = 0;
                var @event = Event.current;
                Uniform.Horizontal(() =>
                {
                    var whiteArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
                    var blackArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (whiteArea.width == 1f) width = position.width / 2;
                    else width = whiteArea.width;
                    GUI.backgroundColor = new Color(0f, 0.83f, 1f);
                    GUI.Box(whiteArea, "[WHITE LIST]", new GUIStyle(EditorStyles.helpBox) {alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic});
                    GUI.backgroundColor = Color.white;
                    GUI.backgroundColor = new Color(1f, 0.13f, 0f);
                    GUI.Box(blackArea, "[BLACK LIST]", new GUIStyle(EditorStyles.helpBox) {alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic});
                    GUI.backgroundColor = Color.white;
                    switch (@event.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (whiteArea.Contains(@event.mousePosition))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (@event.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();
                                    foreach (string path in DragAndDrop.paths)
                                    {
                                        ValidateCross(path, ref LevelEditorSettings.pickupObjectBlackList);
                                        AddToWhiteList(path);
                                    }

                                    ReduceScopeDirectory(ref LevelEditorSettings.pickupObjectWhiteList);
                                    SaveLevelEditorSetting();
                                }
                            }
                            else if (blackArea.Contains(@event.mousePosition))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (@event.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();
                                    foreach (string path in DragAndDrop.paths)
                                    {
                                        ValidateCross(path, ref LevelEditorSettings.pickupObjectWhiteList);
                                        AddToBlackList(path);
                                    }

                                    ReduceScopeDirectory(ref LevelEditorSettings.pickupObjectBlackList);
                                    SaveLevelEditorSetting();
                                }
                            }

                            SaveLevelEditorSetting();
                            break;
                        case EventType.MouseDown when @event.button == 1:
                            var menu = new GenericMenu();
                            if (whiteArea.Contains(@event.mousePosition))
                            {
                                menu.AddItem(new GUIContent("Clear All [WHITE LIST]"),
                                    false,
                                    () =>
                                    {
                                        LevelEditorSettings.pickupObjectWhiteList.Clear();
                                        SaveLevelEditorSetting();
                                    });
                            }
                            else if (blackArea.Contains(@event.mousePosition))
                            {
                                menu.AddItem(new GUIContent("Clear All [BLACK LIST]"),
                                    false,
                                    () =>
                                    {
                                        LevelEditorSettings.pickupObjectBlackList.Clear();
                                        SaveLevelEditorSetting();
                                    });
                            }

                            menu.ShowAsContext();
                            break;
                    }
                });

                Uniform.Horizontal(() =>
                {
                    Uniform.VerticalScope(() =>
                        {
                            if (LevelEditorSettings.pickupObjectWhiteList.Count == 0)
                            {
                                EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(width - 50), GUILayout.Height(0));
                            }
                            else
                            {
                                foreach (string t in LevelEditorSettings.pickupObjectWhiteList.ToList())
                                {
                                    DrawRow(t, width, _ => LevelEditorSettings.pickupObjectWhiteList.Remove(_));
                                }
                            }
                        },
                        GUILayout.Width(width - 10));
                    Uniform.SpaceOneLine();
                    Uniform.VerticalScope(() =>
                        {
                            if (LevelEditorSettings.pickupObjectBlackList.Count == 0)
                            {
                                EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(width - 50), GUILayout.Height(0));
                            }
                            else
                            {
                                foreach (string t in LevelEditorSettings.pickupObjectBlackList.ToList())
                                {
                                    DrawRow(t, width, _ => LevelEditorSettings.pickupObjectBlackList.Remove(_));
                                }
                            }
                        },
                        GUILayout.Width(width - 15));
                });
            }

            void DrawRow(string content, float width, Action<string> action)
            {
                Uniform.Horizontal(() =>
                {
                    EditorGUILayout.LabelField(new GUIContent(content), GUILayout.Width(width - 50));
                    GUILayout.FlexibleSpace();
                    Uniform.Button(Uniform.IconContent("Toolbar Minus", "Remove"),
                        () =>
                        {
                            action?.Invoke(content);
                            SaveLevelEditorSetting();
                        });
                });
            }

            void AddToWhiteList(string path)
            {
                if (IsCanAddToCollection(path, LevelEditorSettings.pickupObjectWhiteList)) LevelEditorSettings.pickupObjectWhiteList.Add(path);
                LevelEditorSettings.pickupObjectWhiteList = LevelEditorSettings.pickupObjectWhiteList.Distinct().ToList(); //unique
            }

            void AddToBlackList(string path)
            {
                if (IsCanAddToCollection(path, LevelEditorSettings.pickupObjectBlackList)) LevelEditorSettings.pickupObjectBlackList.Add(path);
                LevelEditorSettings.pickupObjectBlackList = LevelEditorSettings.pickupObjectBlackList.Distinct().ToList(); //unique
            }

            bool IsCanAddToCollection(string path, List<string> source)
            {
                if (File.Exists(path) && !Path.GetExtension(path).Equals(".prefab")) return false;
                if (source.Count == 0) return true;
                var info = new DirectoryInfo(path);
                var allParent = new List<DirectoryInfo>();
                GetAllParentDirectories(info, ref allParent);

                string dataPath = Application.dataPath.Replace('/', '\\');
                foreach (var p in allParent)
                {
                    if (!EqualPath(p, dataPath, source)) return false;
                }

                return true;
            }

            bool EqualPath(FileSystemInfo p, string dataPath, List<string> source)
            {
                string relativePath = p.FullName;
                if (relativePath.StartsWith(dataPath)) relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                relativePath = relativePath.Replace('\\', '/');

                foreach (string s in source)
                {
                    if (s.Equals(relativePath)) return false;
                }

                return true;
            }

            void ReduceScopeDirectory(ref List<string> source)
            {
                var arr = new string[source.Count];
                source.CopyTo(arr);
                var valueRemove = new List<string>();
                var unique = arr.Distinct().ToList();
                string dataPath = Application.dataPath.Replace('/', '\\');
                foreach (string u in unique)
                {
                    var info = new DirectoryInfo(u);
                    var allParent = new List<DirectoryInfo>();
                    GetAllParentDirectories(info, ref allParent);
                    allParent.Remove(info);
                    foreach (var p in allParent)
                    {
                        if (EqualPath(p, dataPath, unique)) continue;

                        valueRemove.Add(u);
                        break;
                    }
                }

                foreach (string i in valueRemove)
                {
                    unique.Remove(i);
                }

                source = unique;
            }

            void ValidateCross(string path, ref List<string> target)
            {
                foreach (string t in target.ToList())
                {
                    if (path.Equals(t)) target.Remove(t);
                }
            }

            void GetAllParentDirectories(DirectoryInfo directoryToScan, ref List<DirectoryInfo> directories)
            {
                while (true)
                {
                    if (directoryToScan == null || directoryToScan.Name == directoryToScan.Root.Name || !directoryToScan.FullName.Contains("Assets")) return;

                    directories.Add(directoryToScan);
                    directoryToScan = directoryToScan.Parent;
                }
            }
        }

        private void InternalDrawSetting()
        {
            Uniform.DrawUppercaseSection("LEVEL_EDITOR_CONFIG", "SETTING", DrawSetting);

            void DrawSetting()
            {
                _selectedSpawn = EditorGUILayout.Popup("Where Spawn", _selectedSpawn, _optionsSpawn);
                if (EditorGUI.EndChangeCheck())
                {
                    switch (_optionsSpawn[_selectedSpawn])
                    {
                        case "Default":
                            break;
                        case "Custom":
                            Uniform.SpaceOneLine();
                            _attachSpawnGameObject = (GameObject) EditorGUILayout.ObjectField("Spawn in GO here -->", _attachSpawnGameObject, typeof(GameObject), true);
                            break;
                    }
                }
            }
        }

        private void InternalDrawPickupArea()
        {
            Uniform.DrawUppercaseSection("LEVEL_EDITOR_PICKUP_AREA", "PICKUP AREA", DrawPickupArea);

            void DrawPickupArea()
            {
                var tex = LevelWindow.GetPreview(_currentPickObject?.pickedObject);
                if (tex)
                {
                    string pickObjectName = _currentPickObject?.pickedObject.name;
                    Uniform.SpaceOneLine();
                    Uniform.Horizontal(() =>
                    {
                        GUILayout.Space(position.width / 2 - 50);
                        if (GUILayout.Button(tex, GUILayout.Height(80), GUILayout.Width(80))) _currentPickObject = null;
                    });

                    EditorGUILayout.LabelField($"Selected: {pickObjectName}\nPress Icon Again Or Escape Key To Deselect", GUILayout.Height(40));
                    Uniform.HelpBox("Shift + Click To Add", MessageType.Info);
                }
                else
                {
                    Uniform.HelpBox("Select An Object First", MessageType.Info);
                }

                var resultSplitGroupObjects = PickObjects.GroupBy(_ => _.group).Select(_ => _.ToList()).ToList();
                foreach (var splitGroupObject in resultSplitGroupObjects)
                {
                    string nameGroup = splitGroupObject[0].group.ToUpper();
                    Uniform.DrawUppercaseSection($"LEVEL_EDITOR_PICKUP_AREA_CHILD_{nameGroup}", nameGroup, () => DrawInGroup(splitGroupObject));
                }
            }

            void DrawInGroup(IReadOnlyList<PickObject> pickObjectsInGroup)
            {
                var counter = 0;
                CalculateIdealCount(position.width - 50,
                    60,
                    135,
                    5,
                    out int count,
                    out float size);
                count = Mathf.Max(1, count);
                while (counter >= 0 && counter < pickObjectsInGroup.Count)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (var x = 0; x < count; x++)
                    {
                        var pickObj = pickObjectsInGroup[counter];
                        var go = pickObj.pickedObject;
                        var tex = LevelWindow.GetPreview(go);
                        Uniform.Button("",
                            () => _currentPickObject = _currentPickObject == pickObj ? null : pickObj,
                            null,
                            GUILayout.Width(size),
                            GUILayout.Height(size));

                        var rect = GUILayoutUtility.GetLastRect().Grown(-3);
                        if (pickObj == _currentPickObject) EditorGUI.DrawRect(rect, new Color32(86, 221, 255, 242));
                        if (tex) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                        if (go)
                        {
                            if (pickObj == _currentPickObject)
                                EditorGUI.DropShadowLabel(rect, go.name, new GUIStyle(EditorStyles.whiteMiniLabel) {alignment = TextAnchor.LowerCenter});
                            else
                                EditorGUI.LabelField(rect, go.name, new GUIStyle(EditorStyles.whiteMiniLabel) {alignment = TextAnchor.LowerCenter});
                        }

                        counter++;
                        if (counter >= pickObjectsInGroup.Count) break;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (TryClose()) return;
            if (CheckEscape()) return;

            TryFakeRender(sceneView);
        }

        private void TryFakeRender(SceneView sceneView)
        {
            if (!Event.current.shift) return;
            if (_currentPickObject == null || !_currentPickObject.pickedObject) return;

            bool state;
            Vector3 mousePosition;

            if (sceneView.in2DMode)
            {
                state = UtilEditor.Get2DMouseScenePosition(out var mousePosition2d);
                mousePosition = mousePosition2d;
            }
            else
            {
                state = UtilEditor.GetMousePosition(out mousePosition, sceneView);
            }

            if (state)
            {
                UtilEditor.FakeRenderSprite(_currentPickObject.pickedObject, mousePosition, Vector3.one, Quaternion.identity);
                SceneView.RepaintAll();

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    AddPickObject(_currentPickObject, mousePosition);
                    UtilEditor.SkipEvent();
                }
            }
        }

        /// <summary>
        /// Spawn pickup object
        /// </summary>
        /// <param name="pickObject"></param>
        /// <param name="worldPos"></param>
        private void AddPickObject(PickObject pickObject, Vector3 worldPos)
        {
            if (pickObject?.pickedObject)
            {
                Transform parent;

#if UNITY_2021_1_OR_NEWER
                UnityEditor.SceneManagement.PrefabStage currentPrefabState;
                currentPrefabState = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
                UnityEditor.Experimental.SceneManagement.PrefabStage currentPrefabState;
                currentPrefabState = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif

                if (currentPrefabState != null)
                {
                    var prefabRoot = currentPrefabState.prefabContentsRoot.transform;
                    if (_optionsSpawn[_selectedSpawn] == "Default")
                    {
                        parent = prefabRoot;
                    }
                    else
                    {
                        parent = _attachSpawnGameObject ? _attachSpawnGameObject.transform : prefabRoot;
                    }
                }
                else
                {
                    if (_optionsSpawn[_selectedSpawn] == "Default")
                    {
                        parent = null;
                    }
                    else
                    {
                        parent = _attachSpawnGameObject ? _attachSpawnGameObject.transform : null;
                    }
                }

                var inst = pickObject.pickedObject.Instantiate(parent);
                inst.transform.position = worldPos;

                // if (inst.CalculateBounds(out var bounds,
                //         Space.World,
                //         true,
                //         false,
                //         false,
                //         false))
                // {
                //     var difference = worldPos.y - bounds.min.y;
                //
                //     inst.transform.position += difference * Vector3.up;
                // }
                //
                // inst.transform.position = inst.transform.position.Change(y: 0);

                Undo.RegisterCreatedObjectUndo(inst.gameObject, "Create pick obj");
                Selection.activeObject = inst;
            }
        }

        /// <summary>
        /// Calculate count item pickup can display
        /// </summary>
        /// <param name="availableSpace"></param>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
        /// <param name="defaultCount"></param>
        /// <param name="count"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool CalculateIdealCount(float availableSpace, float minSize, float maxSize, int defaultCount, out int count, out float size)
        {
            int minCount = Mathf.FloorToInt(availableSpace / maxSize);
            int maxCount = Mathf.FloorToInt(availableSpace / minSize);
            bool goodness = defaultCount >= minCount && defaultCount <= maxCount;
            count = Mathf.Clamp(defaultCount, minCount, maxCount);
            size = (availableSpace - (count - 1) * (count / 10f)) / count;
            return goodness;
        }

        private void ClearEditor() { Repaint(); }
    }
}