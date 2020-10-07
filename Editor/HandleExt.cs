using UnityEditor;
using UnityEngine;

namespace Unity.LightRelationships
{
    public class HandleExt {
        /// <summary>
        /// Implements an adjustable cone handle.
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="position"></param>
        /// <param name="angleAndRange"></param>
        /// <param name="angleScale"></param>
        /// <param name="rangeScale"></param>
        /// <param name="handlesOnly"></param>
        /// <returns></returns>
        public static Vector2 DoConeHandle(Quaternion rotation, Vector3 position, Vector2 angleAndRange, float angleScale, float rangeScale, bool handlesOnly)
        {
            float spotAngle = angleAndRange.x;
            float range = angleAndRange.y;
            float actualRange = range * rangeScale;

            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            // Range handle at the center of the circle
            bool temp = GUI.changed;
            GUI.changed = false;
            actualRange = SizeSlider(position, forward, actualRange);
            if (GUI.changed)
                range = Mathf.Max(0.0F, actualRange / rangeScale);
            GUI.changed |= temp;

            // Angle handles on circle
            temp = GUI.changed;
            GUI.changed = false;

            float lightDisc = actualRange * Mathf.Tan(Mathf.Deg2Rad * spotAngle / 2.0f) * angleScale;
            lightDisc = SizeSlider(position + forward * actualRange, up, lightDisc);
            lightDisc = SizeSlider(position + forward * actualRange, -up, lightDisc);
            lightDisc = SizeSlider(position + forward * actualRange, right, lightDisc);
            lightDisc = SizeSlider(position + forward * actualRange, -right, lightDisc);
            if (GUI.changed)
                spotAngle = Mathf.Clamp((Mathf.Rad2Deg * Mathf.Atan(lightDisc / (actualRange * angleScale)) * 2), 0.0F, 179F);
            GUI.changed |= temp;

            // Draw disc
            if (!handlesOnly)
            {
                Handles.DrawLine(position, (position + forward * actualRange) + up * lightDisc);
                Handles.DrawLine(position, (position + forward * actualRange) - up * lightDisc);
                Handles.DrawLine(position, (position + forward * actualRange) + right * lightDisc);
                Handles.DrawLine(position, (position + forward * actualRange) - right * lightDisc);
                Handles.DrawWireDisc(position + actualRange * forward, forward, lightDisc);
            }

            return new Vector2(spotAngle, range);
        }

        static internal float SizeSlider(Vector3 p, Vector3 d, float r)
        {
            Vector3 position = p + d * r;
            float size = HandleUtility.GetHandleSize(position);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(position, d, size * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
                r = Vector3.Dot(position - p, d);
            GUI.changed |= temp;
            return r;
        }
    }
}
