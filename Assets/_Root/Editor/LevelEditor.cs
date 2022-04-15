﻿#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pancake.Common;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pancake.Editor
{
    public class LevelEditor : EditorWindow
    {
        private const string COUNT_FOLDER_PREFAB_KEY = "COUNT_FOLDER_PREFAB";
        private const string FOLDER_PREFAB_PATH_KEY = "FOLDER_PREFAB_PATH";
        private const string DEFAULT_FOLDER_PREFAB_PATH = "_Root/Prefabs";
        private const string DEFAULT_LEVEL_SETTING_PATH = "ProjectSettings/LevelEditorSetting.asset";
        private readonly string[] _optionsSpawn = { "Default", "Child", "Custom" };

        private Vector3 _prevPosition;
        private Vector2 _pickObjectScrollPosition;
        private PickObject _currentPickObject;
        private List<PickObject> _pickObjects;
        private SerializedObject _pathFolderSerializedObject;
        private static LevelEditorSettings levelEditorSettings;
        private SerializedProperty _pathFolderProperty;
        private bool _flagFoldoutPath;
        private GameObject _currentSpawnGameObject;
        private int _selectedSpawn;
        private int _childSpawnIndex;
        private GameObject _attachSpawnGameObject;

        private static LevelEditorSettings LevelEditorSettings
        {
            get
            {
                if (!DEFAULT_LEVEL_SETTING_PATH.FileExists()) return new LevelEditorSettings();
                string json = File.ReadAllText(DEFAULT_LEVEL_SETTING_PATH);
                levelEditorSettings = JsonUtility.FromJson<LevelEditorSettings>(json);
                return levelEditorSettings;
            }
            set
            {
                levelEditorSettings = value;
                SaveLevelEditorSetting();
            }
        }

        private List<PickObject> PickObjects => _pickObjects ?? (_pickObjects = new List<PickObject>());

        public void Init()
        {
            SceneView.duringSceneGui += GridUpdate;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
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
            {
                var unused = LevelEditorSettings;
            }

            //_pathFolderSerializedObject = new SerializedObject(_pathFolder);
            //_pathFolderProperty = _pathFolderSerializedObject.FindProperty("pathFolderPrefabs");
            // _reorderablePath = new CustomReorderable(_pathFolderProperty,
            //     OnAddCallback,
            //     OnRemoveCallback,
            //     OnReorderCallbackWithDetails,
            //     ActionCreateButtonSearchPath);

            RefreshPickObject();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        #region reorderable

        private void ActionCreateButtonSearchPath(Rect rect, ref SerializedProperty property, int propertyIndex)
        {
            // if (GUI.Button(rect, new GUIContent("", "Select folder"), EditorStyles.colorField))
            // {
            //     string pathResult = property.GetArrayElementAtIndex(propertyIndex).stringValue;
            //     string path = EditorUtility.OpenFolderPanel("Select folder output", pathResult, "");
            //     if (!string.IsNullOrEmpty(path))
            //     {
            //         pathResult = path;
            //         string[] subFolders = Directory.GetDirectories(pathResult);
            //
            //         if (!_pathFolder.pathFolderPrefabs.Contains(pathResult))
            //         {
            //             if (!string.IsNullOrEmpty($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{propertyIndex}")) SavePath(propertyIndex, pathResult);
            //
            //             property.GetArrayElementAtIndex(propertyIndex).stringValue = path;
            //         }
            //         else
            //         {
            //             Debug.LogWarning("[Level Editor] : folder already exist in the container!");
            //         }
            //
            //         foreach (string pathSubFolder in subFolders)
            //         {
            //             var check = false;
            //             int size = property.arraySize;
            //             for (int j = 0; j < size; j++)
            //             {
            //                 if (pathSubFolder.Equals(property.GetArrayElementAtIndex(j).stringValue))
            //                 {
            //                     check = true;
            //                 }
            //             }
            //
            //             if (!check)
            //             {
            //                 if (!string.IsNullOrEmpty($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{size}"))
            //                 {
            //                     SavePath(size, pathSubFolder);
            //                 }
            //
            //                 property.serializedObject.UpdateIfRequiredOrScript();
            //                 property.InsertArrayElementAtIndex(size);
            //                 property.serializedObject.ApplyModifiedProperties();
            //             }
            //         }
            //
            //         EditorPrefs.SetInt($"{Application.identifier}_{COUNT_FOLDER_PREFAB_KEY}", property.arraySize);
            //
            //         RefreshPathFolder();
            //         RefreshAll();
            //     }
            //
            //     GUI.FocusControl(null);
            // }
        }

        private void OnReorderCallbackWithDetails(ReorderableList list, int oldIndex, int newIndex)
        {
            // _pathFolder.pathFolderPrefabs.Swap(oldIndex, newIndex);
            // UtilEditor.SwapEditorPrefs<string>($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{oldIndex}",
            //     $"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{newIndex}");
            // RefreshAll();
        }

        private void ForceSavePath()
        {
            // for (var j = 0; j < _pathFolder.pathFolderPrefabs.Count; j++)
            // {
            //     if (string.IsNullOrEmpty(_pathFolder.pathFolderPrefabs[j])) continue;
            //     SavePath(j, _pathFolder.pathFolderPrefabs[j]);
            // }
        }

        private void SavePath(int index, string value) { EditorPrefs.SetString($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{index}", value); }

        private void OnAddCallback(ReorderableList list) { UpdateNumberElement(list); }

        private void OnRemoveCallback(ReorderableList list) { UpdateNumberElement(list); }

        private void UpdateNumberElement(ReorderableList list)
        {
            EditorPrefs.SetInt($"{Application.identifier}_{COUNT_FOLDER_PREFAB_KEY}", list.count);
            RefreshAll();
        }

        #endregion

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
            ForceSavePath();
            MapEditorWindowStatics.ClearPreviews();
            RefreshPickObject();
            ClearEditor();
        }

        /// <summary>
        /// display picked object in editor
        /// </summary>
        private void RefreshPickObject()
        {
            RefreshPathFolder();

            _pickObjects = new List<PickObject>();

            // for (int i = 0; i < _pathFolder.pathFolderPrefabs.ToList().Count; i++)
            // {
            //     MakeGroupPrefab(i, _pathFolder.pathFolderPrefabs[i], ref _pathFolder.pathFolderPrefabs);
            // }

            void MakeGroupPrefab(int index, string path, ref List<string> paths)
            {
                string pathLocal = path;
                if (path.Equals(DEFAULT_FOLDER_PREFAB_PATH))
                {
                    pathLocal = pathLocal.Insert(0, $"{Application.dataPath}/");
                }

                if (!Directory.Exists(pathLocal) || !pathLocal.Contains(Application.dataPath))
                {
                    Debug.LogWarning("[Level Editor]: Can not found folder '" + path + "'");
                    return;
                }

                var levelObjects = UtilEditor.FindAllAssetsWithPath<GameObject>(path.Replace(Application.dataPath, "")).Where(lo => !(lo is null)).ToList();
                if (levelObjects.Count == 0)
                {
                    paths.Remove(path);
                    EditorPrefs.DeleteKey($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{index}");
                    EditorPrefs.SetInt($"{Application.identifier}_{COUNT_FOLDER_PREFAB_KEY}", paths.Count);
                    return;
                }

                foreach (var obj in levelObjects)
                {
                    var po = new PickObject { pickedObject = obj.gameObject, group = path.Split('/').Last() };
                    _pickObjects.Add(po);
                }
            }
        }

        private void RefreshPathFolder()
        {
            int count = EditorPrefs.GetInt($"{Application.identifier}_{COUNT_FOLDER_PREFAB_KEY}", 1);
            // if (_pathFolder != null)
            // {
            //     _pathFolder.pathFolderPrefabs = new List<string>(count);
            //     _pathFolder.pathFolderPrefabs.Clear();
            //     for (var i = 0; i < count; i++)
            //     {
            //         _pathFolder.pathFolderPrefabs.Add(EditorPrefs.HasKey($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{i}")
            //             ? EditorPrefs.GetString($"{Application.identifier}_{FOLDER_PREFAB_PATH_KEY}_{i}")
            //             : DEFAULT_FOLDER_PREFAB_PATH);
            //     }
            // }
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

            // _flagFoldoutPath = EditorGUILayout.Foldout(_flagFoldoutPath, "", true);
            // if (_flagFoldoutPath)
            // {
            //     _pathFolderSerializedObject?.Update();
            //     //_reorderablePath?.DoLayoutList();
            //     _pathFolderSerializedObject?.ApplyModifiedProperties();
            // }

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
                var @event = Event.current;
                Uniform.Horizontal(() =>
                {
                    var whiteArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
                    var blackArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
                    GUI.backgroundColor = new Color(0f, 0.83f, 1f);
                    GUI.Box(whiteArea, "WHITE LIST", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
                    GUI.backgroundColor = Color.white;
                    GUI.backgroundColor = new Color(1f, 0.13f, 0f);
                    GUI.Box(blackArea, "BLACK LIST", new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
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
                                        LevelEditorSettings.pickupObjectWhiteList.Add(path);
                                        
                                        Debug.Log("white list :" + Directory.GetParent(path)?.FullName);
                                    }
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
                                        LevelEditorSettings.pickupObjectBlackList.Add(path);
                                        Debug.Log("black list :" + path);
                                    }
                                }
                            }


                            SaveLevelEditorSetting();
                            break;
                    }
                });
            }

            void AddToWhiteList(string path)
            {
                
            }

            void AddToBlackList(string path)
            {
                
            }

            bool IsDirectory(string path) => Directory.Exists(path);
            bool IsFile(string path) => File.Exists(path);

            bool IsPathAlreadyInWhiteList(string path)
            {
                var lowestFolder = path;
                if (IsFile(path))
                {
                    if (!LevelEditorSettings.pickupObjectWhiteList.Contains(path))
                    {
                        
                    }
                    lowestFolder = Directory.GetParent(path)?.FullName;
                }
                
                
                

                return false;
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
                        case "Child":
                            Uniform.SpaceOneLine();
                            _childSpawnIndex = EditorGUILayout.IntField("Child (X): ", _childSpawnIndex, GUILayout.Width(400), GUILayout.Height(20));
                            break;
                        case "Custom":
                            Uniform.SpaceOneLine();
                            _attachSpawnGameObject = (GameObject)EditorGUILayout.ObjectField("Spawn in GO here -->", _attachSpawnGameObject, typeof(GameObject), true);
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
                var tex = MapEditorWindowStatics.GetPreview(_currentPickObject?.pickedObject);
                if (tex)
                {
                    string pickObjectName = _currentPickObject?.pickedObject.name;
                    if (GUILayout.Button(tex, GUILayout.Height(60)))
                    {
                        _currentPickObject = null;
                    }

                    EditorGUILayout.LabelField($"Selected: {pickObjectName}. Press Icon Or Escape Key To Deselect");
                    Uniform.HelpBox("Shift + Click To Add", MessageType.Info);
                }
                else
                {
                    Uniform.HelpBox("Select An Object First", MessageType.Info);
                }


                _pickObjectScrollPosition = EditorGUILayout.BeginScrollView(_pickObjectScrollPosition);

                var resultSplitGroupObjects = PickObjects.GroupBy(_ => _.group).Select(_ => _.ToList()).ToList();
                var key = $"{Application.identifier}_MAPEDITOR_FOLDOUT_GROUP_KEY_";
                // var foldouts = new bool[_pathFolder.pathFolderPrefabs.Count];
                //
                // int numberGroup = Math.Min(foldouts.Length, resultSplitGroupObjects.Count);
                //
                // for (var i = 0; i < numberGroup; i++)
                // {
                //     foldouts[i] = EditorPrefs.GetBool($"{key}_{i}", false);
                //
                //     EditorGUILayout.BeginVertical();
                //
                //    // MakeGroupHeaderButton(ref foldouts[i], _pathFolder.pathFolderPrefabs[i].Split('/').Last(), $"{key}_{i}");
                //
                //     EditorGUILayout.EndVertical();
                //
                //     if (foldouts[i]) DrawInGroup(resultSplitGroupObjects[i]);
                // }

                EditorGUILayout.EndScrollView();
            }

            void MakeGroupHeaderButton(ref bool foldout, string title, string keyFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                if (Uniform.Button(title, color: foldout ? (Color?)new Color32(255, 111, 117, 255) : null))
                {
                    foldout = !foldout;
                    EditorPrefs.SetBool(keyFoldout, foldout);
                }

                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
            }

            void DrawInGroup(IReadOnlyList<PickObject> pickObjectsInGroup)
            {
                var counter = 0;
                CalculateIdealCount(Screen.width - 50,
                    60,
                    135,
                    5,
                    out int count,
                    out float size);
                count = Mathf.Max(1, count);
                while (counter >= 0 && counter < pickObjectsInGroup.Count)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    for (var x = 0; x < count; x++)
                    {
                        var pickObj = pickObjectsInGroup[counter];
                        var go = pickObj.pickedObject;
                        var tex = MapEditorWindowStatics.GetPreview(go);
                        if (GUILayout.Button("", GUILayout.Width(size), GUILayout.Height(size)))
                        {
                            _currentPickObject = _currentPickObject == pickObj ? null : pickObj;
                        }

                        var rect = GUILayoutUtility.GetLastRect().Grown(-3);
                        if (pickObj == _currentPickObject) EditorGUI.DrawRect(rect, new Color32(11, 255, 111, 255));
                        if (tex) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                        if (go) EditorGUI.LabelField(rect, go.name, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.LowerCenter });

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
        /// 
        /// </summary>
        /// <param name="pickObject"></param>
        /// <param name="worldPos"></param>
        private void AddPickObject(PickObject pickObject, Vector3 worldPos)
        {
            if (pickObject?.pickedObject)
            {
                Transform parent = null;

#if UNITY_2021_1_OR_NEWER
                UnityEditor.SceneManagement.PrefabStage currentPrefabState;
                currentPrefabState = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
                UnityEditor.Experimental.SceneManagement.PrefabStage currentPrefabState;
                currentPrefabState = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif

                if (currentPrefabState != null)
                {
                    if (_optionsSpawn[_selectedSpawn] == "Default")
                    {
                        parent = currentPrefabState.prefabContentsRoot.transform;
                    }
                    else if (_optionsSpawn[_selectedSpawn] == "Child")
                    {
                        parent = currentPrefabState.prefabContentsRoot.transform.GetChild(_childSpawnIndex);
                    }
                    else
                    {
                        parent = _attachSpawnGameObject ? _attachSpawnGameObject.transform : currentPrefabState.prefabContentsRoot.transform;
                    }
                }
                else
                {
                    var levelMap = GameObject.Find("LevelContent");
                    if (levelMap != null) parent = levelMap.transform;
                }

                var inst = Instantiate(parent);
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
        /// 
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
            size = availableSpace / count;
            return goodness;
        }

        private void ClearEditor() { Repaint(); }
    }

    public static class MapEditorWindowStatics
    {
        private static PreviewGenerator previewGenerator;

        private static PreviewGenerator PreviewGenerator =>
            previewGenerator ?? (previewGenerator =
                new PreviewGenerator { width = 512, height = 512, transparentBackground = true, sizingType = PreviewGenerator.ImageSizeType.Fit });

        private static Dictionary<GameObject, Texture2D> previewDict;

        // ReSharper disable once UnusedMember.Local
        private static void SelectPrefabFolder()
        {
            var path = "Assets/_Root/Prefabs/";
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        public static void ClearPreviews()
        {
            if (previewDict != null)
            {
                foreach (var kvp in previewDict)
                {
                    Object.DestroyImmediate(kvp.Value);
                }

                previewDict.Clear();
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static void ClearPreview(GameObject go)
        {
            // ReSharper disable once RedundantAssignment
            // ReSharper disable once InlineOutVariableDeclaration
            Texture2D tex = null;
            if (previewDict?.TryGetValue(go, out tex) ?? false)
            {
                Object.DestroyImmediate(tex);
                previewDict.Remove(go);
            }
        }

        public static Texture2D GetPreview(GameObject go, bool canCreate = true)
        {
            if (!go) return null;
            if (!canCreate) return previewDict?.GetOrDefault(go);
            else
            {
                if (previewDict == null) previewDict = new Dictionary<GameObject, Texture2D>();
                previewDict.TryGetValue(go, out var tex);
                if (!tex)
                {
                    tex = PreviewGenerator.CreatePreview(go.gameObject);
                    previewDict[go] = tex;
                }

                return tex;
            }
        }

        [MenuItem("Tools/Snorlax/Level Editor &_3")]
        public static void OpenEditor()
        {
            var window = EditorWindow.GetWindow<LevelEditor>("Level Editor", true, UtilEditor.InspectorWindow);

            if (window)
            {
                window.Init();
                window.Show(true);
            }
        }
    }

    public class PreviewGenerator
    {
        public static readonly PreviewGenerator Default = new PreviewGenerator();

        private const int MAX_TEXTURE_SIZE = 2048;
        private static int latePreviewQueued;

        // ReSharper disable once MemberCanBePrivate.Global
        public Vector3 previewPosition = new Vector3(9999, 9999, -9999);

        // ReSharper disable once MemberCanBePrivate.Global
        public Vector3 latePreviewOffset = new Vector3(100, 100, 0);

        public bool transparentBackground = true;

        // ReSharper disable once MemberCanBePrivate.Global
        public Color solidBackgroundColor = new Color(0.3f, 0.3f, 0.3f);

        // ReSharper disable once MemberCanBePrivate.Global
        public FilterMode imageFilterMode = FilterMode.Point;
        public ImageSizeType sizingType = ImageSizeType.PixelsPerUnit;

        // ReSharper disable once MemberCanBePrivate.Global
        public int pixelPerUnit = 32;
        public int width = 256;
        public int height = 256;

        // ReSharper disable once MemberCanBePrivate.Global
        public CaptureTiming captureTiming = CaptureTiming.Instant;

        // ReSharper disable once MemberCanBePrivate.Global
        public float timingCounter = 1;

        // ReSharper disable once MemberCanBePrivate.Global
        public Action<Texture2D> onCapturedCallback;

        // ReSharper disable once MemberCanBePrivate.Global
        public Action<GameObject> onPreCaptureCallback;


        public enum ImageSizeType
        {
            PixelsPerUnit,
            Fit,
            Fill,
            Stretch,
        }

        public enum CaptureTiming
        {
            Instant,
            EndOfFrame,
            NextFrame,
            NextSecond,
            NextSecondRealtime
        }


        public PreviewGenerator Copy()
        {
            return new PreviewGenerator()
            {
                previewPosition = previewPosition,
                latePreviewOffset = latePreviewOffset,
                transparentBackground = transparentBackground,
                solidBackgroundColor = solidBackgroundColor,
                imageFilterMode = imageFilterMode,
                sizingType = sizingType,
                pixelPerUnit = pixelPerUnit,
                width = width,
                height = height,
                captureTiming = captureTiming,
                timingCounter = timingCounter,
                onCapturedCallback = onCapturedCallback,
                onPreCaptureCallback = onPreCaptureCallback,
            };
        }

        public PreviewGenerator OnCaptured(Action<Texture2D> callback)
        {
            onCapturedCallback = callback;
            return this;
        }

        public PreviewGenerator OnPreCaptured(Action<GameObject> callback)
        {
            onPreCaptureCallback = callback;
            return this;
        }

        public PreviewGenerator SetTiming(CaptureTiming timing, float? counter = null)
        {
            captureTiming = timing;
            timingCounter = counter ?? timingCounter;
            return this;
        }


        public Texture2D CreatePreview(GameObject obj, bool clone = true)
        {
            if (!CanCreatePreview(obj))
            {
                onCapturedCallback?.Invoke(null);
                return null;
            }

            var cachedPosition = obj.transform.position;
            var prevObj = clone ? Object.Instantiate(obj, null) : obj;
            prevObj.transform.position = previewPosition + latePreviewQueued * latePreviewOffset;

            var bounds = Util.GetRendererBounds(prevObj, false);
            var size = GetImageSize(bounds);
            var cam = CreatePreviewCamera(bounds);

            latePreviewQueued++;
            var dummy = DummyBehaviour.Create("Preview dummy");

            void Callback()
            {
                latePreviewQueued--;
                if (clone)
                {
                    Object.DestroyImmediate(prevObj);
                }
                else
                {
                    prevObj.transform.position = cachedPosition;
                }

                Object.DestroyImmediate(cam.gameObject);
                Object.DestroyImmediate(dummy.gameObject);
            }

            if (captureTiming == CaptureTiming.Instant)
                return WrappedCapture(prevObj,
                    cam,
                    size.x,
                    size.y,
                    Callback);

            dummy.StartCoroutine(TimedCapture(prevObj,
                cam,
                size.x,
                size.y,
                Callback));
            return null;
        }


        private void NotifyPreviewTaking(GameObject go, Action<IPreviewComponent> action)
        {
            var allComps = go.GetComponentsInChildren<Component>();
            foreach (var comp in allComps)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (comp is IPreviewComponent component) action?.Invoke(component);
            }
        }

        private bool CanCreatePreview(GameObject obj) { return obj != null && obj.GetComponentsInChildren<Renderer>().Any(r => r != null && r.enabled); }

        private Camera CreatePreviewCamera(Bounds bounds)
        {
            var camObj = new GameObject("Preview generator camera");
            var cam = camObj.AddComponent<Camera>();
            cam.name = "Preview generator camera";

            cam.transform.position = bounds.center + Vector3.back * (bounds.extents.z + 2);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = bounds.size.z + 4;

            cam.orthographic = true;
            cam.orthographicSize = bounds.extents.y;
            cam.aspect = bounds.extents.x / bounds.extents.y;

            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = solidBackgroundColor;
            if (transparentBackground) cam.backgroundColor *= 0;

            cam.enabled = false;

            return cam;
        }

        private Vector2Int GetImageSize(Bounds bounds)
        {
            var w = 1;
            var h = 1;

            if (sizingType == ImageSizeType.PixelsPerUnit)
            {
                w = Mathf.CeilToInt(bounds.size.x * pixelPerUnit);
                h = Mathf.CeilToInt(bounds.size.y * pixelPerUnit);
            }
            else if (sizingType == ImageSizeType.Stretch)
            {
                w = width;
                h = height;
            }
            else if (sizingType == ImageSizeType.Fit || sizingType == ImageSizeType.Fill)
            {
                float widthFactor = width / bounds.size.x;
                float heightFactor = height / bounds.size.y;
                float factor = sizingType == ImageSizeType.Fit ? Mathf.Min(widthFactor, heightFactor) : Mathf.Max(widthFactor, heightFactor);

                w = Mathf.CeilToInt(bounds.size.x * factor);
                h = Mathf.CeilToInt(bounds.size.y * factor);
            }

            if (w > MAX_TEXTURE_SIZE || h > MAX_TEXTURE_SIZE)
            {
                float downscaleWidthFactor = (float)MAX_TEXTURE_SIZE / w;
                float downscaleHeightFactor = (float)MAX_TEXTURE_SIZE / h;
                float downscaleFactor = Mathf.Min(downscaleWidthFactor, downscaleHeightFactor);

                w = Mathf.CeilToInt(w * downscaleFactor);
                h = Mathf.CeilToInt(h * downscaleFactor);
            }

            return new Vector2Int(w, h);
        }

        private IEnumerator TimedCapture(GameObject obj, Camera cam, int w, int h, Action callback)
        {
            if (captureTiming == CaptureTiming.Instant)
            {
            }
            else if (captureTiming == CaptureTiming.EndOfFrame) yield return new WaitForEndOfFrame();
            else if (captureTiming == CaptureTiming.NextFrame)
                for (var i = 0; i < timingCounter; i++)
                    yield return null;
            else if (captureTiming == CaptureTiming.NextSecond) yield return new WaitForSeconds(timingCounter);
            else if (captureTiming == CaptureTiming.NextSecondRealtime) yield return new WaitForSecondsRealtime(timingCounter);

            WrappedCapture(obj,
                cam,
                w,
                h,
                callback);
        }

        private Texture2D WrappedCapture(GameObject obj, Camera cam, int w, int h, Action callback)
        {
            onPreCaptureCallback?.Invoke(obj);

            NotifyPreviewTaking(obj, i => i.OnPreviewCapturing(this));
            var tex = DoCapture(cam, w, h);
            NotifyPreviewTaking(obj, i => i.OnPreviewCaptured(this));

            callback?.Invoke();
            onCapturedCallback?.Invoke(tex);

            return tex;
        }

        private Texture2D DoCapture(Camera cam, int w, int h)
        {
            var temp = RenderTexture.active;
            RenderTexture renderTex = null;
            try
            {
                renderTex = RenderTexture.GetTemporary(w, h, 16);
            }
            catch (Exception)
            {
                //
            }

            RenderTexture.active = renderTex;

            cam.enabled = true;
            cam.targetTexture = renderTex;
            cam.Render();
            cam.targetTexture = null;
            cam.enabled = false;

            if (w <= 0) w = 512;
            if (h <= 0) h = 512;
            var tex = new Texture2D(w, h, transparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24, false) { filterMode = imageFilterMode };
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false, false);

            RenderTexture.active = temp;
            RenderTexture.ReleaseTemporary(renderTex);
            return tex;
        }
    }

    public interface IPreviewComponent
    {
        void OnPreviewCapturing(PreviewGenerator preview);

        void OnPreviewCaptured(PreviewGenerator preview);
    }
}
#endif