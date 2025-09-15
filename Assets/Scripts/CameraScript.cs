using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class CameraScript : MonoBehaviour
{
    public float maxZoom = 300f,
                minZoom = 150f,
                panSpeed = 6f;

    Vector3 bottomLeft, topRight;
    float cameraMaxX, cameraMaxY, cameraMinX, cameraMinY, x, y;
    public Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        topRight = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, -transform.position.z));
        bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, -transform.position.z));
        cameraMaxX = topRight.x;
        cameraMaxY = topRight.y;
        cameraMinX = bottomLeft.x;
        cameraMinY = bottomLeft.y;
    }

    void Update()
    {
        x = Input.GetAxis("Mouse x") * panSpeed;
        y = Input.GetAxis("Mouse Y") * panSpeed;
        transform.Translate(x, y, 0);

        if ((Input.GetAxis("Mouse ScrollWheel") > 0) && cam.orthographicSize > minZoom)
        {
            cam.orthographicSize = cam.orthographicSize - 50;
        }

        if ((Input.GetAxis("Mouse ScrollWheel") < 0) && cam.orthographicSize < maxZoom)
        {
            cam.orthographicSize = cam.orthographicSize + 50;
        }

        topRight = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, -transform.position.z));
        bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, -transform.position.z));

        if (topRight.x > cameraMaxX)
        {
            transform.position = new Vector3(
                transform.position.x - (topRight.x - cameraMaxX), transform.position.y, transform.position.z);
        }

        if (topRight.y > cameraMaxY)
        {
            transform.position = new Vector3(
                transform.position.x, transform.position.y - (topRight.y - cameraMaxY), transform.position.z);
        }

        if (bottomLeft.x > cameraMinX)
        {
            transform.position = new Vector3(
                transform.position.x + (cameraMinX - bottomLeft.x), transform.position.y, transform.position.z);
        }

        if (bottomLeft.y > cameraMinY)
        {
            transform.position = new Vector3(
                transform.position.x, transform.position.y + (cameraMinY - bottomLeft.y), transform.position.z);
        }
    }
}
