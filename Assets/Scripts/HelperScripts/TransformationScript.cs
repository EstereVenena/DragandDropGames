using UnityEngine;

public class TransformationScript : MonoBehaviour
{
    private float rotationSpeed = 90f;      // Degrees per second for rotation
    private float scaleSpeed = 0.5f;        // Scale speed for touch pinch
    private float minScale = 0.3f;
    private float maxScale = 0.9f;

    void Update()
    {
        if (ObjectScript.lastDragged == null) return;

#if UNITY_STANDALONE || UNITY_EDITOR
        HandleKeyboardInput();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif
    }

    // --- Desktop Controls (Windows / Editor) ---
    void HandleKeyboardInput()
    {
        RectTransform target = ObjectScript.lastDragged.GetComponent<RectTransform>();

        // Rotate clockwise
        if (Input.GetKey(KeyCode.Z))
            target.Rotate(0, 0, Time.deltaTime * rotationSpeed);

        // Rotate counterclockwise
        if (Input.GetKey(KeyCode.X))
            target.Rotate(0, 0, -Time.deltaTime * rotationSpeed);

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

    // --- Mobile Controls (Android / iOS) ---
    void HandleTouchInput()
    {
        RectTransform target = ObjectScript.lastDragged.GetComponent<RectTransform>();

        // One-finger drag to rotate
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float rotation = -touch.deltaPosition.x * 0.3f;
                target.Rotate(0, 0, rotation);
            }
        }

        // Two-finger pinch to scale + twist to rotate
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

            // Optional: twist gesture rotates object
            Vector2 prevDir = (prevT1 - prevT0).normalized;
            Vector2 currDir = (t1.position - t0.position).normalized;
            float angle = Vector2.SignedAngle(prevDir, currDir);
            target.Rotate(0, 0, angle);
        }
    }
}
