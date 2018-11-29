﻿using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace ThreeDISevenZeroR.SensorKit
{
    /// <summary>
    /// Utility methods which used in classes derived from PhysicsSensor
    /// </summary>
    public static class PhysicsSensorUtils
    {
        public static Color noHitColor = new Color(1f, 0.5f, 0.5f, 0.5f);
        public static Color rayEndColor = new Color(1f, 1f, 0.5f, 0.5f);
        public static Color hasHitColor = new Color(0.5f, 1f, 0.5f, 0.5f);
        public static Color normalColor = new Color(0.25f, 0.75f, 1f);
        public static Color hitPositionColor = new Color(1f, 0.5f, 1f, 0.5f);
        public static Color triangleColor = new Color(1, 1, 1, 0.5f);

        public static float GetCastDistance(float distance, Vector3 scale)
        {
            return Mathf.Abs(distance * scale.z);
        }

        public static float GetScaledCapsuleRadius(float radius, Vector3 scale)
        {
            return Mathf.Abs(radius * (scale.y < scale.z ? scale.z : scale.y));
        }

        public static float GetScaledSphereRadius(float radius, Vector3 scale)
        {
            return Mathf.Abs(radius * (scale.y < scale.x
                                 ? scale.x < scale.z ? scale.z : scale.x
                                 : scale.y < scale.z
                                     ? scale.z
                                     : scale.y));
        }

        public static Vector3 GetScaledBoxRadius(Vector3 extents, Vector3 scale)
        {
            return new Vector3(extents.x * Mathf.Abs(scale.x),
                extents.y * Mathf.Abs(scale.y),
                extents.z * Mathf.Abs(scale.z));
        }

        public static void GetCapsulePoints(Vector3 origin, Quaternion rotation, float width, float xScale,
            out Vector3 p1, out Vector3 p2)
        {
            var offset = rotation * new Vector3(Mathf.Abs(width * xScale), 0);
            p1 = origin + offset;
            p2 = origin - offset;
        }

#if UNITY_EDITOR

        private static readonly List<Vector3> sharedVertices = new List<Vector3>(8192);
        private static readonly List<int> sharedIndices = new List<int>(8192);
        private static readonly List<int> sharedIndicesTemp = new List<int>(8192);

        public static void DrawCollisionPoints(Vector3 collisionPoint, RaycastHit hit)
        {
            Gizmos.color = hasHitColor;
            Gizmos.DrawSphere(collisionPoint, 0.025f);

            Gizmos.color = hitPositionColor;
            Gizmos.DrawLine(collisionPoint, hit.point);
            Gizmos.DrawSphere(hit.point, 0.025f);
        }

        public static void DrawNormal(RaycastHit hit)
        {
            Gizmos.color = normalColor;
            Handles.color = normalColor;
            var normalEnd = hit.point + hit.normal * 0.5f;
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
            Handles.ConeHandleCap(0, normalEnd, Quaternion.LookRotation(hit.normal), 0.1f, Event.current.type);
        }

        public static void HighlightMeshVertices(RaycastHit hit)
        {
            var meshCollider = hit.collider as MeshCollider;
            if (meshCollider != null)
            {
                var mesh = meshCollider.sharedMesh;
                mesh.GetVertices(sharedVertices);
                sharedIndices.Clear();

                for (var i = 0; i < mesh.subMeshCount; i++)
                {
                    mesh.GetTriangles(sharedIndicesTemp, i, true);
                    sharedIndices.AddRange(sharedIndicesTemp);
                }

                var triangleStart = hit.triangleIndex * 3;
                var normalOffset = hit.normal * 0.001f;
                var v0 = meshCollider.transform.TransformPoint(sharedVertices[sharedIndices[triangleStart]]) +
                         normalOffset;
                var v1 = meshCollider.transform.TransformPoint(sharedVertices[sharedIndices[triangleStart + 1]]) +
                         normalOffset;
                var v2 = meshCollider.transform.TransformPoint(sharedVertices[sharedIndices[triangleStart + 2]]) +
                         normalOffset;

                Gizmos.color = triangleColor;
                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }

        public static void DrawHitInfo(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return;
            }

            var sceneCamera = SceneView.currentDrawingSceneView.camera;
            var offset = sceneCamera.WorldToScreenPoint(hit.point);

            if (offset.z > 0)
            {
                Handles.BeginGUI();
                offset.x = offset.x - 64;
                offset.y = -offset.y + sceneCamera.pixelHeight + 16;
                GUI.Box(new Rect(offset.x, offset.y, 140, 56), GUIContent.none);
                GUI.Label(new Rect(offset.x + 8, offset.y + 8, 132, 16), hit.collider.gameObject.name);
                GUI.Label(new Rect(offset.x + 8, offset.y + 20, 132, 16), "distance: " + hit.distance);
                GUI.Label(new Rect(offset.x + 8, offset.y + 32, 132, 16), "triangleIndex: " + hit.triangleIndex);
                Handles.EndGUI();
            }
        }

        public static void DrawCapsuleGizmo(Vector3 position, Quaternion rotation, Vector3 scale, float width,
            float radius)
        {
            Vector3 p1;
            Vector3 p2;

            GetCapsulePoints(position, rotation, width, scale.x, out p1, out p2);
            var castRadius = GetScaledCapsuleRadius(radius, scale);

            DrawSphereGizmo(p1, rotation, Vector3.one, castRadius);
            DrawSphereGizmo(p2, rotation, Vector3.one, castRadius);

            Gizmos.DrawLine(p1 + rotation * Vector3.up * castRadius, p2 + rotation * Vector3.up * castRadius);
            Gizmos.DrawLine(p1 + rotation * Vector3.down * castRadius, p2 + rotation * Vector3.down * castRadius);
            Gizmos.DrawLine(p1 + rotation * Vector3.forward * castRadius, p2 + rotation * Vector3.forward * castRadius);
            Gizmos.DrawLine(p1 + rotation * Vector3.back * castRadius, p2 + rotation * Vector3.back * castRadius);
        }

        public static void DrawSphereGizmo(Vector3 position, Quaternion rotation, Vector3 scale, float radius)
        {
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Gizmos.DrawWireSphere(Vector3.zero, GetScaledSphereRadius(radius, scale));
            Gizmos.matrix = Matrix4x4.identity;
        }

        public static void DrawBoxGizmo(Vector3 position, Quaternion rotation, Vector3 scale, Vector3 halfExtents)
        {
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, scale);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}