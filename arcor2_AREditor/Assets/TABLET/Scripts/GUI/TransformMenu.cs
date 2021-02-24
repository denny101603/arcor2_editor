using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using TrilleonAutomation;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TransformMenu : Singleton<TransformMenu>
{
    public InteractiveObject InteractiveObject;
    public TransformWheel TransformWheel;
    public CoordinatesBtnGroup Coordinates;
    private GameObject model;

    private Vector3 origPosition = new Vector3(), offsetPosition = new Vector3();
    private Quaternion origRotation;

    private CanvasGroup canvasGroup;

    private void Awake() {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update() {
        if (model == null)
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
        Vector3 position = TransformConvertor.ROSToUnity(offsetPosition);
        model.transform.localPosition = position;
        Coordinates.X.SetValue(origPosition.x + offsetPosition.x);
        Coordinates.X.SetDelta(offsetPosition.x);
        Coordinates.Y.SetValue(origPosition.y + offsetPosition.y);
        Coordinates.Y.SetDelta(offsetPosition.y);
        Coordinates.Z.SetValue(origPosition.z + offsetPosition.z);
        Coordinates.Z.SetDelta(offsetPosition.z);
    }

    public void Show(InteractiveObject interactiveObject) {
        InteractiveObject = interactiveObject;
        model = ((ActionPoint3D) interactiveObject).GetModelCopy();
        origPosition = TransformConvertor.UnityToROS(interactiveObject.transform.localPosition);
        origRotation = interactiveObject.transform.localRotation;
        GameManager.Instance.Gizmo.transform.SetParent(model.transform);
        GameManager.Instance.Gizmo.transform.localPosition = Vector3.zero;
        GameManager.Instance.Gizmo.transform.localRotation = Quaternion.identity;
        GameManager.Instance.Gizmo.SetActive(true);
        enabled = true;
        EditorHelper.EnableCanvasGroup(canvasGroup, true);
    }

    public void Hide() {
        InteractiveObject = null;
        Destroy(model);
        model = null;
        enabled = false;
        EditorHelper.EnableCanvasGroup(canvasGroup, false);
    }

    public void ResetTransformWheel() {
        switch (Coordinates.GetSelectedAxis()) {
            case "x":
                TransformWheel.InitList(offsetPosition.x);
                break;
            case "y":
                TransformWheel.InitList(offsetPosition.y);
                break;
            case "z":
                TransformWheel.InitList(offsetPosition.z);
                break;
        }
    }

    public async void SubmitPosition() {
        if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            try {

                await WebsocketManager.Instance.UpdateActionPointPosition(InteractiveObject.GetId(), DataHelper.Vector3ToPosition(origPosition + offsetPosition));
                ResetPosition();
            } catch (RequestFailedException e) {
                Notifications.Instance.ShowNotification("Failed to update action point position", e.Message);
            }
        } else if (InteractiveObject.GetType() == typeof(DummyBox)) {

        }
    }

    public void ResetPosition() {        
        offsetPosition = Vector3.zero;
        ResetTransformWheel();
    }
}
