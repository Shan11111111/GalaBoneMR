using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meta.XR;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class QuestPhotoCapture : MonoBehaviour
{
    private const string LogTag = "[QuestPhotoCapture]";

    [Header("Quest Passthrough Camera")]
    [SerializeField]
    private PassthroughCameraAccess cameraAccess;

    [Header("GalaBone API")]
    [SerializeField]
    private BoneApiClient boneApiClient;

    [Header("拍照設定")]
    [Range(1, 100)]
    [SerializeField]
    private int jpegQuality = 90;

    [Header("等待設定")]
    [SerializeField]
    private float permissionTimeout = 15f;

    [SerializeField]
    private float cameraStartTimeout = 20f;

    [Header("Debug")]
    [SerializeField]
    private bool saveDebugImage = true;

    private bool isCapturing;

    // Quest 右手 Controller
    private InputDevice rightController;

    // 避免按住 A 鍵一直狂拍
    private bool previousPrimaryButtonState;


    // ==========================================
    // 啟動初始化
    // ==========================================
    private IEnumerator Start()
    {
        Debug.Log(
            $"{LogTag} 開始初始化"
        );


        // ======================================
        // 尋找必要元件
        // ======================================
        if (cameraAccess == null)
        {
            cameraAccess =
                FindFirstObjectByType<PassthroughCameraAccess>();
        }

        if (boneApiClient == null)
        {
            boneApiClient =
                FindFirstObjectByType<BoneApiClient>();
        }

        if (cameraAccess == null)
        {
            Debug.LogError(
                $"{LogTag} 找不到 PassthroughCameraAccess"
            );

            yield break;
        }

        if (boneApiClient == null)
        {
            Debug.LogError(
                $"{LogTag} 找不到 BoneApiClient"
            );

            yield break;
        }

        Debug.Log(
            $"{LogTag} 必要元件檢查完成"
        );


        // ======================================
        // Android Camera Permission
        // ======================================
#if UNITY_ANDROID && !UNITY_EDITOR

        Debug.Log(
            $"{LogTag} 開始檢查 Android Camera 權限"
        );

        if (!Permission.HasUserAuthorizedPermission(
                Permission.Camera
            ))
        {
            Debug.LogWarning(
                $"{LogTag} 尚未取得 Camera runtime 權限，開始請求"
            );

            Permission.RequestUserPermission(
                Permission.Camera
            );

            float permissionElapsed = 0f;

            while (!Permission.HasUserAuthorizedPermission(
                       Permission.Camera
                   ))
            {
                permissionElapsed += Time.deltaTime;

                if (permissionElapsed >= permissionTimeout)
                {
                    Debug.LogError(
                        $"{LogTag} Camera runtime 權限取得失敗或等待逾時"
                    );

                    yield break;
                }

                yield return null;
            }
        }

        Debug.Log(
            $"{LogTag} Camera runtime 權限已取得"
        );

#else

        Debug.Log(
            $"{LogTag} 非 Android 實機環境，略過 Camera runtime 權限檢查"
        );

#endif


        // ======================================
        // 找右手 Controller
        // ======================================
        TryFindRightController();


        Debug.Log(
            $"{LogTag} 初始化完成"
        );

        Debug.Log(
            $"{LogTag} 請先將視線對準 X 光影像，" +
            $"再按右手控制器 A 鍵拍照辨識"
        );
    }


    // ==========================================
    // 每幀偵測 Quest Controller A 鍵
    // ==========================================
    private void Update()
    {
        // Controller 掉線或還沒找到時重新找
        if (!rightController.isValid)
        {
            TryFindRightController();

            return;
        }


        bool primaryButtonPressed = false;

        bool gotButtonValue =
            rightController.TryGetFeatureValue(
                CommonUsages.primaryButton,
                out primaryButtonPressed
            );


        if (!gotButtonValue)
        {
            return;
        }


        // 只偵測「剛按下去」的瞬間
        if (primaryButtonPressed &&
            !previousPrimaryButtonState)
        {
            Debug.Log(
                $"{LogTag} 偵測到右手 A 鍵按下"
            );


            CaptureAndPredict();
        }


        previousPrimaryButtonState =
            primaryButtonPressed;
    }


    // ==========================================
    // 尋找右手 Quest Controller
    // ==========================================
    private void TryFindRightController()
    {
        List<InputDevice> devices =
            new List<InputDevice>();


        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right |
            InputDeviceCharacteristics.Controller,
            devices
        );


        if (devices.Count == 0)
        {
            return;
        }


        rightController =
            devices[0];


        Debug.Log(
            $"{LogTag} 找到右手 Controller：" +
            $"{rightController.name}"
        );
    }


    // ==========================================
    // 手動觸發拍照
    // ==========================================
    [ContextMenu("Capture And Predict")]
    public void CaptureAndPredict()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                $"{LogTag} 請先進入 Play Mode"
            );

            return;
        }


        if (isCapturing)
        {
            Debug.LogWarning(
                $"{LogTag} 目前正在擷取影像，忽略此次按鍵"
            );

            return;
        }


        Debug.Log(
            $"{LogTag} CaptureAndPredict 被呼叫"
        );


        StartCoroutine(
            CaptureCoroutine()
        );
    }


    // ==========================================
    // 擷取 Quest Passthrough Camera
    // ==========================================
    private IEnumerator CaptureCoroutine()
    {
        isCapturing = true;


        if (cameraAccess == null)
        {
            Debug.LogError(
                $"{LogTag} PassthroughCameraAccess 尚未指定"
            );

            isCapturing = false;

            yield break;
        }


        if (boneApiClient == null)
        {
            Debug.LogError(
                $"{LogTag} BoneApiClient 尚未指定"
            );

            isCapturing = false;

            yield break;
        }


#if UNITY_ANDROID && !UNITY_EDITOR

        if (!Permission.HasUserAuthorizedPermission(
                Permission.Camera
            ))
        {
            Debug.LogError(
                $"{LogTag} 拍照時發現 Camera runtime 權限不存在"
            );

            isCapturing = false;

            yield break;
        }

#endif


        Debug.Log(
            $"{LogTag} 等待 Quest Passthrough Camera..."
        );


        float elapsed = 0f;


        while (!cameraAccess.IsPlaying)
        {
            elapsed += Time.deltaTime;


            if (elapsed >= cameraStartTimeout)
            {
                Debug.LogError(
                    $"{LogTag} 等待 Quest Camera 啟動逾時，" +
                    $"IsPlaying = {cameraAccess.IsPlaying}"
                );

                isCapturing = false;

                yield break;
            }


            yield return null;
        }


        Debug.Log(
            $"{LogTag} Camera IsPlaying = true"
        );


        // ======================================
        // 取得 Camera Texture
        // ======================================
        Texture cameraTexture =
            cameraAccess.GetTexture();


        if (cameraTexture == null)
        {
            Debug.LogError(
                $"{LogTag} PassthroughCameraAccess.GetTexture() 回傳 null"
            );

            isCapturing = false;

            yield break;
        }


        Debug.Log(
            $"{LogTag} 取得 Quest Camera 畫面：" +
            $"{cameraTexture.width} x {cameraTexture.height}"
        );


        // ======================================
        // 建立 RenderTexture
        // ======================================
        RenderTexture renderTexture =
            RenderTexture.GetTemporary(
                cameraTexture.width,
                cameraTexture.height,
                0,
                RenderTextureFormat.ARGB32
            );


        RenderTexture previous =
            RenderTexture.active;


        Texture2D snapshot = null;


        try
        {
            Debug.Log(
                $"{LogTag} 開始 Graphics.Blit"
            );


            Graphics.Blit(
                cameraTexture,
                renderTexture
            );


            RenderTexture.active =
                renderTexture;


            // ==================================
            // RenderTexture -> Texture2D
            // ==================================
            snapshot =
                new Texture2D(
                    renderTexture.width,
                    renderTexture.height,
                    TextureFormat.RGB24,
                    false
                );


            snapshot.ReadPixels(
                new Rect(
                    0,
                    0,
                    renderTexture.width,
                    renderTexture.height
                ),
                0,
                0
            );


            snapshot.Apply();


            Debug.Log(
                $"{LogTag} ReadPixels 完成"
            );


            // ==================================
            // Texture2D -> JPG
            // ==================================
            byte[] imageBytes =
                snapshot.EncodeToJPG(
                    jpegQuality
                );


            if (imageBytes == null ||
                imageBytes.Length == 0)
            {
                Debug.LogError(
                    $"{LogTag} JPG 編碼失敗"
                );

                yield break;
            }


            string filename =
                $"quest_capture_" +
                $"{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg";


            Debug.Log(
                $"{LogTag} Quest 拍照成功，" +
                $"大小：{imageBytes.Length / 1024f:F1} KB，" +
                $"檔名：{filename}"
            );


            // ==================================
            // Debug：儲存 Quest 實際圖片
            // ==================================
            if (saveDebugImage)
            {
                try
                {
                    string debugPath =
                        Path.Combine(
                            Application.persistentDataPath,
                            filename
                        );


                    File.WriteAllBytes(
                        debugPath,
                        imageBytes
                    );


                    Debug.Log(
                        $"{LogTag} DEBUG_IMAGE_PATH = {debugPath}"
                    );


                    Debug.Log(
                        $"{LogTag} Debug 圖片儲存成功"
                    );
                }
                catch (System.Exception saveException)
                {
                    Debug.LogError(
                        $"{LogTag} Debug 圖片儲存失敗：\n" +
                        $"{saveException}"
                    );
                }
            }


            // ==================================
            // 送 GalaBone API
            // ==================================
            Debug.Log(
                $"{LogTag} 準備送交 BoneApiClient"
            );


            boneApiClient.UploadImage(
                imageBytes,
                filename
            );


            Debug.Log(
                $"{LogTag} 已送交 BoneApiClient：" +
                $"{filename}"
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"{LogTag} 拍照流程發生例外：\n" +
                $"{exception}"
            );
        }
        finally
        {
            RenderTexture.active =
                previous;


            RenderTexture.ReleaseTemporary(
                renderTexture
            );


            if (snapshot != null)
            {
                Destroy(snapshot);
            }


            isCapturing = false;


            Debug.Log(
                $"{LogTag} CaptureCoroutine 結束"
            );
        }
    }
}