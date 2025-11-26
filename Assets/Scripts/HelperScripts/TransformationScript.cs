using UnityEngine;
using UnityEngine.EventSystems;

public class TransformationScript : MonoBehaviour
{
    [Header("Speeds")]
    public float rotationSpeed = 90f;     // grādi sekundē
    public float scaleSpeed = 0.5f;       // scaling ātrums (pogām + touch)

    [Header("Scale limits")]
    public float minScale = 0.3f;
    public float maxScale = 0.9f;

    // publisks flags – citi skripti var pārbaudīt, vai objekts tiek mainīts
    public static bool isTransforming = false;

    // UI pogu flagi
    private bool rotateCW, rotateCCW;
    private bool scaleUpY, scaleDownY, scaleUpX, scaleDownX;

    void Update()
    {
        if (ObjectScript.lastDragged == null)
            return;

        RectTransform rt = ObjectScript.lastDragged.GetComponent<RectTransform>();
        if (!rt) return;

        // --- DESKTOP / EDITOR tastatūra ---
#if UNITY_STANDALONE || UNITY_EDITOR
        HandleKeyboardInput(rt);
#endif

        // --- MOBILE touch kontrole ---
#if UNITY_ANDROID || UNITY_IOS
        HandleTouchInput(rt);
#endif

        // --- UI POGU kontrole (strādā uz visām platformām) ---
        HandleButtonInput(rt);

        // vai vispār kaut kas notiek
        isTransforming = rotateCW || rotateCCW ||
                         scaleUpY || scaleDownY ||
                         scaleUpX || scaleDownX;
    }

    // ===================== DESKTOP =====================
    void HandleKeyboardInput(RectTransform target)
    {
        // Rotācija
        if (Input.GetKey(KeyCode.Z))
            target.Rotate(0, 0, Time.deltaTime * rotationSpeed);

        if (Input.GetKey(KeyCode.X))
            target.Rotate(0, 0, -Time.deltaTime * rotationSpeed);

        // Skala
        Vector3 scale = target.localScale;

        if (Input.GetKey(KeyCode.UpArrow))
            scale.y = Mathf.Min(scale.y + 0.005f, maxScale);
        if (Input.GetKey(KeyCode.DownArrow))
            scale.y = Mathf.Max(scale.y - 0.005f, minScale);
        if (Input.GetKey(KeyCode.LeftArrow))
            scale.x = Mathf.Max(scale.x - 0.005f, minScale);
        if (Input.GetKey(KeyCode.RightArrow))
            scale.x = Mathf.Min(scale.x + 0.005f, maxScale);

        target.localScale = scale;
    }

    // ===================== MOBILE TOUCH =====================
    void HandleTouchInput(RectTransform target)
    {
        // 1 pirksts – rotācija (pavelkot horizontāli)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float rotation = -touch.deltaPosition.x * 0.3f;
                target.Rotate(0, 0, rotation);
            }
        }

        // 2 pirksti – pinch scale + twist rotācija
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevT0 = t0.position - t0.deltaPosition;
            Vector2 prevT1 = t1.position - t1.deltaPosition;

            float prevDist = (prevT0 - prevT1).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = currDist - prevDist;

            // Scale (pinch)
            Vector3 scale = target.localScale;
            float newScale = scale.x + (delta * scaleSpeed * Time.deltaTime * 0.01f);
            newScale = Mathf.Clamp(newScale, minScale, maxScale);
            target.localScale = new Vector3(newScale, newScale, 1f);

            // Twist rotācija (pēc izvēles)
            Vector2 prevDir = (prevT1 - prevT0).normalized;
            Vector2 currDir = (t1.position - t0.position).normalized;
            float angle = Vector2.SignedAngle(prevDir, currDir);
            target.Rotate(0, 0, angle);
        }
    }

    // ===================== UI BUTTONS =====================
    void HandleButtonInput(RectTransform rt)
    {
        // Rotācija no pogām
        if (rotateCW)
            rt.Rotate(0, 0, -rotationSpeed * Time.deltaTime);

        if (rotateCCW)
            rt.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Skala no pogām
        Vector3 scale = rt.localScale;

        if (scaleUpY)
            scale.y = Mathf.Min(scale.y + scaleSpeed * Time.deltaTime, maxScale);

        if (scaleDownY)
            scale.y = Mathf.Max(scale.y - scaleSpeed * Time.deltaTime, minScale);

        if (scaleUpX)
            scale.x = Mathf.Min(scale.x + scaleSpeed * Time.deltaTime, maxScale);

        if (scaleDownX)
            scale.x = Mathf.Max(scale.x - scaleSpeed * Time.deltaTime, minScale);

        rt.localScale = scale;
    }

    // ===================== UI EVENTTRIGGER CALLBACKI =====================
    public void StartRotateCW(BaseEventData data)  { rotateCW = true; }
    public void StopRotateCW(BaseEventData data)   { rotateCW = false; }

    public void StartRotateCCW(BaseEventData data) { rotateCCW = true; }
    public void StopRotateCCW(BaseEventData data)  { rotateCCW = false; }

    public void StartScaleUpY(BaseEventData data)  { scaleUpY = true; }
    public void StopScaleUpY(BaseEventData data)   { scaleUpY = false; }

    public void StartScaleDownY(BaseEventData data){ scaleDownY = true; }
    public void StopScaleDownY(BaseEventData data) { scaleDownY = false; }

    public void StartScaleUpX(BaseEventData data)  { scaleUpX = true; }
    public void StopScaleUpX(BaseEventData data)   { scaleUpX = false; }

    public void StartScaleDownX(BaseEventData data){ scaleDownX = true; }
    public void StopScaleDownX(BaseEventData data) { scaleDownX = false; }
}
