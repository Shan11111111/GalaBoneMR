using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BoneInfoPanel : MonoBehaviour
{
    public static BoneInfoPanel Instance { get; private set; }

    [Header("文字元件")]
    [SerializeField]
    private TMP_Text chineseName;

    [SerializeField]
    private TMP_Text englishName;

    [SerializeField]
    private TMP_Text description;

    [Header("玩家相機")]
    [SerializeField]
    private Camera xrCamera;

    [Header("資訊卡與骨頭的距離")]
    [SerializeField]
    private float distanceFromBone = 0.35f;

    [Header("資訊卡垂直偏移")]
    [SerializeField]
    private float verticalOffset = 0.15f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "場景中存在重複的 BoneInfoPanel，已刪除後建立的物件。",
                gameObject
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvasGroup =
            GetComponent<CanvasGroup>();

        if (xrCamera == null)
        {
            xrCamera = Camera.main;
        }

        Hide();
    }

    public void Show(
        GalaBoneInfo boneInfo,
        Vector3 bonePosition
    )
    {
        if (boneInfo == null)
        {
            Debug.LogError(
                "BoneInfoPanel 收到空的 GalaBoneInfo"
            );

            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        SetText(chineseName, boneInfo.chineseName);
        SetText(englishName, boneInfo.englishName);
        SetText(description, BuildDescription(boneInfo));

        transform.position =
            CalculatePanelPosition(bonePosition);

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        FaceCamera();

        Debug.Log(
            $"顯示骨骼資訊卡：{boneInfo.englishName}"
        );
    }

    private string BuildDescription(
        GalaBoneInfo boneInfo
    )
    {
        string result = string.Empty;

        if (!string.IsNullOrWhiteSpace(
                boneInfo.introduction))
        {
            result += boneInfo.introduction;
        }

        if (!string.IsNullOrWhiteSpace(
                boneInfo.structureFunction))
        {
            if (!string.IsNullOrEmpty(result))
            {
                result += "\n\n";
            }

            result +=
                $"【結構與功能】\n" +
                boneInfo.structureFunction;
        }

        if (!string.IsNullOrWhiteSpace(
                boneInfo.learning))
        {
            if (!string.IsNullOrEmpty(result))
            {
                result += "\n\n";
            }

            result +=
                $"【學習重點】\n" +
                boneInfo.learning;
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            result = "目前尚未建立此骨骼的介紹資料。";
        }

        return result;
    }

    private void SetText(
        TMP_Text targetText,
        string value
    )
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text =
            string.IsNullOrWhiteSpace(value)
                ? "尚無資料"
                : value;
    }

    private Vector3 CalculatePanelPosition(
        Vector3 bonePosition
    )
    {
        if (xrCamera == null)
        {
            xrCamera = Camera.main;
        }

        if (xrCamera == null)
        {
            return bonePosition +
                   Vector3.up * verticalOffset +
                   Vector3.right * distanceFromBone;
        }

        Vector3 cameraRight =
            xrCamera.transform.right;

        Vector3 cameraUp =
            xrCamera.transform.up;

        return bonePosition +
               cameraRight * distanceFromBone +
               cameraUp * verticalOffset;
    }

    public void Hide()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void LateUpdate()
    {
        if (canvasGroup == null ||
            canvasGroup.alpha <= 0f)
        {
            return;
        }

        FaceCamera();
    }

    private void FaceCamera()
    {
        if (xrCamera == null)
        {
            xrCamera = Camera.main;
        }

        if (xrCamera == null)
        {
            return;
        }

        Vector3 direction =
            transform.position -
            xrCamera.transform.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}