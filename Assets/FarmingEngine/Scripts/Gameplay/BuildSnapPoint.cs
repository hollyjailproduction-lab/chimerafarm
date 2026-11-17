using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// Titik tempel untuk ghost Buildable (mis. sarang inkubator).
    /// Pasang di ROOT objek sarang yang punya collider.
    public class BuildSnapPoint : MonoBehaviour
    {
        public GroupData snap_group;       // samakan dengan Buildable.snap_group prefab telur
        public Vector3 local_offset;       // posisi telur relatif ke sarang (mis. 0, 0.2, 0)
        public float snap_range = 1.5f;    // seberapa dekat kursor supaya ghost menempel

        private static List<BuildSnapPoint> points = new List<BuildSnapPoint>();

        void OnEnable() { points.Add(this); }
        void OnDisable() { points.Remove(this); }

        public Vector3 GetPoint() { return transform.TransformPoint(local_offset); }

        public static BuildSnapPoint FindNearest(GroupData group, Vector3 pos)
        {
            BuildSnapPoint best = null;
            float best_dist = float.MaxValue;
            foreach (BuildSnapPoint p in points)
            {
                if (p.snap_group != group) continue;
                Vector3 d = p.GetPoint() - pos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= p.snap_range && dist < best_dist)
                {
                    best = p;
                    best_dist = dist;
                }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetPoint(), 0.25f);
        }
    }
}