using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// Titik tujuan kamera untuk sebuah area (mis. kandang). Taruh di tengah kandang.
    public class CameraViewPoint : MonoBehaviour
    {
        public GroupData zone_group;         // samain dengan required_zone_group hewan (AnimalJackalope, dst)
        public Vector3 extra_camera_offset;  // zoom out/geser khusus kandang ini (relatif kamera)

        private static List<CameraViewPoint> points = new List<CameraViewPoint>();

        void OnEnable() { points.Add(this); }
        void OnDisable() { points.Remove(this); }

        public static CameraViewPoint GetByGroup(GroupData group)
        {
            if (group == null) return null;
            foreach (CameraViewPoint p in points)
                if (p.zone_group == group) return p;
            return null;
        }

        // Cuma bantuan visual di Scene view: scale objeknya bisa dipakai buat ngepasin ukuran kandang
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}