using System.Collections;
using System.Collections.Generic;
using Base;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TransformMenu : Singleton<TransformMenu>
{
    public InteractiveObject InteractiveObject;
    public TransformWheel TransformWheel;
    public CoordinatesBtnGroup Coordinates;

    private Vector3 origPosition = new Vector3(), offsetPosition = new Vector3();
    private Quaternion origRotation;

    private CanvasGroup canvasGroup;

    private void Awake() {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update() {
        if (InteractiveObject == null)
            return;
        float value = TransformWheel.GetValue();
        switch (Coordinates.GetSelectedAxis()) {
            case "x":
                offsetPosition.x = value;
                break;
            case "y":
                offsetPosition.y = value;
                break;
            case "z":
                offsetPosition.z = value;
                break;
        }
        Debug.LogError(offsetPosition);
        InteractiveObject.transform.localPosition = origPosition + offsetPosition;
        Coordinates.X.Value.text = (origPosition.x + offsetPosition.x).ToString();
        Coordinates.X.Delta.text = (offsetPosition.x).ToString();
        Coordinates.Y.Value.text = (origPosition.y + offsetPosition.y).ToString();
        Coordinates.Y.Delta.text = (offsetPosition.y).ToString();
        Coordinates.Z.Value.text = (origPosition.z + offsetPosition.z).ToString();
        Coordinates.Z.Delta.text = (offsetPosition.z).ToString();
    }

    public void Show(InteractiveObject interactiveObject) {
        InteractiveObject = interactiveObject;
        origPosition = interactiveObject.transform.localPosition;
        origRotation = interactiveObject.transform.localRotation;
        GameManager.Instance.Gizmo.transform.SetParent(InteractiveObject.transform);
        GameManager.Instance.Gizmo.transform.localPosition = Vector3.zero;
        GameManager.Instance.Gizmo.transform.localRotation = Quaternion.identity;
        GameManager.Instance.Gizmo.SetActive(true);
        enabled = true;
        EditorHelper.EnableCanvasGroup(canvasGroup, true);
    }

    public void Hide() {
        InteractiveObject = null;
        enabled = false;
        EditorHelper.EnableCanvasGroup(canvasGroup, false);
    }

    public void ResetTransformWheel() {
        TransformWheel.InitList();
    }
}
