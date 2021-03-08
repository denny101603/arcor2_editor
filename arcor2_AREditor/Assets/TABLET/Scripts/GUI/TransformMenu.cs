using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using RosSharp.Urdf;
using TrilleonAutomation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;

[RequireComponent(typeof(CanvasGroup))]
public class TransformMenu : Singleton<TransformMenu> {
    public InteractiveObject InteractiveObject;
    public TransformWheel TransformWheel;
    public CoordinatesBtnGroup Coordinates;
    public TranformWheelUnits Units, UnitsDegrees;
    private GameObject model;
    public TwoStatesToggle RobotTabletBtn, RotateTranslateBtn;

    private Vector3 origPosition = new Vector3(), offsetPosition = new Vector3(), interPosition = new Vector3(), cameraOrig = new Vector3();
    private Quaternion origRotation = new Quaternion(), offsetRotation = new Quaternion(), interRotation = Quaternion.identity;

    [HideInInspector]
    public CanvasGroup CanvasGroup;

    private bool handHolding = false, DummyAimBox = false;
    private string robotId;

    private RobotEE endEffector;

    public GameObject DummyBoxing;

    public List<Image> Arrows, Dots, DotsBackgrounds;
    private List<GameObject> dummyPoints = new List<GameObject>();
    private int currentArrowIndex;
    public Button NextArrowBtn, PreviousArrowBtn;


    private void Awake() {
        CanvasGroup = GetComponent<CanvasGroup>();
        dummyPoints.Add(null);
        dummyPoints.Add(null);
        dummyPoints.Add(null);
        dummyPoints.Add(null);
    }

    private void Update() {
        if (model == null)
            return;
        if (RobotTabletBtn.CurrentState == "robot") {
            if (endEffector != null) {
                model.transform.position = endEffector.transform.position;
                Coordinates.X.SetValueMeters(endEffector.transform.position.x);
                Coordinates.X.SetDeltaMeters(model.transform.position.x - InteractiveObject.transform.position.x);
                Coordinates.Y.SetValueMeters(endEffector.transform.position.y);
                Coordinates.Y.SetDeltaMeters(model.transform.position.y - InteractiveObject.transform.position.y);
                Coordinates.Z.SetValueMeters(endEffector.transform.position.z);
                Coordinates.Z.SetDeltaMeters(model.transform.position.z - InteractiveObject.transform.position.z);
                return;
            }
        }
        if (RotateTranslateBtn.CurrentState == "rotate") {
            UpdateRotate(GetRotationValue(TransformWheel.GetValue()));
        } else {
            UpdateTranslate(GetPositionValue(TransformWheel.GetValue()));
        }
        
        if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            Vector3 position = TransformConvertor.ROSToUnity(interPosition + offsetPosition);
            model.transform.localPosition = TransformConvertor.ROSToUnity(origRotation) * position;
            Quaternion rotation = TransformConvertor.ROSToUnity(TransformConvertor.UnityToROS(((ActionPoint3D) InteractiveObject).GetRotation()) * interRotation * offsetRotation);
            model.transform.localRotation = rotation;
        } else if (InteractiveObject.GetType() == typeof(DummyBox)) {
            Vector3 position = TransformConvertor.ROSToUnity(interPosition + offsetPosition);
            model.transform.localPosition = position;
            Quaternion rotation = TransformConvertor.ROSToUnity(interRotation * offsetRotation);
            model.transform.localRotation = rotation;
        }
        
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
                    TransformWheel.SetValue(ComputePositionValue(offsetPosition.x));
                    break;
                case "y":
                    offsetPosition.y = GetRoundedValue(cameraNow.y - cameraOrig.y);
                    TransformWheel.SetValue(ComputePositionValue(offsetPosition.y));
                    break;
                case "z":
                    offsetPosition.z = GetRoundedValue(cameraNow.z - cameraOrig.z);
                    TransformWheel.SetValue(ComputePositionValue(offsetPosition.z));
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
        Quaternion delta = Quaternion.identity;
        if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            delta = TransformConvertor.UnityToROS(model.transform.localRotation) * Quaternion.Inverse(origRotation);
        } else if (InteractiveObject.GetType() == typeof(DummyBox)) {
            delta = TransformConvertor.UnityToROS(model.transform.localRotation);
        }
       
        Quaternion newrotation = TransformConvertor.UnityToROS(GameManager.Instance.Scene.transform.rotation * Quaternion.Inverse(model.transform.rotation));
        Coordinates.X.SetValueDegrees(newrotation.eulerAngles.x);
        Coordinates.X.SetDeltaDegrees(delta.eulerAngles.x);
        Coordinates.Y.SetValueDegrees(newrotation.eulerAngles.y);
        Coordinates.Y.SetDeltaDegrees(delta.eulerAngles.y);
        Coordinates.Z.SetValueDegrees(newrotation.eulerAngles.z);
        Coordinates.Z.SetDeltaDegrees(delta.eulerAngles.z);
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
    }

    public void SwitchToRotate() {
        TransformWheel.Units = UnitsDegrees;
        Units.gameObject.SetActive(false);
        UnitsDegrees.gameObject.SetActive(true);
        ResetPosition();
    }

    public void SwitchToTablet() {
        TransformWheel.gameObject.SetActive(true);
        ResetPosition();
        RotateTranslateBtn.SetInteractivity(true);
    }

    public void SwitchToRobot() {
        if (endEffector == null) {
            endEffector = FindObjectOfType<RobotEE>();
            if (endEffector == null) {
                Notifications.Instance.ShowNotification("Robot not ready", "Scene not started");
                return;
            }
        }
        TransformWheel.gameObject.SetActive(false);
        ResetPosition();
        if (RotateTranslateBtn.CurrentState == "rotate") {
            RotateTranslateBtn.SetState("translate");
            SwitchToTranslate();
        }
        RotateTranslateBtn.SetInteractivity(false);
    }

    public void HoldPressed() {
        if (RobotTabletBtn.CurrentState == "tablet") {
            cameraOrig = TransformConvertor.UnityToROS(InteractiveObject.transform.InverseTransformPoint(Camera.main.transform.position));
            StoreInterPosition();
            handHolding = true;
        } else {
            WebsocketManager.Instance.HandTeachingMode(robotId: robotId, enable: true);
        }
    }

    public void HoldReleased() {
        if (RobotTabletBtn.CurrentState == "tablet")
            handHolding = false;
        else
            WebsocketManager.Instance.HandTeachingMode(robotId: robotId, enable: false);
    }

    public void StoreInterPosition() {
        
        interPosition += offsetPosition;
        interRotation *= offsetRotation;
        offsetPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;
    }

    public void Show(InteractiveObject interactiveObject, bool dummyAimBox = false) {
        foreach (IRobot robot in SceneManager.Instance.GetRobots()) {
            robotId = robot.GetId();
        }
        RobotTabletBtn.SetState("robot");
        RotateTranslateBtn.SetState("translate");
        RobotTabletBtn.SetInteractivity(true);
        RotateTranslateBtn.SetInteractivity(true);
        DummyBoxing.SetActive(false);
        offsetPosition = Vector3.zero;
        ResetTransformWheel();
        DummyAimBox = dummyAimBox;
        if (DummyAimBox)
            InteractiveObject = ((DummyAimBox) interactiveObject).ActionPoint;
        else
            InteractiveObject = interactiveObject;
        SwitchToTranslate();
        SwitchToRobot();
        if (interactiveObject.GetType() == typeof(ActionPoint3D)) {
            model = ((ActionPoint3D) interactiveObject).GetModelCopy();
            origRotation = TransformConvertor.UnityToROS(((ActionPoint3D) interactiveObject).GetRotation());
        } else if (interactiveObject.GetType() == typeof(DummyBox)) {
            model = ((DummyBox) interactiveObject).GetModelCopy();
            origRotation = interactiveObject.transform.localRotation;
        } else if (DummyAimBox) {
            model = GetPointModel();
            for (int i = 0; i < 4; ++i) {
                bool aimed = PlayerPrefsHelper.LoadBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/PointAimed/" + i, false);
                if (aimed)
                    Dots[i].color = Color.green;
                else
                    Dots[i].color = Color.red;
            }
            DummyBoxing.SetActive(true);
            RobotTabletBtn.SetInteractivity(false);
            RotateTranslateBtn.SetInteractivity(false);
            currentArrowIndex = 0;
            SetArrowVisible(0);
        }
        Debug.LogError(model);
        if (model == null) {
            Hide();
            return;
        }
        
        origPosition = TransformConvertor.UnityToROS(interactiveObject.transform.localPosition);
        
        GameManager.Instance.Gizmo.transform.SetParent(model.transform);
        GameManager.Instance.Gizmo.transform.localPosition = Vector3.zero;
        GameManager.Instance.Gizmo.transform.localRotation = Quaternion.identity;
        GameManager.Instance.Gizmo.SetActive(true);
        enabled = true;
        EditorHelper.EnableCanvasGroup(CanvasGroup, true);
    }

    private GameObject GetPointModel() {
        GameObject m = Instantiate(ProjectManager.Instance.ActionPointSphere, InteractiveObject.transform);
        m.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        return m;
    }

    private void SetArrowVisible(int index) {
        for (int i = 0; i < 4; ++i) {
            Arrows[i].gameObject.SetActive(false);
            DotsBackgrounds[i].color = Color.clear;
        }            
        Arrows[index].gameObject.SetActive(true);
        DotsBackgrounds[index].color = Color.white;
        NextArrowBtn.interactable = index != 3;
        PreviousArrowBtn.interactable = index != 0;
    }
    public void NextArrow() {
        Debug.LogError("next");
        if (currentArrowIndex < 3)
            SetArrowVisible(++currentArrowIndex);
    }

    public void PreviousArrow() {
        Debug.LogError("previous");
        if (currentArrowIndex > 0)
            SetArrowVisible(--currentArrowIndex);

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
                if (RotateTranslateBtn.CurrentState == "rotate")
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.x));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.x));
                break;
            case "y":
                if (RotateTranslateBtn.CurrentState == "rotate")
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.y));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.y));
                break;
            case "z":
                if (RotateTranslateBtn.CurrentState == "rotate")
                    TransformWheel.InitList(ComputeRotationValue(offsetPosition.z));
                else
                    TransformWheel.InitList(ComputePositionValue(offsetPosition.z));
                break;
        }
    }

    public async void SubmitPosition() {
        if (DummyAimBox) {
            Dots[currentArrowIndex].color = Color.green;
            PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/PointAimed/" + currentArrowIndex, true);
            if (dummyPoints[currentArrowIndex] != null) {
                GameManager.Instance.Gizmo.transform.SetParent(GameManager.Instance.Scene.transform);
                Destroy(dummyPoints[currentArrowIndex]);
            }
            dummyPoints[currentArrowIndex] = model;
            model = GetPointModel();

            GameManager.Instance.Gizmo.transform.SetParent(model.transform);
            GameManager.Instance.Gizmo.transform.localPosition = Vector3.zero;
            bool done = true;
            foreach (GameObject p in dummyPoints) {
                if (p == null) {
                    done = false;
                    break;
                }
            }
            if (done) {
                DummyAimBox dummyAimBox = FindObjectOfType<DummyAimBox>();
                if (dummyAimBox != null)
                    dummyAimBox.AimFinished();
            }
        } else if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            try {

                //Vector3 position = interPosition + offsetPosition;
                //model.transform.localPosition = TransformConvertor.ROSToUnity(origRotation) * position;
                if (RobotTabletBtn.CurrentState == "tablet")
                    await WebsocketManager.Instance.UpdateActionPointPosition(InteractiveObject.GetId(), DataHelper.Vector3ToPosition(origPosition + origRotation *(interPosition + offsetPosition)));
                else {
                    await WebsocketManager.Instance.UpdateActionPointUsingRobot(InteractiveObject.GetId(), robotId, endEffector.GetId());
                }
                ((ActionPoint3D) InteractiveObject).SetRotation(model.transform.localRotation);
                origRotation = TransformConvertor.UnityToROS(model.transform.localRotation);
                origPosition = origPosition + origRotation * (interPosition + offsetPosition);
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

    public void ResetPosition(bool manually = false) {
        if (manually && DummyAimBox) {
            for (int i = 0; i < 4; ++i) {
                Dots[i].color = Color.red;
                PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/PointAimed/" + i, false);
                if (dummyPoints[i] != null) {
                    Destroy(dummyPoints[i]);
                }
                DummyAimBox dummyAimBox = FindObjectOfType<DummyAimBox>();
                if (dummyAimBox != null)
                    dummyAimBox.SetVisibility(false);
            }
        }
        offsetPosition = Vector3.zero;
        interPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;
        interRotation = Quaternion.identity;
        if (InteractiveObject.GetType() == typeof(ActionPoint3D)) {
            origRotation = TransformConvertor.UnityToROS(((ActionPoint3D) InteractiveObject).GetRotation());
        }
        ResetTransformWheel();
    }
}
