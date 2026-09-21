using System.Collections.Generic;
using UnityEngine;

public class BoneMeshLookup : MonoBehaviour
{
    private readonly Dictionary<string, Transform> meshes =
        new Dictionary<string, Transform>();

    private void Awake()
    {
        BuildLookup();

        TestFind("C1");
        TestFind("Thumb_Proximal.L");
        TestFind("Scaphoid.L");
    }

    private void BuildLookup()
    {
        meshes.Clear();

        Transform[] allTransforms =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform target in allTransforms)
        {
            // 排除掛載腳本的 HumanModel 根物件
            if (target == transform)
                continue;

            if (meshes.ContainsKey(target.name))
            {
                Debug.LogWarning(
                    $"發現重複物件名稱：{target.name}",
                    target
                );
                continue;
            }

            meshes.Add(target.name, target);
        }

        Debug.Log($"Loaded {meshes.Count} bone objects");
    }

    public Transform Find(string meshName)
    {
        Debug.Log($"查詢：{meshName}");
        Debug.Log($"Dictionary 數量：{meshes.Count}");

        if (meshes.TryGetValue(meshName, out Transform target))
        {
            Debug.Log($"成功找到：{target.name}");
            return target;
        }

        Debug.LogWarning("Dictionary 裡沒有這個名稱，列出前20個：");

        int i = 0;
        foreach (var key in meshes.Keys)
        {
            Debug.Log(key);

            i++;
            if (i >= 20)
                break;
        }

        return null;
    }

    private void TestFind(string meshName)
    {
        Transform result = Find(meshName);

        if (result != null)
        {
            Debug.Log($"找到 Mesh：{meshName}", result);
        }
        else
        {
            Debug.LogError($"找不到 Mesh：{meshName}");
        }
    }
}