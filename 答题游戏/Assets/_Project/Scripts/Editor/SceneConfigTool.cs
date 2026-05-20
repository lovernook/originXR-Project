#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace OriginXR.EditorTools
{
    /// <summary>
    /// 场景配置工具（Unity Editor 窗口）
    /// 负责：
    /// 1. 可视化配置 LobbyScene 中建筑入口和 NPC 参数
    /// 2. 从场景扫描 BuildingEntry 组件并批量编辑
    /// 3. 导出/导入场景配置 JSON
    /// 4. 在 Scene 视图中绘制 Gizmo 辅助线
    ///
    /// 使用方式：菜单栏 OriginXR → Scene Config Tool
    /// </summary>
    public class SceneConfigTool : EditorWindow
    {
        // === Tab ===
        private int _selectedTab;

        // === 建筑配置 ===
        private List<BuildingConfigData> _buildings = new List<BuildingConfigData>();
        private Vector2 _buildingScrollPos;
        private bool _showBuildingDetail;

        // === NPC配置 ===
        private List<NPCConfigData> _npcs = new List<NPCConfigData>();
        private Vector2 _npcScrollPos;
        private bool _showNPCDetail;

        // === 导出 ===
        private string _exportPath = "Assets/_Project/Resources/Configs/SceneConfig.json";

        [Serializable]
        private class BuildingConfigData
        {
            public string id;
            public string name;
            public string type;          // TeachingBuilding/Arena/GuildHall/...
            public string targetSceneName;
            public string targetPanelName;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 teleportTarget;
            public string unlockCondition;
        }

        [Serializable]
        private class NPCConfigData
        {
            public string id;
            public string name;
            public string modelId;
            public Vector3 position;
            public Vector3 rotation;
            public string greetingMessage;
            public string functionId;
            public List<NPCDialogue> dialogues;
        }

        [Serializable]
        private class NPCDialogue
        {
            public string text;
            public string voiceId;
            public float duration;
        }

        // === 窗口入口 ===

        [MenuItem("OriginXR/Scene Config Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneConfigTool>("场景配置工具");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            ScanBuildingsFromScene();
            ScanNPCsFromScene();
        }

        private void OnGUI()
        {
            // Tab 切换
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_selectedTab == 0, "建筑入口配置", EditorStyles.toolbarButton)) _selectedTab = 0;
            if (GUILayout.Toggle(_selectedTab == 1, "NPC 配置", EditorStyles.toolbarButton)) _selectedTab = 1;
            if (GUILayout.Toggle(_selectedTab == 2, "导入/导出", EditorStyles.toolbarButton)) _selectedTab = 2;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("从场景扫描", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ScanBuildingsFromScene();
                ScanNPCsFromScene();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            switch (_selectedTab)
            {
                case 0: DrawBuildingConfigTab(); break;
                case 1: DrawNPCConfigTab(); break;
                case 2: DrawExportTab(); break;
            }
        }

        // === 建筑配置 Tab ===

        private void DrawBuildingConfigTab()
        {
            EditorGUILayout.LabelField($"场景中的建筑入口 ({_buildings.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("+ 新建建筑入口", GUILayout.Height(30)))
            {
                _buildings.Add(new BuildingConfigData { id = Guid.NewGuid().ToString().Substring(0, 8), name = "新建筑" });
            }

            _buildingScrollPos = EditorGUILayout.BeginScrollView(_buildingScrollPos);

            for (int i = 0; i < _buildings.Count; i++)
            {
                DrawBuildingEntry(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBuildingEntry(int index)
        {
            var b = _buildings[index];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            b.name = EditorGUILayout.TextField("名称", b.name);
            b.type = EditorGUILayout.TextField("类型", b.type, GUILayout.Width(150));

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _buildings.RemoveAt(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            _showBuildingDetail = EditorGUILayout.Foldout(_showBuildingDetail, "详细配置");
            if (_showBuildingDetail)
            {
                b.id = EditorGUILayout.TextField("ID", b.id);
                b.targetSceneName = EditorGUILayout.TextField("目标场景", b.targetSceneName);
                b.targetPanelName = EditorGUILayout.TextField("目标面板", b.targetPanelName);
                b.position = EditorGUILayout.Vector3Field("位置", b.position);
                b.rotation = EditorGUILayout.Vector3Field("旋转", b.rotation);
                b.teleportTarget = EditorGUILayout.Vector3Field("传送目标", b.teleportTarget);
                b.unlockCondition = EditorGUILayout.TextField("解锁条件", b.unlockCondition);

                if (GUILayout.Button("定位到此建筑", GUILayout.Height(25)))
                {
                    SelectBuildingInScene(b.id);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // === NPC 配置 Tab ===

        private void DrawNPCConfigTab()
        {
            EditorGUILayout.LabelField($"场景中的 NPC ({_npcs.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("+ 新建 NPC", GUILayout.Height(30)))
            {
                _npcs.Add(new NPCConfigData { id = Guid.NewGuid().ToString().Substring(0, 8), name = "新NPC", dialogues = new List<NPCDialogue>() });
            }

            _npcScrollPos = EditorGUILayout.BeginScrollView(_npcScrollPos);

            for (int i = 0; i < _npcs.Count; i++)
            {
                DrawNPCEntry(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNPCEntry(int index)
        {
            var npc = _npcs[index];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            npc.name = EditorGUILayout.TextField("名称", npc.name);
            npc.modelId = EditorGUILayout.TextField("模型ID", npc.modelId, GUILayout.Width(120));

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _npcs.RemoveAt(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            _showNPCDetail = EditorGUILayout.Foldout(_showNPCDetail, "详细配置");
            if (_showNPCDetail)
            {
                npc.position = EditorGUILayout.Vector3Field("位置", npc.position);
                npc.rotation = EditorGUILayout.Vector3Field("旋转", npc.rotation);
                npc.greetingMessage = EditorGUILayout.TextField("问候语", npc.greetingMessage);
                npc.functionId = EditorGUILayout.TextField("功能入口ID", npc.functionId);

                // 对话列表
                EditorGUILayout.LabelField($"对话列表 ({npc.dialogues.Count})", EditorStyles.miniBoldLabel);
                for (int d = 0; d < npc.dialogues.Count; d++)
                {
                    EditorGUILayout.BeginHorizontal();
                    npc.dialogues[d].text = EditorGUILayout.TextField("", npc.dialogues[d].text);
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                        npc.dialogues.RemoveAt(d);
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ 添加对话"))
                    npc.dialogues.Add(new NPCDialogue());
            }

            EditorGUILayout.EndVertical();
        }

        // === 导入/导出 Tab ===

        private void DrawExportTab()
        {
            EditorGUILayout.LabelField("场景配置导入/导出", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            _exportPath = EditorGUILayout.TextField("导出路径", _exportPath);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("导出 JSON", GUILayout.Height(40), GUILayout.Width(120)))
            {
                ExportToJson();
            }

            if (GUILayout.Button("导入 JSON", GUILayout.Height(40), GUILayout.Width(120)))
            {
                ImportFromJson();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 验证
            if (GUILayout.Button("验证配置完整性", GUILayout.Height(30)))
            {
                ValidateConfig();
            }

            // 导出预览
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("导出预览 (JSON):", EditorStyles.boldLabel);
            string jsonPreview = JsonUtility.ToJson(new { buildings = _buildings, npcs = _npcs }, true);
            EditorGUILayout.TextArea(jsonPreview.Substring(0, Mathf.Min(jsonPreview.Length, 500)) + "...", GUILayout.Height(200));
        }

        // === 场景扫描 ===

        private void ScanBuildingsFromScene()
        {
            _buildings.Clear();
            var entries = FindObjectsOfType<Lobby.BuildingEntry>();
            foreach (var entry in entries)
            {
                var t = entry.transform;
                _buildings.Add(new BuildingConfigData
                {
                    id = entry.buildingId,
                    name = entry.buildingName,
                    type = entry.type.ToString(),
                    targetSceneName = entry.targetSceneName,
                    targetPanelName = entry.targetPanelName,
                    position = t.position,
                    rotation = t.eulerAngles,
                    teleportTarget = entry.teleportTargetPosition
                });
            }
            Debug.Log($"[SceneConfigTool] 扫描到 {_buildings.Count} 个建筑入口");
        }

        private void ScanNPCsFromScene()
        {
            // 扫描标签为 "NPC" 的 GameObject
            _npcs.Clear();
            GameObject[] npcObjects = GameObject.FindGameObjectsWithTag("NPC");
            foreach (var obj in npcObjects)
            {
                _npcs.Add(new NPCConfigData
                {
                    id = obj.name,
                    name = obj.name,
                    position = obj.transform.position,
                    rotation = obj.transform.eulerAngles,
                    dialogues = new List<NPCDialogue>()
                });
            }
            Debug.Log($"[SceneConfigTool] 扫描到 {_npcs.Count} 个 NPC");
        }

        private void SelectBuildingInScene(string buildingId)
        {
            var entries = FindObjectsOfType<Lobby.BuildingEntry>();
            foreach (var entry in entries)
            {
                if (entry.buildingId == buildingId)
                {
                    Selection.activeGameObject = entry.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                    return;
                }
            }
        }

        // === 导入/导出 ===

        private void ExportToJson()
        {
            try
            {
                var exportData = new SceneConfigExportData
                {
                    buildings = _buildings,
                    npcs = _npcs
                };

                string json = JsonUtility.ToJson(exportData, true);
                string directory = Path.GetDirectoryName(_exportPath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(_exportPath, json);
                AssetDatabase.Refresh();

                Debug.Log($"[SceneConfigTool] 配置已导出至: {_exportPath}");
                EditorUtility.DisplayDialog("导出成功", $"配置已保存至:\n{_exportPath}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", ex.Message, "确定");
            }
        }

        private void ImportFromJson()
        {
            string filePath = EditorUtility.OpenFilePanel("选择配置JSON文件", "Assets/_Project/Resources/Configs", "json");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var importData = JsonUtility.FromJson<SceneConfigExportData>(json);

                _buildings = importData.buildings ?? new List<BuildingConfigData>();
                _npcs = importData.npcs ?? new List<NPCConfigData>();

                Debug.Log($"[SceneConfigTool] 已导入: {_buildings.Count} 建筑, {_npcs.Count} NPC");
                EditorUtility.DisplayDialog("导入成功", $"已导入 {_buildings.Count} 个建筑和 {_npcs.Count} 个NPC配置", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", ex.Message, "确定");
            }
        }

        private void ValidateConfig()
        {
            List<string> errors = new List<string>();

            // 检查建筑
            foreach (var b in _buildings)
            {
                if (string.IsNullOrEmpty(b.id)) errors.Add($"建筑 [{b.name}] 缺少 ID");
                if (string.IsNullOrEmpty(b.name)) errors.Add("存在无名建筑");
                if (string.IsNullOrEmpty(b.type)) errors.Add($"建筑 [{b.name}] 缺少类型");

                // 验证场景名称
                var validScenes = new[] { "LobbyScene", "BattleScene", "KnowledgeVisualizationScene", "GuildScene", "AchievementScene", "" };
                if (!string.IsNullOrEmpty(b.targetSceneName) && Array.IndexOf(validScenes, b.targetSceneName) < 0)
                    errors.Add($"建筑 [{b.name}] 的目标场景 [{b.targetSceneName}] 不在场景列表中");
            }

            // 检查 NPC
            foreach (var npc in _npcs)
            {
                if (string.IsNullOrEmpty(npc.id)) errors.Add($"NPC [{npc.name}] 缺少 ID");
            }

            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("验证通过", "所有配置完整无误！", "确定");
            }
            else
            {
                string errorMsg = string.Join("\n", errors);
                EditorUtility.DisplayDialog("验证失败", $"发现 {errors.Count} 个问题:\n\n{errorMsg}", "确定");
                Debug.LogWarning($"[SceneConfigTool] 配置验证失败:\n{errorMsg}");
            }
        }

        [Serializable]
        private class SceneConfigExportData
        {
            public List<BuildingConfigData> buildings;
            public List<NPCConfigData> npcs;
        }
    }
}
#endif
