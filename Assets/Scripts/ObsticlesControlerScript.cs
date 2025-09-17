using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ObsticlesControlerScript : MonoBehaviour
{

    [HideInInspector]
    public float speed = 1f;
    public float waweAmpLitude = 25f;
    public float waweFrequency = 1f;
    public float fadeDuration = 1.5f;
    private ObjectScript objectScript;
    private ScreenBoundriesScript screenBoundriesScript;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isFadingOut = false;
    private Image image;
    private Color originalColor;
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();

        image = GetComponent<Image>();
        originalColor = image.color;

        objectScript = Object.FindAnyObjectByType<ObjectScript>();
        screenBoundriesScript = Object.FindFirstObjectByType<ScreenBoundriesScript>();
        StartCoroutine(FadeIn());
    }

    // Update is called once per frame
    void Update()
    {
        float waveOffset = Mathf.



    




    If(speed > 0)











    /*bilde kods*/



    /*bilde kods*/






    if(objectScript.drag && !isFadingOut && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input))
        }


    IEnumerator ShrinkAndDestroy(GameObject target, float duration)
        {
            Vector3 originalScale = target.transform.localScale;
            Quaternion originalRotation = target.transform.rotation;
            float t = 0f;
            while(t < duration)
            {
                t += Time.deltaTime;
                target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t / duration);
                float angle = Mathf.Lerp(0, 360, t / duration);
                target.transform.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;

            }

            //Ko darīt ar masīnu talāk?
            //nav obligāti jaiznīcina.
            Destroy(target);
        }

        IEnumerator RecoverColor()
        {
            yield return new WaitForSeconds(0.5f);
            image.tintColor = originalColor;

            IEnumerator Vibrate()
            {
                Vector2 originalPosition = rectTransform.anchoredPosition;
                float duration = 0.3f;
                float elpased = 0f;
                float intensity = 5f;

                while(elpased < duration)
                {
                    RectTransform rectTransform = new RectTransform()/*kods*/
                }
            }
        }
    }
}
