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
        private readonly string[] _optionsSpawn = { "Default", "Custom" };

        private Vector2 _pickObjectScrollPosition;
        private PickObject _currentPickObject;
        private List<PickObject> _pickObjects;
        private SerializedObject _pathFolderSerializedObject;
        private SerializedProperty _pathFolderProperty;
        private int _selectedSpawn;
        private GameObject _rootSpawn;
        private string _dataPath;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static UtilEditor.ProjectSetting<LevelEditorSettings> levelEditorSettings = new UtilEditor.ProjectSetting<LevelEditorSettings>();

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

        private void OnEnable()
        {
            _dataPath = Application.dataPath.Replace('/', '\\');
            Uniform.FoldoutSettings.LoadSetting();
            levelEditorSettings.LoadSetting();
            RefreshPickObject();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= GridUpdate;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;

            levelEditorSettings.SaveSetting();
            Uniform.FoldoutSettings.SaveSetting();
        }

        private void PlayModeStateChanged(PlayModeStateChange obj) { }

        private void OnProjectChange() { TryClose(); }

        private void OnHierarchyChange() { TryClose(); }

        private void GridUpdate(SceneView sceneView) { }

        private bool TryClose() { return false; }

        // ReSharper disable once UnusedMember.Local
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

            foreach (string whitepath in levelEditorSettings.Settings.pickupObjectWhiteList)
            {
                MakeGroupPrefab(whitepath);

                if (!Directory.Exists(whitepath)) continue;
                string[] directories = Directory.GetDirectories(whitepath);
                foreach (string directory in directories)
                {
                    string dir = directory.Replace('\\', '/');
                    MakeGroupPrefab(dir);
                }
            }

            void MakeGroupPrefab(string whitePath)
            {
                if (!Directory.Exists(whitePath) && !File.Exists(whitePath) || !whitePath.StartsWith("Assets"))
                {
                    Debug.LogWarning("[Level Editor]: Can not found folder '" + whitePath + "'");
                    return;
                }

                var levelObjects = new List<GameObject>();
                if (File.Exists(whitePath))
                {
                    levelObjects.Add(AssetDatabase.LoadAssetAtPath<GameObject>(whitePath));
                }
                else
                {
                    var removeList = new List<string>();
                    var nameFileExclude = new List<string>();

                    foreach (string blackPath in levelEditorSettings.Settings.pickupObjectBlackList)
                    {
                        if (IsChildOfPath(blackPath, whitePath)) removeList.Add(blackPath);
                    }

                    if (removeList.Contains(whitePath) || levelEditorSettings.Settings.pickupObjectBlackList.Contains(whitePath)) return;

                    foreach (string str in removeList)
                    {
                        if (File.Exists(str)) nameFileExclude.Add(Path.GetFileNameWithoutExtension(str));
                    }

                    levelObjects = UtilEditor.FindAllAssetsWithPath<GameObject>(whitePath.Replace(Application.dataPath, "").Replace("Assets/", ""))
                        .Where(lo => !(lo is null) && !nameFileExclude.Exists(_ => _.Equals(lo.name)))
                        .ToList();
                }

                string group = whitePath.Split('/').Last();
                if (File.Exists(whitePath))
                {
                    var pathInfo = new DirectoryInfo(whitePath);
                    if (pathInfo.Parent != null) group = pathInfo.Parent.Name;
                }

                foreach (var obj in levelObjects)
                {
                    var po = new PickObject { pickedObject = obj.gameObject, group = group };
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
                    GUI.Box(whiteArea, "[WHITE LIST]", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
                    GUI.backgroundColor = Color.white;
                    GUI.backgroundColor = new Color(1f, 0.13f, 0f);
                    GUI.Box(blackArea, "[BLACK LIST]", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
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
                                        ValidateByWhite(path, ref levelEditorSettings.Settings.pickupObjectBlackList);
                                        AddToWhiteList(path);
                                    }

                                    ReduceScopeDirectory(ref levelEditorSettings.Settings.pickupObjectWhiteList);
                                    levelEditorSettings.SaveSetting();
                                    RefreshAll();
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
                                        ValidateByBlack(path, ref levelEditorSettings.Settings.pickupObjectWhiteList);
                                        AddToBlackList(path);
                                    }

                                    ReduceScopeDirectory(ref levelEditorSettings.Settings.pickupObjectBlackList);
                                    levelEditorSettings.SaveSetting();
                                    RefreshAll();
                                }
                            }

                            break;
                        case EventType.MouseDown when @event.button == 1:
                            var menu = new GenericMenu();
                            if (whiteArea.Contains(@event.mousePosition))
                            {
                                menu.AddItem(new GUIContent("Clear All [WHITE LIST]"),
                                    false,
                                    () =>
                                    {
                                        levelEditorSettings.Settings.pickupObjectWhiteList.Clear();
                                        levelEditorSettings.SaveSetting();
                                    });
                            }
                            else if (blackArea.Contains(@event.mousePosition))
                            {
                                menu.AddItem(new GUIContent("Clear All [BLACK LIST]"),
                                    false,
                                    () =>
                                    {
                                        levelEditorSettings.Settings.pickupObjectBlackList.Clear();
                                        levelEditorSettings.SaveSetting();
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
                            if (levelEditorSettings.Settings.pickupObjectWhiteList.Count == 0)
                            {
                                EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(width - 50), GUILayout.Height(0));
                            }
                            else
                            {
                                foreach (string t in levelEditorSettings.Settings.pickupObjectWhiteList.ToList())
                                {
                                    DrawRow(t, width, _ => levelEditorSettings.Settings.pickupObjectWhiteList.Remove(_));
                                }
                            }
                        },
                        GUILayout.Width(width - 10));
                    Uniform.SpaceOneLine();
                    Uniform.VerticalScope(() =>
                        {
                            if (levelEditorSettings.Settings.pickupObjectBlackList.Count == 0)
                            {
                                EditorGUILayout.LabelField(new GUIContent(""), GUILayout.Width(width - 50), GUILayout.Height(0));
                            }
                            else
                            {
                                foreach (string t in levelEditorSettings.Settings.pickupObjectBlackList.ToList())
                                {
                                    DrawRow(t, width, _ => levelEditorSettings.Settings.pickupObjectBlackList.Remove(_));
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
                    EditorGUILayout.LabelField(new GUIContent(content), GUILayout.Width(width - 80));
                    GUILayout.FlexibleSpace();
                    Uniform.Button(Uniform.IconContent("d_scenevis_visible_hover", "Ping Selection"),
                        () =>
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(content);
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        });
                    Uniform.Button(Uniform.IconContent("Toolbar Minus", "Remove"),
                        () =>
                        {
                            action?.Invoke(content);
                            levelEditorSettings.SaveSetting();
                            RefreshAll();
                        });
                });
            }

            void ValidateByWhite(string path, ref List<string> blackList)
            {
                foreach (string t in blackList.ToList())
                {
                    if (path.Equals(t)) blackList.Remove(t);
                }
            }

            void ValidateByBlack(string path, ref List<string> whiteList)
            {
                foreach (string t in whiteList.ToList())
                {
                    if (path.Equals(t) || IsChildOfPath(t, path)) whiteList.Remove(t);
                }
            }
        }

        private void AddToWhiteList(string path)
        {
            var check = false;
            foreach (string whitePath in levelEditorSettings.Settings.pickupObjectWhiteList)
            {
                if (IsChildOfPath(path, whitePath)) check = true;
            }

            if (!check) levelEditorSettings.Settings.pickupObjectWhiteList.Add(path);
            levelEditorSettings.Settings.pickupObjectWhiteList = levelEditorSettings.Settings.pickupObjectWhiteList.Distinct().ToList(); //unique
        }

        private void AddToBlackList(string path)
        {
            var check = false;
            foreach (string blackPath in levelEditorSettings.Settings.pickupObjectBlackList)
            {
                if (IsChildOfPath(path, blackPath)) check = true;
            }

            if (!check) levelEditorSettings.Settings.pickupObjectBlackList.Add(path);
            levelEditorSettings.Settings.pickupObjectBlackList = levelEditorSettings.Settings.pickupObjectBlackList.Distinct().ToList(); //unique
        }

        // return true if child is childrent of parent
        private bool IsChildOfPath(string child, string parent)
        {
            if (child.Equals(parent)) return false;
            var allParent = new List<DirectoryInfo>();
            GetAllParentDirectories(new DirectoryInfo(child), ref allParent);

            foreach (var p in allParent)
            {
                bool check = EqualPath(p, parent);
                if (check) return true;
            }

            return false;
        }

        static void GetAllParentDirectories(DirectoryInfo directoryToScan, ref List<DirectoryInfo> directories)
        {
            while (true)
            {
                if (directoryToScan == null || directoryToScan.Name == directoryToScan.Root.Name || !directoryToScan.FullName.Contains("Assets")) return;

                directories.Add(directoryToScan);
                directoryToScan = directoryToScan.Parent;
            }
        }

        private bool EqualPath(FileSystemInfo info, string str)
        {
            string relativePath = info.FullName;
            if (relativePath.StartsWith(_dataPath)) relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            relativePath = relativePath.Replace('\\', '/');
            return str.Equals(relativePath);
        }

        private void ReduceScopeDirectory(ref List<string> source)
        {
            var arr = new string[source.Count];
            source.CopyTo(arr);
            var valueRemove = new List<string>();
            var unique = arr.Distinct().ToList();
            foreach (string u in unique)
            {
                var check = false;
                foreach (string k in unique)
                {
                    if (IsChildOfPath(u, k)) check = true;
                }

                if (check) valueRemove.Add(u);
            }

            foreach (string i in valueRemove)
            {
                unique.Remove(i);
            }

            source = unique;
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
                            _rootSpawn = (GameObject)EditorGUILayout.ObjectField("Spawn in GO here -->", _rootSpawn, typeof(GameObject), true);
                            break;
                    }
                }
            }
        }

        private void InternalDrawPickupArea()
        {
            Uniform.DrawUppercaseSectionWithRightClick("LEVEL_EDITOR_PICKUP_AREA", "PICKUP AREA", DrawPickupArea, ShowMenuRefresh);

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
                            () =>
                            {
                                if (Event.current.button == 1)
                                {
                                    Selection.activeObject = pickObj.pickedObject;
                                    EditorGUIUtility.PingObject(pickObj.pickedObject);
                                    return;
                                }

                                _currentPickObject = _currentPickObject == pickObj ? null : pickObj;
                            },
                            null,
                            GUILayout.Width(size),
                            GUILayout.Height(size));

                        var rect = GUILayoutUtility.GetLastRect().Grown(-3);
                        if (pickObj == _currentPickObject) EditorGUI.DrawRect(rect, new Color32(86, 221, 255, 242));
                        if (tex) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                        if (go)
                        {
                            if (pickObj == _currentPickObject)
                                EditorGUI.DropShadowLabel(rect, go.name, new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.LowerCenter });
                            else
                                EditorGUI.LabelField(rect, go.name, new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.LowerCenter });
                        }

                        counter++;
                        if (counter >= pickObjectsInGroup.Count) break;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            void ShowMenuRefresh()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Refresh Pickup  Area"), false, RefreshAll);
                menu.ShowAsContext();
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
                        parent = _rootSpawn ? _rootSpawn.transform : prefabRoot;
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
                        parent = _rootSpawn ? _rootSpawn.transform : null;
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