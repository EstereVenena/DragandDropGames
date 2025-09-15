using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Switch;

public class DropPlaceScript : MonoBehaviour
{
    private float placeZRot, vehicleZRot, rotDiff;
    private Vector3 placeSiz, vehicleSiz;
    private float xsizeDiff, ySizeDiff;
    public ObjectScript objScript;

    public void OnDrop(PointerEventData eventData)
    {
        if ((eventData.pointerDrag != null) && Input.GetMouseButtonUp(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) ;
        {
            if (eventData.pointerDrag.tag.Equals(tag))
            {

                placeZRot = eventData.pointerDrag.GetComponent<RectTransform>().transform.eulerAngles.z;
                vehicleZRot = GetComponent<RectTransform>().transform.eulerAngles.z;

                rotDiff = Mathf.Abs(placeZRot - vehicleZRot);
                Debug.Log("Rotation Difference: " + rotDiff);

                placeSiz = eventData.pointerDrag.GetComponent<RectTransform>().localScale;
                vehicleSiz = GetComponent<RectTransform>().localScale;

                xsizeDiff = Mathf.Abs(placeSiz.x - vehicleSiz.x);
                ySizeDiff = Mathf.Abs(placeSiz.y - vehicleSiz.y);

                Debug.Log("X size Difference: " + xsizeDiff);
                Debug.Log("y size Difference: " + ySizeDiff);

                if ((rotDiff <= 5 || (rotDiff >=355 && rotDiff <= 360)) &&
                    (xsizeDiff <= 0.05 && ySizeDiff <= 0.05)){

                    Debug.Log("Corect place");
                    objScript.rightPlace = true;
                    eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;
                    eventData.pointerDrag.GetComponent<RectTransform>().localRotation = GetComponent<RectTransform>().localRotation;
                    eventData.pointerDrag.GetComponent<RectTransform>().localScale = GetComponent<RectTransform>().localScale;

                    switch (eventData.pointerDrag.tag)
                    {
                        case "Garbage":
                            objScript.effects.PlayOneShot(objScript.audioCli[2]);
                            break;

                        case "Medicine":
                            objScript.effects.PlayOneShot(objScript.audioCli[3]);
                            break;

                        case "Fire":
                            objScript.effects.PlayOneShot(objScript.audioCli[4]);
                            break;

                        case "Buss":
                            objScript.effects.PlayOneShot(objScript.audioCli[5]);
                            break;

                        case "Cement":
                            objScript.effects.PlayOneShot(objScript.audioCli[6]);
                            break;

                        case "Ekskavator":
                            objScript.effects.PlayOneShot(objScript.audioCli[7]);
                            break;

                        case "Police":
                            objScript.effects.PlayOneShot(objScript.audioCli[8]);
                            break;

                        case "Trctor1":
                            objScript.effects.PlayOneShot(objScript.audioCli[9]);
                            break;

                        case "Tractor5":
                            objScript.effects.PlayOneShot(objScript.audioCli[10]);
                            break;

                        case "CarE46":
                            objScript.effects.PlayOneShot(objScript.audioCli[11]);
                            break;

                        case "CarEB2":
                            objScript.effects.PlayOneShot(objScript.audioCli[12]);
                            break;

                        case "CarE61":
                            objScript.effects.PlayOneShot(objScript.audioCli[13]);
                            break;

                        default:
                            Debug.Log("Unknown tag detected");
                            break;
                    }
                }
            }
            else
            {
                objScript.rightPlace = false;
                objScript.effects.PlayOneShot(objScript.audioCli[1]);
                switch(eventData.pointerDrag.tag)
                {
                    case "Garbage":
                        objScript.vehicles[0].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[0];
                        break;

                    case "Medicine":
                        objScript.vehicles[1].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[1];
                        break;

                    case "Fire":
                        objScript.vehicles[2].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[2];
                        break;

                    case "Buss":
                        objScript.vehicles[3].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[3];
                        break;

                    case "Cement":
                        objScript.vehicles[4].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[4];
                        break;

                    case "Ekskavator":
                        objScript.vehicles[5].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[5];
                        break;

                    case "Police":
                        objScript.vehicles[6].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[6];
                        break;

                    case "Trctor1":
                        objScript.vehicles[7].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[7];
                        break;

                    case "Tractor5":
                        objScript.vehicles[8].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[8];
                        break;

                    case "CarE46":
                        objScript.vehicles[9].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[9];
                        break;

                    case "CarEB2":
                        objScript.vehicles[10].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[10];
                        break;

                    case "CarE61":
                        objScript.vehicles[11].GetComponent<RectTransform>().localPosition = objScript.startCoordinates[11];
                        break;

                    default:
                        Debug.Log("Unknown tag detected");
                        break;
                }
            }
        }
    }
}
