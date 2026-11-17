using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FarmingEngine; // <-- INI YANG HILANG

public class FindDuplicateItems : EditorWindow
{
    [MenuItem("Tools/Find Duplicate Item IDs")]
    public static void FindDuplicates()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        Dictionary<string, List<string>> idMap = new Dictionary<string, List<string>>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;

            string id = item.id; // cek nama field di ItemData.cs
            if (string.IsNullOrEmpty(id)) continue;

            if (!idMap.ContainsKey(id))
                idMap[id] = new List<string>();
            idMap[id].Add(path);
        }

        bool found = false;
        foreach (var kvp in idMap)
        {
            if (kvp.Value.Count > 1)
            {
                found = true;
                Debug.LogError($"<b>Duplikat ID '{kvp.Key}'</b> ditemukan di:");
                foreach (string p in kvp.Value)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ItemData>(p);
                    Debug.LogError($"  → {p}", asset);
                }
            }
        }

        if (!found)
            Debug.Log("Tidak ada duplikat ID. 👍");
    }
}