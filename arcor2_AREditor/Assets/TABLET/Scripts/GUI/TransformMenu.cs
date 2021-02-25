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
    public TranformWheelUnits Units, UnitsDegrees;
    private GameObject model;
    private bool rotate = false;

    private Vector3 origPosition = new Vector3(), offsetPosition = new Vector3(), interPosition = new Vector3(), cameraOrig = new Vector3();
    private Quaternion origRotation = new Quaternion(), offsetRotation = new Quaternion(), interRotation = Quaternion.identity;

    [HideInInspector]
    public CanvasGroup CanvasGroup;

    private bool handHolding = false;

    private void Awake() {
        CanvasGroup = GetComponent<CanvasGroup>();

    }

    private void Update() {
        if (model == null)
            return;
        
        if (rotate) {
            UpdateRotate(GetRotationValue(TransformWheel.GetValue()));
        } else {
            UpdateTranslate(GetPositionValue(TransformWheel.GetValue()));
        }
        Vector3 position = TransformConvertor.ROSToUnity(interPosition + offsetPosition);
        model.transform.localPosition = position;
        Quaternion roatation = TransformConvertor.ROSToUnity(interRotation * offsetRotation);
        model.transform.localRotation = roatation;
    }

    private float GetPositionValue(float v) {
        switch (Units.GetValue()) {
            case "m":
                return v;
            case "cm":
                return v * 0.01f;
            case "mm":
                return v * 0.001f;
            case "μm":
                return v * 0.000001f;
            default:
                return v;
        };
    }

    private int ComputePositionValue(float value) {
        switch (Units.GetValue()) {
            case "cm":
                return (int) (value * 100);
            case "mm":
                return (int) (value * 1000);
            case "μm":
                return (int) (value * 1000000);
            default:
                return (int) value;
        };
    }

    private float GetRotationValue(float v) {
        switch (UnitsDegrees.GetValue()) {
            case "°":
                return v;
            case "'":
                return v / 60f;
            case "''":
                return v / 3600f;
            default:
                return v;
        };
    }

    private int ComputeRotationValue(float value) {
        switch (UnitsDegrees.GetValue()) {
            case "°":
                return (int) value;
            case "'":
                return (int) (value * 60);
            case "''":
                return (int) (value * 3600);
            default:
                return (int) value;
        };
    }

    private void UpdateTranslate(float wheelValue) {
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
                    offsetPosition.x = wheelValue;
                    break;
                case "y":
                    offsetPosition.y = wheelValue;
                    break;
                case "z":
                    offsetPosition.z = wheelValue;
                    break;
            }
        }
        
        Vector3 newPosition = origPosition + InteractiveObject.transform.TransformDirection(interPosition + offsetPosition);
        Coordinates.X.SetValueMeters(newPosition.x);
        Coordinates.X.SetDeltaMeters(offsetPosition.x + interPosition.x);
        Coordinates.Y.SetValueMeters(newPosition.y);
        Coordinates.Y.SetDeltaMeters(offsetPosition.y + interPosition.y);
        Coordinates.Z.SetValueMeters(newPosition.z);
        Coordinates.Z.SetDeltaMeters(offsetPosition.z + interPosition.z);
    }

    private void UpdateRotate(float wheelValue) {
        if (handHolding) {
            /*Vector3 cameraNow = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));

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
            }*/
        } else {

            switch (Coordinates.GetSelectedAxis()) {
                case "x":
                    offsetRotation = Quaternion.Euler(wheelValue, offsetRotation.eulerAngles.y, offsetRotation.eulerAngles.z);
                    break;
                case "y":
                    offsetRotation = Quaternion.Euler(offsetRotation.eulerAngles.x, wheelValue, offsetRotation.eulerAngles.z);
                    break;
                case "z":
                    offsetRotation = Quaternion.Euler(offsetRotation.eulerAngles.x, offsetRotation.eulerAngles.y, wheelValue);
                    break;
            }

        }

        Quaternion newrotation = TransformConvertor.UnityToROS(GameManager.Instance.Scene.transform.rotation * Quaternion.Inverse(model.transform.rotation));
        Coordinates.X.SetValueDegrees(newrotation.eulerAngles.x);
        Coordinates.X.SetDeltaDegrees(TransformConvertor.UnityToROS(model.transform.localRotation).eulerAngles.x);
        Debug.LogError(TransformConvertor.UnityToROS(model.transform.localRotation).eulerAngles.x);
        Coordinates.Y.SetValueDegrees(newrotation.eulerAngles.y);
        Coordinates.Y.SetDeltaDegrees(TransformConvertor.UnityToROS(model.transform.localRotation).eulerAngles.y);
        Coordinates.Z.SetValueDegrees(newrotation.eulerAngles.z);
        Coordinates.Z.SetDeltaDegrees(TransformConvertor.UnityToROS(model.transform.localRotation).eulerAngles.z);
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

    public void SwitchToTranslate() {
        TransformWheel.Units = Units;
        Units.gameObject.SetActive(true);
        UnitsDegrees.gameObject.SetActive(false);
        ResetPosition();
        rotate = false;
    }

    public void SwitchToRotate() {
        TransformWheel.Units = UnitsDegrees;
        Units.gameObject.SetActive(false);
        UnitsDegrees.gameObject.SetActive(true);
        ResetPosition();
        rotate = true;
    }

    public void HoldPressed() {
        cameraOrig = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));
        handHolding = true;
    }

    public void HoldReleased() {
        handHolding = false;
    }

    public void StoreInterPosition() {
        
        interPosition += offsetPosition;
        interRotation *= offsetRotation;
        offsetPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;
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
                if (rotate)
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.x));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.x));
                break;
            case "y":
                if (rotate)
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.y));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.y));
                break;
            case "z":
                if (rotate)
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.z));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.z));
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
            //InteractiveObject.transform.localRotation = TransformConvertor.ROSToUnity(origRotation * interRotation * offsetRotation);
            //InteractiveObject.transform.Translate(TransformConvertor.ROSToUnity(interPosition + offsetPosition));
            InteractiveObject.transform.position = model.transform.position;
            InteractiveObject.transform.rotation = model.transform.rotation;
            InteractiveObject.transform.hasChanged = true;
            ResetPosition();
        }
    }

    public void ResetPosition() {        
        offsetPosition = Vector3.zero;
        interPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;
        interRotation = Quaternion.identity;
        ResetTransformWheel();
    }
}
