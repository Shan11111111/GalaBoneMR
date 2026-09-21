using UnityEngine;
using UnityEngine.InputSystem;

public class BoneRayInteractor : MonoBehaviour
{
    [Header("用來發射射線的相機")]
    [SerializeField]
    private Camera rayCamera;

    [Header("最大射線距離")]
    [SerializeField]
    private float maxDistance = 10f;

    private void Awake()
    {
        if (rayCamera == null)
        {
            rayCamera = Camera.main;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (rayCamera == null)
        {
            Debug.LogError("BoneRayInteractor 找不到 Camera");
            return;
        }

        Vector2 mousePosition =
            mouse.position.ReadValue();

        Ray ray = rayCamera.ScreenPointToRay(
            mousePosition
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance))
        {
            Debug.Log("射線沒有碰到物件");
            return;
        }

        BoneSelectable selectable =
            hit.collider.GetComponent<BoneSelectable>();

        if (selectable == null)
        {
            selectable =
                hit.collider.GetComponentInParent<BoneSelectable>();
        }

        if (selectable == null)
        {
            Debug.Log(
                $"碰到 {hit.collider.name}，但沒有 BoneSelectable"
            );
            return;
        }

        selectable.Select();
    }
}