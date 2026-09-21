using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class BoneApiClient : MonoBehaviour
{
    private const string LogTag = "[BoneApiClient]";

    [Header("FastAPI")]
    [SerializeField]
    private string predictUrl =
        "http://140.136.155.157:8000/predict?include_mr_data=true";

    [Header("Editor 測試圖片完整路徑")]
    [SerializeField]
    private string testImagePath =
        @"C:\Test\testIMG.jpg";

    [Header("骨骼生成器")]
    [SerializeField]
    private BoneSpawner boneSpawner;

    [Header("上傳設定")]
    [SerializeField]
    private int timeoutSeconds = 120;


    public PredictResponse LastResponse { get; private set; }

    public bool IsUploading { get; private set; }


    public event Action UploadStarted;

    public event Action<PredictResponse> UploadSucceeded;

    public event Action<string> UploadFailed;


    // =========================================================
    // Inspector 測試：讀取 Windows 本機圖片
    // =========================================================
    [ContextMenu("Upload Test Image")]
    public void UploadTestImage()
    {
        PredictFromFile(testImagePath);
    }


    // =========================================================
    // 從圖片路徑進行辨識
    // Editor / Windows 測試使用
    // =========================================================
    public void PredictFromFile(string imagePath)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                $"{LogTag} 請先進入 Play Mode，再上傳圖片"
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(imagePath))
        {
            HandleUploadFailure(
                "圖片路徑是空的"
            );

            return;
        }


        if (!File.Exists(imagePath))
        {
            HandleUploadFailure(
                $"找不到測試圖片：{imagePath}"
            );

            return;
        }


        try
        {
            byte[] imageBytes =
                File.ReadAllBytes(imagePath);

            string filename =
                Path.GetFileName(imagePath);


            PredictFromBytes(
                imageBytes,
                filename
            );
        }
        catch (Exception exception)
        {
            HandleUploadFailure(
                $"讀取圖片失敗：{exception.Message}"
            );
        }
    }


    // =========================================================
    // 從 byte[] 進行辨識
    // Quest 相機拍照後呼叫這裡
    // =========================================================
    public void PredictFromBytes(
        byte[] imageBytes,
        string filename = "captured_xray.jpg"
    )
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                $"{LogTag} 請先進入 Play Mode，再上傳圖片"
            );

            return;
        }


        if (IsUploading)
        {
            Debug.LogWarning(
                $"{LogTag} 目前已有圖片正在辨識，請稍候"
            );

            return;
        }


        if (imageBytes == null ||
            imageBytes.Length == 0)
        {
            HandleUploadFailure(
                "影像資料是空的"
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(filename))
        {
            filename =
                "captured_xray.jpg";
        }


        Debug.Log(
            $"{LogTag} PredictFromBytes：" +
            $"filename={filename}, " +
            $"size={imageBytes.Length} bytes"
        );


        StartCoroutine(
            UploadImageCoroutine(
                imageBytes,
                filename
            )
        );
    }


    // =========================================================
    // 保留舊名稱
    // =========================================================
    public void UploadImage(
        byte[] imageBytes,
        string filename
    )
    {
        Debug.Log(
            $"{LogTag} UploadImage 被呼叫：" +
            $"{filename}"
        );


        PredictFromBytes(
            imageBytes,
            filename
        );
    }


    // =========================================================
    // 上傳影像到 FastAPI
    // =========================================================
    private IEnumerator UploadImageCoroutine(
        byte[] imageBytes,
        string filename
    )
    {
        BeginUpload();


        List<IMultipartFormSection> formSections =
            new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(
                    "file",
                    imageBytes,
                    filename,
                    GetContentType(filename)
                )
            };


        using UnityWebRequest request =
            UnityWebRequest.Post(
                predictUrl,
                formSections
            );


        request.timeout =
            Mathf.Max(
                1,
                timeoutSeconds
            );


        Debug.Log(
            $"{LogTag} 開始上傳影像"
        );

        Debug.Log(
            $"{LogTag} URL = {predictUrl}"
        );

        Debug.Log(
            $"{LogTag} filename = {filename}"
        );

        Debug.Log(
            $"{LogTag} size = {imageBytes.Length} bytes"
        );


        yield return request.SendWebRequest();


        Debug.Log(
            $"{LogTag} HTTP request completed"
        );

        Debug.Log(
            $"{LogTag} HTTP Status = {request.responseCode}"
        );


        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            string errorMessage =
                BuildRequestErrorMessage(
                    request
                );


            HandleUploadFailure(
                errorMessage
            );


            yield break;
        }


        string json =
            request.downloadHandler?.text;


        Debug.Log(
            $"{LogTag} RAW JSON = {json}"
        );


        if (string.IsNullOrWhiteSpace(json))
        {
            HandleUploadFailure(
                "API 回傳內容是空的"
            );

            yield break;
        }


        try
        {
            PredictResponse response =
                JsonConvert.DeserializeObject<PredictResponse>(
                    json
                );


            if (response == null)
            {
                HandleUploadFailure(
                    "API 回傳解析後是 null"
                );

                yield break;
            }


            Debug.Log(
                $"{LogTag} JSON 解析成功"
            );


            Debug.Log(
                $"{LogTag} image_case_id = " +
                $"{response.image_case_id}"
            );


            Debug.Log(
                $"{LogTag} count = " +
                $"{response.count}"
            );


            Debug.Log(
                $"{LogTag} boxes count = " +
                $"{response.boxes?.Count ?? 0}"
            );


            Debug.Log(
                $"{LogTag} bone_groups count = " +
                $"{response.bone_groups?.Count ?? 0}"
            );


            LastResponse =
                response;


            HandlePredictResponse(
                response
            );


            EndUploadSuccess(
                response
            );
        }
        catch (JsonException exception)
        {
            HandleUploadFailure(
                $"JSON 解析失敗：{exception.Message}\n" +
                $"原始回傳：{json}"
            );
        }
        catch (Exception exception)
        {
            HandleUploadFailure(
                $"處理辨識結果時發生錯誤：\n" +
                $"{exception}"
            );
        }
    }


    // =========================================================
    // 開始辨識前清除上一輪結果
    // =========================================================
    private void BeginUpload()
    {
        IsUploading =
            true;

        LastResponse =
            null;


        if (BoneInfoPanel.Instance != null)
        {
            BoneInfoPanel.Instance.Hide();
        }


        if (boneSpawner != null)
        {
            boneSpawner.ClearSpawnedBones();
        }


        UploadStarted?.Invoke();


        Debug.Log(
            $"{LogTag} 開始新的影像辨識流程"
        );
    }


    // =========================================================
    // 處理辨識結果
    // =========================================================
    private void HandlePredictResponse(
        PredictResponse response
    )
    {
        Debug.Log(
            $"{LogTag} HandlePredictResponse START"
        );


        Debug.Log(
            $"{LogTag} 辨識成功，案件 ID：" +
            $"{response.image_case_id}，" +
            $"共 {response.count} 個框"
        );


        Debug.Log(
            $"{LogTag} boxes count = " +
            $"{response.boxes?.Count ?? 0}"
        );


        Debug.Log(
            $"{LogTag} bone_groups count = " +
            $"{response.bone_groups?.Count ?? 0}"
        );


        PrintDetectionResults(
            response
        );


        Debug.Log(
            $"{LogTag} 開始 RegisterBoneInformation"
        );


        RegisterBoneInformation(
            response
        );


        Debug.Log(
            $"{LogTag} RegisterBoneInformation 完成"
        );


        List<string> meshNames =
            CollectMeshNames(
                response
            );


        Debug.Log(
            $"{LogTag} CollectMeshNames 結果：" +
            $"{meshNames.Count}"
        );


        foreach (string meshName in meshNames)
        {
            Debug.Log(
                $"{LogTag} 準備生成 Mesh = " +
                $"{meshName}"
            );
        }


        if (boneSpawner == null)
        {
            Debug.LogError(
                $"{LogTag} BoneSpawner = NULL"
            );


            throw new InvalidOperationException(
                "BoneApiClient 尚未指定 BoneSpawner"
            );
        }


        if (meshNames.Count == 0)
        {
            Debug.LogWarning(
                $"{LogTag} API 有回傳結果，" +
                $"但沒有可生成的 Mesh"
            );


            boneSpawner.ClearSpawnedBones();


            return;
        }


        Debug.Log(
            $"{LogTag} 呼叫 SpawnBoneGroup"
        );


        boneSpawner.SpawnBoneGroup(
            meshNames
        );


        Debug.Log(
            $"{LogTag} 已送出 " +
            $"{meshNames.Count} 個唯一 MeshName " +
            $"給 BoneSpawner"
        );
    }


    // =========================================================
    // 輸出辨識框資訊
    // =========================================================
    private void PrintDetectionResults(
        PredictResponse response
    )
    {
        if (response.boxes == null ||
            response.boxes.Count == 0)
        {
            Debug.LogWarning(
                $"{LogTag} API 沒有回傳辨識框"
            );


            return;
        }


        Debug.Log(
            $"{LogTag} 開始列印辨識結果"
        );


        foreach (DetectionBox box in
                 response.boxes)
        {
            if (box == null)
            {
                continue;
            }


            string boneZh =
                box.bone_info != null
                    ? box.bone_info.bone_zh
                    : "無對應資料";


            Debug.Log(
                $"{LogTag} 辨識：" +
                $"{box.cls_name}，" +
                $"中文：{boneZh}，" +
                $"信心值：{box.conf:F3}"
            );
        }
    }


    // =========================================================
    // 將 API 骨骼資訊註冊到 Repository
    // =========================================================
    private void RegisterBoneInformation(
        PredictResponse response
    )
    {
        if (BoneInfoRepository.Instance == null)
        {
            Debug.LogError(
                $"{LogTag} 找不到 BoneInfoRepository"
            );


            return;
        }


        BoneInfoRepository.Instance.Clear();


        if (response.bone_groups == null)
        {
            Debug.LogWarning(
                $"{LogTag} bone_groups = null，" +
                $"無法註冊骨骼資料"
            );


            return;
        }


        Debug.Log(
            $"{LogTag} 準備註冊 " +
            $"{response.bone_groups.Count} 個 bone group"
        );


        foreach (BoneGroup group in
                 response.bone_groups)
        {
            if (group == null)
            {
                continue;
            }


            Debug.Log(
                $"{LogTag} Register group：" +
                $"{group.bone_zh}"
            );


            if (group.small_bones == null)
            {
                Debug.LogWarning(
                    $"{LogTag} group.small_bones = null"
                );


                continue;
            }


            foreach (SmallBone smallBone in
                     group.small_bones)
            {
                if (smallBone == null)
                {
                    continue;
                }


                Debug.Log(
                    $"{LogTag} Register small bone：" +
                    $"{smallBone.small_bone_zh}，" +
                    $"place={smallBone.place}, " +
                    $"has3D={smallBone.has_3d_model}, " +
                    $"meshCount=" +
                    $"{smallBone.mesh_names?.Count ?? 0}"
                );


                if (smallBone.mesh_names == null)
                {
                    continue;
                }


                foreach (string mesh in
                         smallBone.mesh_names)
                {
                    if (string.IsNullOrWhiteSpace(
                            mesh
                        ))
                    {
                        continue;
                    }


                    GalaBoneInfo info =
                        new GalaBoneInfo();


                    info.meshName =
                        mesh.Trim();


                    info.chineseName =
                        (smallBone.place == "Left"
                            ? "左側"
                            : smallBone.place == "Right"
                                ? "右側"
                                : "")
                        + smallBone.small_bone_zh;


                    info.englishName =
                        smallBone.place + " "
                        + smallBone.small_bone_en;


                    info.introduction =
                        smallBone.intro_text;


                    info.structureFunction =
                        smallBone.structure_function_text;


                    info.learning =
                        smallBone.learning_text;


                    BoneInfoRepository.Instance
                        .Register(
                            info
                        );


                    Debug.Log(
                        $"{LogTag} 已載入骨骼資料：" +
                        $"{info.meshName}"
                    );
                }
            }
        }
    }


    // =========================================================
    // 從 bone_groups 收集 MeshName
    // =========================================================
    private List<string> CollectMeshNames(
        PredictResponse response
    )
    {
        List<string> meshNames =
            new List<string>();


        HashSet<string> uniqueMeshNames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );


        if (response.bone_groups == null)
        {
            Debug.LogWarning(
                $"{LogTag} CollectMeshNames：" +
                $"bone_groups = null"
            );


            return meshNames;
        }


        foreach (BoneGroup group in
                 response.bone_groups)
        {
            if (group == null)
            {
                continue;
            }


            int smallBoneCount =
                group.small_bones?.Count ?? 0;


            Debug.Log(
                $"{LogTag} 骨骼群組：" +
                $"{group.bone_zh}，" +
                $"細部骨骼數：{smallBoneCount}"
            );


            if (group.small_bones == null)
            {
                continue;
            }


            foreach (SmallBone smallBone in
                     group.small_bones)
            {
                if (smallBone == null)
                {
                    continue;
                }


                Debug.Log(
                    $"{LogTag} 小骨：" +
                    $"{smallBone.small_bone_zh}，" +
                    $"側別：{smallBone.place}，" +
                    $"has_3d_model=" +
                    $"{smallBone.has_3d_model}，" +
                    $"mesh_count=" +
                    $"{smallBone.mesh_names?.Count ?? 0}"
                );


                if (!smallBone.has_3d_model)
                {
                    Debug.LogWarning(
                        $"{LogTag} 略過 " +
                        $"{smallBone.small_bone_zh}：" +
                        $"has_3d_model = false"
                    );


                    continue;
                }


                if (smallBone.mesh_names == null ||
                    smallBone.mesh_names.Count == 0)
                {
                    Debug.LogWarning(
                        $"{LogTag} 略過 " +
                        $"{smallBone.small_bone_zh}：" +
                        $"mesh_names 為空"
                    );


                    continue;
                }


                foreach (string meshName in
                         smallBone.mesh_names)
                {
                    if (string.IsNullOrWhiteSpace(
                            meshName
                        ))
                    {
                        continue;
                    }


                    string normalizedMeshName =
                        meshName.Trim();


                    Debug.Log(
                        $"{LogTag} Mesh = " +
                        $"{normalizedMeshName}"
                    );


                    if (uniqueMeshNames.Add(
                            normalizedMeshName
                        ))
                    {
                        meshNames.Add(
                            normalizedMeshName
                        );
                    }
                }
            }
        }


        return meshNames;
    }


    // =========================================================
    // 成功
    // =========================================================
    private void EndUploadSuccess(
        PredictResponse response
    )
    {
        IsUploading =
            false;


        UploadSucceeded?.Invoke(
            response
        );


        Debug.Log(
            $"{LogTag} 影像辨識流程完成"
        );
    }


    // =========================================================
    // 失敗
    // =========================================================
    private void HandleUploadFailure(
        string errorMessage
    )
    {
        IsUploading =
            false;


        Debug.LogError(
            $"{LogTag} {errorMessage}"
        );


        UploadFailed?.Invoke(
            errorMessage
        );
    }


    // =========================================================
    // 建立 HTTP 錯誤訊息
    // =========================================================
    private string BuildRequestErrorMessage(
        UnityWebRequest request
    )
    {
        string responseText =
            request.downloadHandler != null
                ? request.downloadHandler.text
                : string.Empty;


        return
            $"API 請求失敗\n" +
            $"網址：{predictUrl}\n" +
            $"狀態碼：{request.responseCode}\n" +
            $"錯誤：{request.error}\n" +
            $"回傳：{responseText}";
    }


    // =========================================================
    // Content-Type
    // =========================================================
    private string GetContentType(
        string filename
    )
    {
        string extension =
            Path.GetExtension(
                    filename
                )
                .ToLowerInvariant();


        switch (extension)
        {
            case ".png":
                return "image/png";

            case ".jpg":
            case ".jpeg":
                return "image/jpeg";

            default:
                return "application/octet-stream";
        }
    }
}