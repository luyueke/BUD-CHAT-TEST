using Basic.Utils;
using Game.Utils;
using UnityEngine;

public class UIFollow : MonoBehaviour
{
    public Transform Target;
    public Vector3 offset;
    public Vector2 screenOffset;

    [SerializeField]
    private Camera cam;

    private CanvasGroup canvasGroup;
    private void Awake()
    {
        cam = cam == null ? Camera.main : cam;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (Target && cam != null)
        {
            Vector3 CamPos = cam.WorldToScreenPoint(Target.position + offset);
            if (canvasGroup)
            {
                canvasGroup.alpha = CamPos.z > 0 ? 1 : 0;
            }
            Vector2 targetPos = ConvertScreenPointToUIPoint(transform.parent, CamPos);
            targetPos += new Vector2(screenOffset.x, screenOffset.y);
            transform.localPosition = targetPos;
        }
    }
    
    public static Vector2 ConvertScreenPointToUIPoint(Transform transform,Vector2 screenPoint)
    {
        RectTransform rect = transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, GameCameraUtils.Inst.GetUICamera(), out Vector2 result);
        return result;
    }
    
    private bool IsInView(Vector3 worldPos)
    {
        Transform camTransform = cam.transform;
        Vector2 viewPos = cam.WorldToViewportPoint(worldPos);
        Vector3 dir = (worldPos - camTransform.position).normalized;
        float dot = Vector3.Dot(camTransform.forward, dir); //判断物体是否在相机前面
        if (dot > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
        {
            return true;
        }
        return false;
    }

    public void SetCamera(Camera cam = null)
    {
        this.cam = cam == null ? Camera.main : cam;
    }
}
