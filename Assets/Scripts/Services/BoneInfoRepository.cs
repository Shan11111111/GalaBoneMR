using System;
using System.Collections.Generic;
using UnityEngine;

public class BoneInfoRepository : MonoBehaviour
{
    public static BoneInfoRepository Instance
    {
        get;
        private set;
    }

    private readonly Dictionary<string, GalaBoneInfo>
        boneInfoByMeshName =
            new Dictionary<string, GalaBoneInfo>(
                StringComparer.OrdinalIgnoreCase
            );

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Clear()
    {
        boneInfoByMeshName.Clear();

        Debug.Log("已清除上一輪骨骼教學資料");
    }

    public void Register(
        GalaBoneInfo boneInfo
    )
    {
        if (boneInfo == null)
        {
            Debug.LogWarning(
                "無法登錄空的 GalaBoneInfo"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(
                boneInfo.meshName))
        {
            Debug.LogWarning(
                "無法登錄沒有 MeshName 的骨骼資料"
            );
            return;
        }

        string key =
            boneInfo.meshName.Trim();

        boneInfoByMeshName[key] =
            boneInfo;

        Debug.Log(
            $"已登錄骨骼資料：{key} / " +
            $"{boneInfo.chineseName} / " +
            $"{boneInfo.englishName}"
        );
    }

    public bool TryGet(
        string meshName,
        out GalaBoneInfo boneInfo
    )
    {
        boneInfo = null;

        if (string.IsNullOrWhiteSpace(meshName))
        {
            return false;
        }

        return boneInfoByMeshName.TryGetValue(
            meshName.Trim(),
            out boneInfo
        );
    }

    public int Count =>
        boneInfoByMeshName.Count;
}