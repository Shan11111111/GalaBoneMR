using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BoneSpawner : MonoBehaviour
{
    [Header("骨骼查找器")]
    [SerializeField]
    private BoneMeshLookup boneMeshLookup;

    [Header("生成位置")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("每塊骨頭的目標大小（公尺）")]
    [SerializeField]
    private float targetBoneSize = 0.3f;

    [Header("多個骨頭之間的間距")]
    [SerializeField]
    private float spacing = 0.4f;

    [Header("測試用 Mesh 名稱")]
    [SerializeField]
    private string testMeshName = "Scaphoid.L";

    private GameObject currentGroup;

    private void Start()
    {
        SpawnTestBone();
    }

    [ContextMenu("Spawn Test Bone")]
    public void SpawnTestBone()
    {
        SpawnBoneGroup(
            new List<string>
            {
                testMeshName
            }
        );
    }

    public void SpawnBoneGroup(List<string> meshNames)
    {
        ClearSpawnedBones();

        if (boneMeshLookup == null)
        {
            Debug.LogError("尚未指定 BoneMeshLookup");
            return;
        }

        if (meshNames == null || meshNames.Count == 0)
        {
            Debug.LogWarning("沒有可生成的 MeshName");
            return;
        }

        Transform target =
            spawnPoint != null
                ? spawnPoint
                : transform;

        currentGroup =
            new GameObject("SpawnedBoneGroup");

        currentGroup.transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );

        int validCount = 0;

        foreach (string meshName in meshNames)
        {
            if (string.IsNullOrWhiteSpace(meshName))
            {
                continue;
            }

            Transform source =
                boneMeshLookup.Find(meshName);

            if (source == null)
            {
                Debug.LogError(
                    $"找不到骨頭：{meshName}"
                );

                continue;
            }

            GameObject wrapper =
                new GameObject(
                    $"BoneWrapper_{meshName}"
                );

            wrapper.transform.SetParent(
                currentGroup.transform,
                false
            );

            GameObject clone = Instantiate(
                source.gameObject,
                wrapper.transform
            );

            clone.name =
                $"Spawned_{meshName}";

            clone.SetActive(true);

            EnableRenderers(clone);

            clone.transform.localPosition =
                Vector3.zero;

            clone.transform.localRotation =
                source.localRotation;

            clone.transform.localScale =
                source.localScale;

            NormalizeAndCenterBone(
                clone,
                wrapper.transform
            );

            SetupInteraction(
                clone,
                wrapper,
                meshName
            );

            validCount++;
        }

        if (validCount == 0)
        {
            Destroy(currentGroup);
            currentGroup = null;
            return;
        }

        ArrangeBoneWrappers();

        Debug.Log(
            $"骨骼群組生成完成，共 {validCount} 個物件"
        );
    }

    private void EnableRenderers(
        GameObject target
    )
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(
                true
            );

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = true;
            }
        }
    }

    private void NormalizeAndCenterBone(
        GameObject clone,
        Transform wrapper
    )
    {
        Renderer[] renderers =
            clone.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                $"{clone.name} 找不到 Renderer"
            );

            return;
        }

        Bounds bounds =
            CalculateBounds(renderers);

        float largestSize =
            Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            );

        if (largestSize > 0f)
        {
            float scaleFactor =
                targetBoneSize / largestSize;

            clone.transform.localScale *=
                scaleFactor;
        }

        renderers =
            clone.GetComponentsInChildren<Renderer>(
                true
            );

        bounds =
            CalculateBounds(renderers);

        Vector3 offset =
            wrapper.position - bounds.center;

        clone.transform.position += offset;
    }

    private void ArrangeBoneWrappers()
    {
        int count =
            currentGroup.transform.childCount;

        float centerOffset =
            (count - 1) * 0.5f;

        for (
            int index = 0;
            index < count;
            index++
        )
        {
            Transform wrapper =
                currentGroup.transform.GetChild(
                    index
                );

            float x =
                (index - centerOffset) * spacing;

            wrapper.localPosition =
                new Vector3(
                    x,
                    0f,
                    0f
                );
        }
    }

    private void SetupInteraction(
        GameObject clone,
        GameObject wrapper,
        string meshName
    )
    {
        BoxCollider boneCollider =
            AddBoxCollider(wrapper, clone);

        if (boneCollider == null)
        {
            Debug.LogError(
                $"{wrapper.name} 無法建立抓取互動：Collider 建立失敗"
            );

            return;
        }

        Rigidbody rigidbody =
            wrapper.AddComponent<Rigidbody>();

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;

        rigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        rigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        XRGrabInteractable grabInteractable =
            wrapper.AddComponent<XRGrabInteractable>();

        grabInteractable.movementType =
            XRBaseInteractable.MovementType.Kinematic;

        grabInteractable.colliders.Clear();
        grabInteractable.colliders.Add(
            boneCollider
        );

        BoneSelectable selectable =
            wrapper.AddComponent<BoneSelectable>();

        selectable.Initialize(
            meshName,
            grabInteractable
        );
    }

    private BoxCollider AddBoxCollider(
        GameObject wrapper,
        GameObject visualObject
    )
    {
        Renderer[] renderers =
            visualObject.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                $"{visualObject.name} 無法建立 Collider：找不到 Renderer"
            );

            return null;
        }

        Bounds worldBounds =
            CalculateBounds(renderers);

        BoxCollider collider =
            wrapper.AddComponent<BoxCollider>();

        collider.center =
            wrapper.transform.InverseTransformPoint(
                worldBounds.center
            );

        Vector3 localSize =
            wrapper.transform.InverseTransformVector(
                worldBounds.size
            );

        collider.size =
            new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z)
            );

        collider.isTrigger = false;

        return collider;
    }

    private Bounds CalculateBounds(
        Renderer[] renderers
    )
    {
        Bounds bounds =
            renderers[0].bounds;

        for (
            int index = 1;
            index < renderers.Length;
            index++
        )
        {
            bounds.Encapsulate(
                renderers[index].bounds
            );
        }

        return bounds;
    }

    [ContextMenu("Clear Spawned Bones")]
    public void ClearSpawnedBones()
    {
        if (currentGroup == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentGroup);
        }
        else
        {
            DestroyImmediate(currentGroup);
        }

        currentGroup = null;
    }
}