using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BoneSelectable : MonoBehaviour
{
    [SerializeField]
    private string meshName;

    public string MeshName => meshName;

    private XRBaseInteractable interactable;
    private Renderer[] boneRenderers;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly Color NormalColor =
        Color.white;

    private static readonly Color HoverColor =
        new Color(1f, 0.75f, 0.15f, 1f);

    public void Initialize(
        string value,
        XRBaseInteractable xrInteractable
    )
    {
        meshName = value;
        interactable = xrInteractable;

        boneRenderers =
            GetComponentsInChildren<Renderer>(true);

        propertyBlock =
            new MaterialPropertyBlock();

        if (interactable == null)
        {
            Debug.LogError(
                $"{gameObject.name} 找不到 XRBaseInteractable"
            );

            return;
        }

        interactable.hoverEntered.AddListener(
            OnHoverEntered
        );

        interactable.hoverExited.AddListener(
            OnHoverExited
        );

        interactable.selectEntered.AddListener(
            OnSelectEntered
        );

        interactable.selectExited.AddListener(
            OnSelectExited
        );
    }

    private void OnHoverEntered(
        HoverEnterEventArgs args
    )
    {
        SetColor(HoverColor);

        Debug.Log(
            $"Hover 骨頭：{meshName}",
            gameObject
        );
    }

    private void OnHoverExited(
        HoverExitEventArgs args
    )
    {
        if (interactable != null &&
            interactable.isSelected)
        {
            return;
        }

        SetColor(NormalColor);

        Debug.Log(
            $"離開骨頭：{meshName}",
            gameObject
        );
    }

    private void OnSelectEntered(
        SelectEnterEventArgs args
    )
    {
        SetColor(HoverColor);

        Debug.Log(
            $"抓取骨頭：{meshName}",
            gameObject
        );

        ShowBoneInformation();
    }

    private void OnSelectExited(
        SelectExitEventArgs args
    )
    {
        SetColor(NormalColor);

        Debug.Log(
            $"放開骨頭：{meshName}",
            gameObject
        );
    }

    public void Select()
    {
        Debug.Log(
            $"滑鼠選到骨頭：{meshName}",
            gameObject
        );

        ShowBoneInformation();
    }

    private void ShowBoneInformation()
    {
        if (BoneInfoPanel.Instance == null)
        {
            Debug.LogError(
                "場景中找不到 BoneInfoPanel Instance"
            );

            return;
        }

        if (BoneInfoRepository.Instance == null)
        {
            Debug.LogError(
                "場景中找不到 BoneInfoRepository"
            );

            return;
        }

        bool found =
            BoneInfoRepository.Instance.TryGet(
                meshName,
                out GalaBoneInfo boneInfo
            );

        if (!found || boneInfo == null)
        {
            Debug.LogWarning(
                $"Repository 找不到骨骼資料：{meshName}",
                gameObject
            );

            GalaBoneInfo fallbackInfo =
                new GalaBoneInfo
                {
                    meshName = meshName,
                    chineseName = "未知骨骼",
                    englishName = meshName,
                    introduction =
                        "目前找不到此骨骼的教學資料。",
                    structureFunction =
                        "尚無資料。",
                    learning =
                        "尚無資料。",
                    suggestedQuestions =
                        System.Array.Empty<string>()
                };

            BoneInfoPanel.Instance.Show(
                fallbackInfo,
                CalculatePanelPosition()
            );

            return;
        }

        Vector3 panelPosition =
            CalculatePanelPosition();

        BoneInfoPanel.Instance.Show(
            boneInfo,
            panelPosition
        );

        Debug.Log(
            $"已從 Repository 載入骨骼資料：" +
            $"{boneInfo.meshName} / " +
            $"{boneInfo.englishName}",
            gameObject
        );
    }

    private Vector3 CalculatePanelPosition()
    {
        Camera targetCamera =
            Camera.main;

        if (targetCamera == null)
        {
            return transform.position +
                   Vector3.up * 0.25f +
                   Vector3.right * 0.3f;
        }

        return targetCamera.transform.position +
               targetCamera.transform.forward * 1.2f +
               targetCamera.transform.right * 0.45f +
               targetCamera.transform.up * 0.05f;
    }

    private void SetColor(Color color)
    {
        if (boneRenderers == null ||
            propertyBlock == null)
        {
            return;
        }

        foreach (Renderer boneRenderer in
                 boneRenderers)
        {
            if (boneRenderer == null)
            {
                continue;
            }

            boneRenderer.GetPropertyBlock(
                propertyBlock
            );

            propertyBlock.SetColor(
                BaseColorId,
                color
            );

            boneRenderer.SetPropertyBlock(
                propertyBlock
            );
        }
    }

    private void OnDestroy()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.hoverEntered.RemoveListener(
            OnHoverEntered
        );

        interactable.hoverExited.RemoveListener(
            OnHoverExited
        );

        interactable.selectEntered.RemoveListener(
            OnSelectEntered
        );

        interactable.selectExited.RemoveListener(
            OnSelectExited
        );
    }
}