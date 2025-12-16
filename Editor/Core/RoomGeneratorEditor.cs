#if UNITY_EDITOR

using System.IO;
using EZRoomGen.Generation;
using EZRoomGen.Generation.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

#if USE_FBX_EXPORTER
using UnityEditor.Formats.Fbx.Exporter;
#endif

namespace EZRoomGen.Core.Editor
{
    /// <summary>
    /// Main editor class that handles room generation settings and displays layout grid.
    /// </summary>
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : UnityEditor.Editor
    {
        private Vector2 scrollPos;
        private static bool showGrid = false;

        private SerializedProperty gridWidthProp;
        private SerializedProperty gridHeightProp;
        private SerializedProperty defaultHeightProp;
        private SerializedProperty meshResolutionProp;
        private SerializedProperty uvScaleProp;
        private SerializedProperty heightScaleProp;
        private SerializedProperty cellWindingProp;
        private SerializedProperty realtimeGenerationProp;
        private SerializedProperty wallMaterialProp;
        private SerializedProperty floorMaterialProp;
        private SerializedProperty roofMaterialProp;
        private SerializedProperty automaticallyAddLightsProp;
        private SerializedProperty lampPrefabProp;
        private SerializedProperty roomSpacingProp;
        private SerializedProperty corridorSpacingProp;
        private SerializedProperty addCollidersProp;
        private SerializedProperty invertRoofProp;
        private SerializedProperty generatorTypeProp;
        private SerializedProperty generateLayoutAfterResizeProp;
        private SerializedProperty generatedInitialRoomProp;
        private SerializedProperty generateLayoutProp;

        private RoomCorridorLayoutGeneratorEditor roomCorridorGeneratorEditor = new();
        private DungeonLayoutGeneratorEditor dungeonLayoutGeneratorEditor = new();
        private MazeLayoutGeneratorEditor mazeLayoutGeneratorEditor = new();
        private RoomGridDrawer gridDrawer = new();

        private ILayoutGenerator layoutGenerator;
        private RoomGenerator generator;

        private void OnEnable()
        {
            gridWidthProp = serializedObject.FindProperty("gridWidth");
            gridHeightProp = serializedObject.FindProperty("gridHeight");
            defaultHeightProp = serializedObject.FindProperty("defaultHeight");
            meshResolutionProp = serializedObject.FindProperty("meshResolution");
            uvScaleProp = serializedObject.FindProperty("uvScale");
            heightScaleProp = serializedObject.FindProperty("gridData").FindPropertyRelative("gridHeightScale");
            cellWindingProp = serializedObject.FindProperty("cellWinding");
            realtimeGenerationProp = serializedObject.FindProperty("realtimeGeneration");
            wallMaterialProp = serializedObject.FindProperty("wallMaterial");
            floorMaterialProp = serializedObject.FindProperty("floorMaterial");
            roofMaterialProp = serializedObject.FindProperty("roofMaterial");
            automaticallyAddLightsProp = serializedObject.FindProperty("automaticallyAddLights");
            lampPrefabProp = serializedObject.FindProperty("lampPrefab");
            roomSpacingProp = serializedObject.FindProperty("roomSpacing");
            corridorSpacingProp = serializedObject.FindProperty("corridorSpacing");
            addCollidersProp = serializedObject.FindProperty("addColliders");
            invertRoofProp = serializedObject.FindProperty("invertRoof");
            generatorTypeProp = serializedObject.FindProperty("generatorType");
            generateLayoutAfterResizeProp = serializedObject.FindProperty("generateLayoutAfterResize");
            generatedInitialRoomProp = serializedObject.FindProperty("generatedInitialRoom");
            generateLayoutProp = serializedObject.FindProperty("generateLayout");

            generator = (RoomGenerator)target;
            if (generator == null) return;

            HandleFirstRun();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            ShowHeader();
            EditorGUILayout.Space();

            HandleGridSettings();

            bool generalSettingsChanged = DrawParameters();
            EditorGUILayout.Space();

            bool materialsChanged = DrawMaterials();
            EditorGUILayout.Space();

            bool lightingParamsChanged = DrawLightPlacement();
            EditorGUILayout.Space();

            DrawLayoutGeneratorMenu();
            EditorGUILayout.Space();

            HandleGrid();
            EditorGUILayout.Space();

            DrawBottomMenu();

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();

            if (realtimeGenerationProp.boolValue && (generalSettingsChanged || materialsChanged || lightingParamsChanged))
            {
                generator.GenerateRoom();
                // GenerateRoomFromLayout();
            }
        }

        /// <summary>
        /// Controls grid display.
        /// </summary>
        private void HandleGrid()
        {
            EditorGUILayout.BeginHorizontal();
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
            if (GUILayout.Button("Open Grid Window", GUILayout.Width(140)))
            {
                RoomGridEditorWindow.OpenWindow(generator);
            }
            EditorGUILayout.EndHorizontal();

            if (showGrid)
            {
                gridDrawer.Draw(generator);
            }
        }

        private void ShowHeader()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("EZ Room Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Find Player prefab", GUILayout.Width(150)))
            {
                string[] guids = AssetDatabase.FindAssets("EZRoomGenerator_Player t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var prefab = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (prefab)
                    {
                        EditorGUIUtility.PingObject(prefab);
                        Selection.activeObject = prefab;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Not found", "Player.prefab was not found in the project.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void HandleGridSettings()
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(gridWidthProp, Constants.MinRoomWidth, Constants.MaxRoomWidth, new GUIContent("Grid Width"));
            EditorGUILayout.IntSlider(gridHeightProp, Constants.MinRoomHeight, Constants.MaxRoomHeight, new GUIContent("Grid Height"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                generator.ResizeGrid(gridWidthProp.intValue, gridHeightProp.intValue, !generateLayoutAfterResizeProp.boolValue);
                if (realtimeGenerationProp.boolValue)
                {
                    if (generateLayoutAfterResizeProp.boolValue)
                    {
                        GenerateRoomFromLayout();
                    }
                    else
                    {
                        generator.GenerateRoom();
                    }
                }
            }

            EditorGUILayout.Space();
        }

        /// <summary>
        /// Shows mesh generation settings.
        /// </summary>
        private bool DrawParameters()
        {
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // EditorGUILayout.IntSlider(meshResolutionProp, 1, 3, new GUIContent("Mesh Resolution"));
            // EditorGUILayout.Slider(uvScaleProp, 1f, 10f, new GUIContent("UV Scale"));
            EditorGUILayout.PropertyField(cellWindingProp);
            EditorGUILayout.Slider(heightScaleProp, 0.1f, 10f, new GUIContent("Final Height Scale", "Height scale of final mesh, doesn't modify generated or drawn grid data."));
            EditorGUILayout.PropertyField(realtimeGenerationProp);
            if (generateLayoutProp.boolValue)
            {
                EditorGUILayout.PropertyField(generateLayoutAfterResizeProp);
            }
            else if (generateLayoutAfterResizeProp.boolValue)
            {
                generateLayoutAfterResizeProp.boolValue = false;
            }
            EditorGUILayout.PropertyField(addCollidersProp);
            EditorGUILayout.PropertyField(invertRoofProp);

            bool changed = EditorGUI.EndChangeCheck();

            return changed;
        }

        /// <summary>
        /// Shows materials settings.
        /// </summary>
        private bool DrawMaterials()
        {
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(wallMaterialProp);
            EditorGUILayout.PropertyField(floorMaterialProp);
            EditorGUILayout.PropertyField(roofMaterialProp);

            bool changed = EditorGUI.EndChangeCheck();

            return changed;
        }

        /// <summary>
        /// Displays options for procedural light placement.
        /// </summary>
        private bool DrawLightPlacement()
        {
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(automaticallyAddLightsProp);

            if (automaticallyAddLightsProp.boolValue)
            {
                EditorGUILayout.PropertyField(lampPrefabProp);
                EditorGUILayout.Slider(roomSpacingProp, 2f, 20f, new GUIContent("Room Spacing"));
                EditorGUILayout.Slider(corridorSpacingProp, 2f, 20f, new GUIContent("Corridor Spacing"));
            }

            bool changed = EditorGUI.EndChangeCheck();

            return changed;
        }

        /// <summary>
        /// Draws menu with basic operations for generated room.
        /// </summary>
        private void DrawBottomMenu()
        {
            EditorGUILayout.BeginHorizontal();

            // Clear Grid button
            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                generator.ClearGrid();
                if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
            }

            // Generate Room button (only if not in realtime mode)
            if (!realtimeGenerationProp.boolValue)
            {
                if (GUILayout.Button("Generate Room", GUILayout.Height(30)))
                {
                    generator.GenerateRoom();
                }
            }

#if USE_FBX_EXPORTER
            // Export as FBX button (only if room object exists)
            if (generator.GetRoomObject() != null)
            {
                if (GUILayout.Button("Export as FBX", GUILayout.Height(30)))
                {
                    string path = EditorUtility.SaveFilePanel("Export Room as FBX", "Assets", "GeneratedRoom.fbx", "fbx");
                    if (!string.IsNullOrEmpty(path))
                    {
                        ExportRoomAsFBX(path);
                    }
                }

                if (GUILayout.Button("Export as Prefab", GUILayout.Height(30)))
                {
                    string fbxPath = EditorUtility.SaveFilePanel("Export Room FBX", "Assets", "GeneratedRoom.fbx", "fbx");
                    if (!string.IsNullOrEmpty(fbxPath))
                    {
                        string defaultPrefabName = Path.GetFileNameWithoutExtension(fbxPath) + ".prefab";
                        string prefabPath = EditorUtility.SaveFilePanelInProject("Save Room Prefab", defaultPrefabName, "prefab", "Choose location for the room prefab");
                        if (!string.IsNullOrEmpty(prefabPath))
                        {
                            ExportRoomAsPrefab(fbxPath, prefabPath);
                        }
                    }
                }
            }
#endif

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays layout generation settings.
        /// </summary>
        private void DrawLayoutGeneratorMenu()
        {
            EditorGUILayout.LabelField("Layout Generation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generateLayoutProp);

            EditorGUILayout.PropertyField(generatorTypeProp);

            if (!generateLayoutProp.boolValue)
                return;

            var index = generatorTypeProp.enumValueIndex;
            LayoutGeneratorType type = (LayoutGeneratorType)index;

            bool shouldGenerate = false;

            if (type == LayoutGeneratorType.RoomCorridor)
            {
                shouldGenerate = roomCorridorGeneratorEditor.DrawInspector(generator.RoomCorridorGeneratorSettings);
                if (shouldGenerate) generator.DefaultHeight = generator.RoomCorridorGeneratorSettings.height;
                layoutGenerator = new RoomCorridorLayoutGenerator(generator.RoomCorridorGeneratorSettings);
            }
            else if (type == LayoutGeneratorType.Dungeon)
            {
                shouldGenerate = dungeonLayoutGeneratorEditor.DrawInspector(generator.DungeonGeneratorSettings);
                if (shouldGenerate) generator.DefaultHeight = generator.DungeonGeneratorSettings.height;
                layoutGenerator = new DungeonLayoutGenerator(generator.DungeonGeneratorSettings);
            }
            else if (type == LayoutGeneratorType.Maze)
            {
                shouldGenerate = mazeLayoutGeneratorEditor.DrawInspector(generator.MazeGeneratorSettings);
                if (shouldGenerate) generator.DefaultHeight = generator.MazeGeneratorSettings.height;
                layoutGenerator = new MazeLayoutGenerator(generator.MazeGeneratorSettings);
            }

            if (realtimeGenerationProp.boolValue)
            {
                if (shouldGenerate)
                {
                    GenerateRoomFromLayout();
                }
            }
            else
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Generate Layout & Room", GUILayout.Height(30), GUILayout.Width(240)))
                {
                    GenerateRoomFromLayout();
                }
            }
        }

        /// <summary>
        /// Generates initial room.
        /// </summary>
        private void HandleFirstRun()
        {
            if (!generatedInitialRoomProp.boolValue)
            {
                serializedObject.Update();
                layoutGenerator = new DungeonLayoutGenerator(generator.DungeonGeneratorSettings);
                GenerateRoomFromLayout();
                generatedInitialRoomProp.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void GenerateRoomFromLayout()
        {
            float[,] layout = layoutGenerator.Generate(gridWidthProp.intValue, gridHeightProp.intValue);
            generator.LoadGridFromArray(layout);
            generator.GenerateRoom();
        }

#if USE_FBX_EXPORTER
        /// <summary>
        /// Exports only the Wall, Floor, and Roof meshes from the currently generated room as an FBX file.
        /// </summary>
        /// <param name="path">Full file path for the exported FBX file (including .fbx extension).</param>
        public void ExportRoomAsFBX(string path)
        {
            if (generator.RoomObject == null)
            {
                Debug.LogWarning($"{Constants.ProjectDebugName}: No generated room to export.");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Create a temporary container to hold only Wall, Floor, and Roof objects
                GameObject tempContainer = new GameObject("TempExport");

                // Store original materials for later reassignment
                Dictionary<string, Material> originalMaterials = new Dictionary<string, Material>();

                // Find and copy only Wall, Floor, and Roof objects
                Transform[] children = generator.RoomObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child == generator.RoomObject.transform) continue;

                    string childName = child.gameObject.name;
                    if (childName == Constants.WallsMeshName || childName == Constants.FloorMeshName || childName == Constants.RoofMeshName)
                    {
                        // Store the original material
                        MeshRenderer originalRenderer = child.GetComponent<MeshRenderer>();
                        if (originalRenderer != null && originalRenderer.sharedMaterial != null)
                        {
                            originalMaterials[childName] = originalRenderer.sharedMaterial;
                        }

                        GameObject copy = Object.Instantiate(child.gameObject, tempContainer.transform);
                        copy.name = child.gameObject.name;
                        copy.transform.localPosition = child.localPosition;
                        copy.transform.localRotation = child.localRotation;
                        copy.transform.localScale = child.localScale;

                        // Remove the MeshRenderer component to avoid exporting materials
                        MeshRenderer renderer = copy.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            Object.DestroyImmediate(renderer);
                        }
                    }
                }

                ExportModelOptions exportModelOptions = new ExportModelOptions();
                exportModelOptions.ExportFormat = ExportFormat.Binary;
                exportModelOptions.KeepInstances = true;

                ModelExporter.ExportObject(path, tempContainer, exportModelOptions);

                // Clean up temporary object
                Object.DestroyImmediate(tempContainer);

                string relativePath = null;
                if (path.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.Refresh();

                    // Wait for import to complete and reassign materials
                    GameObject importedFBX = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    if (importedFBX != null)
                    {
                        // Get the model importer
                        ModelImporter importer = AssetImporter.GetAtPath(relativePath) as ModelImporter;
                        if (importer != null)
                        {
                            importer.materialImportMode = ModelImporterMaterialImportMode.None;
                            AssetDatabase.WriteImportSettingsIfDirty(relativePath);
                            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                        }

                        // Reassign original materials to the imported FBX
                        Transform[] fbxChildren = importedFBX.GetComponentsInChildren<Transform>(true);
                        foreach (Transform fbxChild in fbxChildren)
                        {
                            if (originalMaterials.ContainsKey(fbxChild.name))
                            {
                                MeshRenderer renderer = fbxChild.GetComponent<MeshRenderer>();
                                if (renderer != null)
                                {
                                    renderer.sharedMaterial = originalMaterials[fbxChild.name];
                                }
                            }
                        }

                        EditorUtility.SetDirty(importedFBX);
                        AssetDatabase.SaveAssets();
                    }

                    Debug.Log($"{Constants.ProjectDebugName}: ✅ Room meshes (Wall, Floor, Roof) successfully exported to: {relativePath}");
                }
                else
                {
                    Debug.Log($"{Constants.ProjectDebugName}: ✅ Room meshes (Wall, Floor, Roof) successfully exported to: {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{Constants.ProjectDebugName}: ❌ FBX Export failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Exports the room as FBX (Wall, Floor, Roof meshes) and creates a prefab that includes the FBX and all prefab instances from the hierarchy.
        /// </summary>
        /// <param name="fbxPath">Full file path for the exported FBX file (including .fbx extension).</param>
        /// <param name="prefabPath">Relative path within Assets folder for the prefab (including .prefab extension).</param>
        public void ExportRoomAsPrefab(string fbxPath, string prefabPath)
        {
            if (generator.RoomObject == null)
            {
                Debug.LogWarning("{Constants.ProjectDebugName}: No generated room to export.");
                return;
            }

            try
            {
                // Export FBX with Wall, Floor, and Roof meshes
                ExportRoomAsFBX(fbxPath);

                // Convert FBX path to relative Asset path
                string fbxRelativePath;
                if (fbxPath.StartsWith(Application.dataPath))
                {
                    fbxRelativePath = "Assets" + fbxPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("{Constants.ProjectDebugName}: FBX must be saved inside the Assets folder for prefab creation.");
                    return;
                }

                // Refresh to ensure FBX is imported
                AssetDatabase.Refresh();

                // Create a new GameObject to hold the complete room
                GameObject prefabRoot = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

                //Add the FBX model as a child
                GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxRelativePath);
                if (fbxModel != null)
                {
                    GameObject fbxInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel, prefabRoot.transform);
                    fbxInstance.name = Constants.DefaultRoomObjectName;
                }

                // Copy all prefab instances from the original hierarchy
                Transform[] children = generator.gameObject.GetComponentsInChildren<Transform>(true);
                Dictionary<string, Transform> parentCache = new Dictionary<string, Transform>();

                foreach (Transform child in children)
                {
                    if (child == generator.RoomObject.transform) continue;

                    string childName = child.gameObject.name;

                    // Skip Wall, Floor, and Roof and room object's transform since they're in the FBX
                    if (childName == Constants.WallsMeshName || childName == Constants.FloorMeshName
                        || childName == Constants.RoofMeshName || childName == generator.RoomObject.name)
                        continue;

                    // Check if this object is a prefab instance
                    if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                    {
                        // Get the outermost prefab instance
                        GameObject prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);

                        // Only process if this is the root of the prefab instance
                        if (prefabInstanceRoot == child.gameObject)
                        {
                            // Get the source prefab asset
                            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);

                            if (sourcePrefab != null)
                            {
                                Transform originalParent = child.parent;
                                string parentName = originalParent.name;

                                // Get or create parent in the new hierarchy
                                if (!parentCache.TryGetValue(parentName, out Transform targetParent))
                                {
                                    targetParent = new GameObject(parentName).transform;
                                    targetParent.SetParent(prefabRoot.transform);
                                    parentCache[parentName] = targetParent;
                                }

                                // Instantiate the prefab in the new hierarchy
                                GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, targetParent);
                                prefabInstance.name = child.gameObject.name;
                                prefabInstance.transform.localPosition = child.localPosition;
                                prefabInstance.transform.localRotation = child.localRotation;
                                prefabInstance.transform.localScale = child.localScale;

                                // Copy any property overrides from the original instance
                                PrefabUtility.ApplyPrefabInstance(child.gameObject, InteractionMode.AutomatedAction);
                            }
                        }
                    }
                }

                // Ensure prefab directory exists
                string prefabDirectory = Path.GetDirectoryName(prefabPath);
                if (!string.IsNullOrEmpty(prefabDirectory) && !AssetDatabase.IsValidFolder(prefabDirectory))
                {
                    string[] folders = prefabDirectory.Split('/');
                    string currentPath = "";
                    for (int i = 0; i < folders.Length; i++)
                    {
                        if (i == 0)
                        {
                            currentPath = folders[i];
                            continue;
                        }
                        string parentPath = currentPath;
                        currentPath += "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(currentPath))
                        {
                            AssetDatabase.CreateFolder(parentPath, folders[i]);
                        }
                    }
                }

                // Create the prefab
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

                // Clean up the temporary object
                Object.DestroyImmediate(prefabRoot);

                if (savedPrefab != null)
                {
                    AssetDatabase.Refresh();
                    Debug.Log($"{Constants.ProjectDebugName}: ✅ Complete room prefab created at: {prefabPath}");
                    EditorGUIUtility.PingObject(savedPrefab);
                }
                else
                {
                    Debug.LogError("{Constants.ProjectDebugName}: ❌ Failed to create prefab.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{Constants.ProjectDebugName}: ❌ Prefab creation failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
#endif
    }
}

#endif