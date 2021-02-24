using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using TrilleonAutomation;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TransformMenu : Singleton<TransformMenu> {
    public InteractiveObject InteractiveObject;
    public TransformWheel TransformWheel;
    public CoordinatesBtnGroup Coordinates;
    public TranformWheelUnits Units;
    private GameObject model;

    private Vector3 origPosition = new Vector3(), offsetPosition = new Vector3(), interPosition = new Vector3(), cameraOrig = new Vector3();
    private Quaternion origRotation;

    [HideInInspector]
    public CanvasGroup CanvasGroup;

    private bool handHolding = false;

    private void Awake() {
        CanvasGroup = GetComponent<CanvasGroup>();

    }

    private void Update() {
        if (model == null)
            return;
        float value = TransformWheel.GetValue();
        if (handHolding) {
            Vector3 cameraNow = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));

            switch (Coordinates.GetSelectedAxis()) {
                case "x":
                    offsetPosition.x = GetRoundedValue(cameraNow.x - cameraOrig.x);
                    break;
                case "y":
                    offsetPosition.y = GetRoundedValue(cameraNow.y - cameraOrig.y);
                    break;
                case "z":
                    offsetPosition.z = GetRoundedValue(cameraNow.z - cameraOrig.z);
                    break;
            }
        } else {

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
        }
        Vector3 position = TransformConvertor.ROSToUnity(interPosition + offsetPosition);
        model.transform.localPosition = position;
        Vector3 newPosition = origPosition + InteractiveObject.transform.TransformDirection(interPosition + offsetPosition);
        Coordinates.X.SetValue(newPosition.x);
        Coordinates.X.SetDelta(offsetPosition.x + interPosition.x);
        Coordinates.Y.SetValue(newPosition.y);
        Coordinates.Y.SetDelta(offsetPosition.y + interPosition.y);
        Coordinates.Z.SetValue(newPosition.z);
        Coordinates.Z.SetDelta(offsetPosition.z + interPosition.z);
    }

    public float GetRoundedValue(float value) {
        switch (Units.GetValue()) {
            case "cm":
                return Mathf.Floor(value * 100) / 100f;
            case "mm":
                return Mathf.Floor(value * 1000) / 1000;
            case "μm":
                return Mathf.Floor(value * 1000000) / 1000000;
            default:
                return Mathf.Floor(value);
        };
    }

    public void HoldPressed() {
        cameraOrig = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));
        handHolding = true;
    }

    public void HoldReleased() {
        //handHolding = false;
    }

    public void StoreInterPosition() {
        interPosition += offsetPosition;
        offsetPosition = Vector3.zero;
    }

    public void Show(InteractiveObject interactiveObject) {
        offsetPosition = Vector3.zero;
        ResetTransformWheel();
        InteractiveObject = interactiveObject;

        if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            model = ((ActionPoint3D) interactiveObject).GetModelCopy();
        } else if (InteractiveObject.GetType() == typeof(DummyBox)) {
            model = ((DummyBox) interactiveObject).GetModelCopy();
        }
        if (model == null) {
            Hide();
            return;
        }
        
        origPosition = TransformConvertor.UnityToROS(interactiveObject.transform.localPosition);
        origRotation = interactiveObject.transform.localRotation;
        GameManager.Instance.Gizmo.transform.SetParent(model.transform);
        GameManager.Instance.Gizmo.transform.localPosition = Vector3.zero;
        GameManager.Instance.Gizmo.transform.localRotation = Quaternion.identity;
        GameManager.Instance.Gizmo.SetActive(true);
        enabled = true;
        EditorHelper.EnableCanvasGroup(CanvasGroup, true);
    }

    public void Hide() {
        InteractiveObject = null;
        GameManager.Instance.Gizmo.SetActive(false);
        GameManager.Instance.Gizmo.transform.SetParent(GameManager.Instance.Scene.transform);
        Destroy(model);
        model = null;
        enabled = false;
        EditorHelper.EnableCanvasGroup(CanvasGroup, false);        
    }

    public void ResetTransformWheel() {
        StoreInterPosition();
        if (handHolding)
            cameraOrig = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));
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
                await WebsocketManager.Instance.UpdateActionPointPosition(InteractiveObject.GetId(), DataHelper.Vector3ToPosition(origPosition + interPosition + offsetPosition));
                ResetPosition();
            } catch (RequestFailedException e) {
                Notifications.Instance.ShowNotification("Failed to update action point position", e.Message);
            }
        } else if (InteractiveObject.GetType() == typeof(DummyBox)) {   
            //InteractiveObject.transform.Translate(TransformConvertor.ROSToUnity(offsetPosition));
            InteractiveObject.transform.Translate(TransformConvertor.ROSToUnity(interPosition + offsetPosition));
            InteractiveObject.transform.hasChanged = true;
            ResetPosition();
        }
    }

    public void ResetPosition() {        
        offsetPosition = Vector3.zero;
        interPosition = Vector3.zero;
        ResetTransformWheel();
    }
}
