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
    public class LightAssistantWindow : EditorWindow
    {
        static Color SCENE_SELECTION_COLOR = new Color(1, 1, 0, 0.15f);
        static Color GUI_SELECTION_COLOR = new Color(1, 1, 0, 1);
        static Color kGizmoLight = new Color(254 / 255f, 253 / 255f, 136 / 255f, 128 / 255f);
        static Color kGizmoDisabledLight = new Color(135 / 255f, 116 / 255f, 50 / 255f, 128 / 255f);


        Vector2 scroll;
        static LightAssistantWindow editorWindow;
        List<Light> lights = new List<Light>();
        List<SerializedObject> lightObjects = new List<SerializedObject>();
        Dictionary<int, List<GameObject>> otherObjects = new Dictionary<int, List<GameObject>>();

        int hotLightIndex = -1;
        int lastHotLightIndex = -1;

        [MenuItem("Window/General/Light Assistant")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<LightAssistantWindow>();
            window.ShowUtility();
        }

        void OnEnable()
        {
            titleContent.text = "Light Assistant";
            editorWindow = this;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            OnSelectionChange();
        }

        void OnDisable()
        {
            editorWindow = null;
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.update -= OnUpdate;
        }

        void OnUpdate()
        {
            if (lastHotLightIndex != hotLightIndex)
            {
                lastHotLightIndex = hotLightIndex;
                if (hotLightIndex >= 0)
                {
                    EditorGUIUtility.PingObject(lights[hotLightIndex].gameObject);
                }
                Repaint();
            }
        }

        void DuringSceneGUI(SceneView sceneView)
        {
            var center = Selection.activeTransform;
            if (center != null)
                for (var i = 0; i < lights.Count; i++)
                {
                    EditorGUI.BeginChangeCheck();
                    var light = lights[i];
                    var lightPosition = light.transform.position;
                    var rotation = light.transform.rotation;
                    var range = light.range;
                    var angleAndRange = new Vector2(light.spotAngle, light.range);

                    if (light.type == LightType.Point)
                    {
                        DrawPointLightGUI(light, ref lightPosition, rotation, ref range);
                    }

                    if (light.type == LightType.Spot)
                    {
                        Handles.TransformHandle(ref lightPosition, ref rotation);
                        Handles.color = light.enabled ? kGizmoDisabledLight : kGizmoDisabledLight;
                        angleAndRange = HandleExt.DoConeHandle(rotation, lightPosition, angleAndRange, 1.0f, 1.0f, false);
                    }

                    if (light.type == LightType.Directional)
                    {
                        DrawDirectionalLightGUI(light, ref lightPosition, ref rotation);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(light.transform, "Modify Light");
                        light.transform.position = lightPosition;
                        light.transform.rotation = rotation;
                        if (light.type == LightType.Spot)
                        {
                            light.spotAngle = angleAndRange.x;
                            light.range = Mathf.Max(0.01f, angleAndRange.y);
                        }
                        else
                            light.range = Mathf.Max(0.01f, range);
                        hotLightIndex = i;
                        lightObjects[i].Update();
                        Repaint();
                    }

                    if (hotLightIndex == i)
                    {
                        Handles.color = SCENE_SELECTION_COLOR;
                        Handles.DrawSolidDisc(light.transform.position, sceneView.camera.transform.forward, HandleUtility.GetHandleSize(light.transform.position));
                    }

                    foreach (var otherObject in otherObjects[i])
                    {
                        var pos = otherObject.transform.position;
                        var isOutOfRange = IsOutOfRange(light, pos);
                        Handles.color = light.color;
                        if (isOutOfRange)
                            Handles.DrawDottedLine(pos, lightPosition, 5);
                        else
                            Handles.DrawAAPolyLine(1, pos, lightPosition);
                    }
                }
        }

        bool IsOutOfRange(Light light, Vector3 position)
        {
            switch (light.type)
            {
                case LightType.Point:
                    return Vector3.Distance(light.transform.position, position) > light.range;
                case LightType.Spot:
                    return Vector3.Distance(light.transform.position, position) > light.range
                    ||
                    Vector3.Angle(Vector3.forward, light.transform.InverseTransformPoint(position).normalized) > light.spotAngle * 0.5f;
                default:
                    return false;
            }

        }

        static void DrawDirectionalLightGUI(Light light, ref Vector3 position, ref Quaternion rotation)
        {
            Handles.TransformHandle(ref position, ref rotation);
            Handles.color = light.enabled ? kGizmoDisabledLight : kGizmoDisabledLight; ;
            var point = new Vector3(0, 1, 0);
            var handleScale = HandleUtility.GetHandleSize(light.transform.position);
            for (var x = 0; x < 8; x++)
            {
                var p = light.transform.TransformPoint(Quaternion.Euler(0, 0, x * (360f / 8)) * Vector3.up * handleScale * 0.25f);
                Handles.DrawLine(p, p + light.transform.forward * handleScale);
            }
        }

        static void DrawPointLightGUI(Light light, ref Vector3 position, Quaternion rotation, ref float range)
        {
            Handles.color = light.enabled ? kGizmoDisabledLight : kGizmoDisabledLight; ;
            position = Handles.PositionHandle(position, Quaternion.identity);
            range = Handles.RadiusHandle(rotation, position, range, handlesOnly: false);
        }

        void OnSelectionChange()
        {
            var renderers = (from i in Selection.gameObjects select i.GetComponent<Renderer>());
            var layerMask = 0;
            foreach (var i in renderers)
            {
                if (i != null)
                {
                    layerMask |= 1 << i.gameObject.layer;
                }
            }
            lights.Clear();
            lights.AddRange(from i in GameObject.FindObjectsOfType<Light>() where (i.cullingMask & layerMask) != 0 select i);
            lightObjects.Clear();
            foreach (var i in lights)
            {
                lightObjects.Add(new SerializedObject(i));
            }
            hotLightIndex = -1;
            otherObjects.Clear();

            var allRenderers = GameObject.FindObjectsOfType<Renderer>();

            for (var i = 0; i < lights.Count; i++)
            {
                var lightList = otherObjects[i] = new List<GameObject>();
                for (var j = 0; j < allRenderers.Length; j++)
                {
                    var o = allRenderers[j];
                    if ((1 << o.gameObject.layer & lights[i].cullingMask) != 0)
                    {
                        lightList.Add(o.gameObject);
                    }
                }
            }
            Repaint();
        }


        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Lights");
            for (var i = 0; i < lightObjects.Count; i++)
            {
                var editor = lightObjects[i];
                if (editor != null)
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(editor.targetObject.name, EditorStyles.boldLabel))
                    {
                        hotLightIndex = i;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("A"))
                    {
                        SceneView.lastActiveSceneView.AlignViewToObject(lights[i].transform);
                    }
                    if (GUILayout.Button("B"))
                    {
                        var cam = SceneView.lastActiveSceneView.camera.transform;
                        lights[i].transform.rotation = cam.rotation;
                        lights[i].transform.position = cam.position + cam.forward * 1.5f;
                    }
                    GUILayout.EndHorizontal();
                    DrawLightEditor(i);

                    GUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        editor.ApplyModifiedProperties();
                        hotLightIndex = i;
                    }
                    if (hotLightIndex == i)
                    {
                        Handles.DrawSolidRectangleWithOutline(GUILayoutUtility.GetLastRect(), Color.clear, GUI_SELECTION_COLOR);
                    }
                    GUILayout.Space(8);

                }
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawLightEditor(int index)
        {
            var so = lightObjects[index];
            var typeProperty = so.FindProperty("m_Type");
            EditorGUILayout.PropertyField(typeProperty);
            EditorGUILayout.PropertyField(so.FindProperty("m_Color"));
            EditorGUILayout.PropertyField(so.FindProperty("m_RenderMode"));
            EditorGUILayout.PropertyField(so.FindProperty("m_Intensity"));
            var typeValue = (LightType)System.Enum.Parse(typeof(LightType), typeProperty.enumNames[typeProperty.enumValueIndex]);
            if (typeValue == LightType.Point || typeValue == LightType.Spot)
            {
                var rangeProperty = so.FindProperty("m_Range");
                EditorGUILayout.PropertyField(rangeProperty);

                if (Selection.activeTransform != null && IsOutOfRange(lights[index], Selection.activeTransform.position))
                {
                    EditorGUILayout.HelpBox("Out of Range", MessageType.Warning);
                }

            }
            if (typeValue == LightType.Spot)
                EditorGUILayout.PropertyField(so.FindProperty("m_SpotAngle"));
        }
    }
}
