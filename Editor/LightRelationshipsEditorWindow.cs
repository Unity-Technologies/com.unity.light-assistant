using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.SelectionGroups;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.LightRelationships
{
    /// <summary>
    /// Editor window to visualise light relationships.
    /// </summary>
    public class LightRelationshipsEditorWindow : EditorWindow
    {

        Vector2 scroll;
        static LightRelationshipsEditorWindow editorWindow;
        List<Light> lights = new List<Light>();
        List<Renderer> renderers = new List<Renderer>();
        Dictionary<int, HashSet<Light>> lightGroups = new Dictionary<int, HashSet<Light>>();
        Dictionary<int, HashSet<Renderer>> rendererGroups = new Dictionary<int, HashSet<Renderer>>();

        [MenuItem("Window/General/Light Relationships")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<LightRelationshipsEditorWindow>();
            window.ShowUtility();
        }

        void OnEnable()
        {
            titleContent.text = "Light Relationships";
            editorWindow = this;
            RefreshGroupIndex();
        }

        void OnDisable()
        {
            editorWindow = null;
            lightGroups.Clear();
            rendererGroups.Clear();
            lights.Clear();
            renderers.Clear();
        }

        void RefreshGroupIndex()
        {
            lights.Clear();
            renderers.Clear();
            lightGroups.Clear();
            rendererGroups.Clear();
            foreach (var n in SelectionGroupManager.instance)
            {
                foreach (var obj in n)
                {
                    var g = obj as GameObject;
                    if (g == null) continue;
                    if (g.TryGetComponent<Light>(out Light light))
                        AddLight(light);
                    if (g.TryGetComponent<Renderer>(out Renderer renderer))
                        AddRenderer(renderer);
                }
            }
            foreach (var kv in lightGroups)
            {
                var cullingMask = kv.Key;
                foreach (var renderer in renderers)
                {
                    if ((1 << renderer.gameObject.layer & cullingMask) != 0)
                    {
                        if (rendererGroups.TryGetValue(cullingMask, out HashSet<Renderer> group))
                            group.Add(renderer);
                        else
                            rendererGroups.Add(cullingMask, new HashSet<Renderer>(new[] { renderer }));
                    }
                }
            }
        }

        void AddRenderer(Renderer renderer)
        {
            renderers.Add(renderer);
        }

        void AddLight(Light light)
        {
            if (lightGroups.TryGetValue(light.cullingMask, out HashSet<Light> group))
                group.Add(light);
            else
                lightGroups.Add(light.cullingMask, new HashSet<Light>(new[] { light }));
            lights.Add(light);
        }

        void OnGUI()
        {
            if (GUILayout.Button("Refresh"))
            {
                RefreshGroupIndex();
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label("Lights");
                foreach (var kv in lightGroups)
                {
                    var cullingMask = kv.Key;
                    var group = kv.Value;
                    DrawLightGroup(cullingMask, group);
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);

                }
                if (cc.changed)
                {

                }
            }
            EditorGUILayout.EndScrollView();
            if (focusedWindow == this)
                Repaint();
        }

        void DrawLightGroup(int cullingMask, HashSet<Light> group)
        {
            var layerNames = GetLayerNames(cullingMask);
            GUILayout.BeginVertical(GUIContent.none, "box");
            GUILayout.Label(GetLayerNames(cullingMask), EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Select All", "Light icon"), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                Selection.objects = group.ToArray();
            }
            foreach (var light in group)
            {
                GUILayout.Label($" - {light.name}");
            }
            GUILayout.EndVertical();
            GUILayout.Space(16);
            GUILayout.BeginVertical("box");

            if (rendererGroups.TryGetValue(cullingMask, out HashSet<Renderer> groupMembers))
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Select All", "MeshRenderer icon"), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    Selection.objects = groupMembers.ToArray();
                }
                foreach (var renderer in groupMembers)
                {
                    GUILayout.Label(renderer.gameObject.name);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        string GetLayerNames(int layerMask)
        {
            var mask = (uint)layerMask;
            if (mask == 0) return "Nothing";
            if (mask == 0xFFFFFFFF) return "Everything";
            var names = new List<string>();
            for (var i = 1; i <= 31; i++)
            {
                if ((1 << i & mask) != 0)
                    names.Add($"{LayerMask.LayerToName(i)}");
            }
            return string.Join(", ", names);
        }

    }
}
