using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using Base;
using IO.Swagger.Model;
using UnityEngine;
using UnityEngine.UI;
using static Base.GameManager;

[RequireComponent(typeof(CanvasGroup))]
public class LeftMenu : Base.Singleton<LeftMenu> {

    private CanvasGroup CanvasGroup;

    public Button FocusButton, RobotButton, AddButton, SettingsButton, HomeButton;
    public Button AddMeshButton, MoveButton, RemoveButton, SetActionPointParentButton, AddActionButton, RenameButton, CalibrationButton;
    public GameObject HomeButtons, SettingsButtons, AddButtons, MeshPicker, ActionPicker;
    public RenameDialog RenameDialog;
    public TMPro.TMP_Text ProjectName, SelectedObjectText;

    private void Awake() {
        CanvasGroup = GetComponent<CanvasGroup>();
        GameManager.Instance.OnEditorStateChanged += OnEditorStateChanged;
    }

    private bool updateButtonsInteractivity = false;

    private bool requestingObject = false;

    private void OnEditorStateChanged(object sender, EditorStateEventArgs args) {
        UpdateVisibility();

        switch (args.Data) {
            case GameManager.EditorStateEnum.Normal:
                requestingObject = false;
                updateButtonsInteractivity = true;
                break;
            case GameManager.EditorStateEnum.InteractionDisabled:
                updateButtonsInteractivity = false;
                break;
            case GameManager.EditorStateEnum.Closed:
                updateButtonsInteractivity = false;
                break;
            case EditorStateEnum.SelectingAction:
            case EditorStateEnum.SelectingActionInput:
            case EditorStateEnum.SelectingActionObject:
            case EditorStateEnum.SelectingActionOutput:
            case EditorStateEnum.SelectingActionPoint:
            case EditorStateEnum.SelectingActionPointParent:
                requestingObject = true;
                break;
        }
    }

    private void Update() {
        //UpdateVisibility();
        if (!updateButtonsInteractivity)
            return;

        if (MenuManager.Instance.CheckIsAnyRightMenuOpened()) {
            SetActiveSubmenu(LeftMenuSelection.None);
            FocusButton.GetComponent<Image>().enabled = false;

            RobotButton.interactable = false;
            AddButton.interactable = false;
            SettingsButton.interactable = false;
            HomeButton.interactable = false;
            return;
        }

        FocusButton.GetComponent<Image>().enabled = SelectorMenu.Instance.gameObject.activeSelf;

        RobotButton.interactable = true;
        AddButton.interactable = true;
        SettingsButton.interactable = true;
        HomeButton.interactable = true;

        

        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (requestingObject || selectedObject == null) {
            SelectedObjectText.text = "";
            MoveButton.interactable = false;
            RemoveButton.interactable = false;
            SetActionPointParentButton.interactable = false;
            AddActionButton.interactable = false;
            RenameButton.interactable = false;
            CalibrationButton.interactable = false;
        } else {
            SelectedObjectText.text = selectedObject.GetName() + "\n" + selectedObject.GetType();

            MoveButton.interactable = selectedObject.Movable();
            RemoveButton.interactable = selectedObject.Removable();

            SetActionPointParentButton.interactable = selectedObject is ActionPoint3D;
            AddActionButton.interactable = selectedObject is ActionPoint3D;
            RenameButton.interactable = selectedObject is ActionPoint3D ||
                selectedObject is DummyBox ||
                selectedObject is Action3D;

            CalibrationButton.interactable = selectedObject.GetType() == typeof(Recalibrate) ||
                selectedObject.GetType() == typeof(CreateAnchor);
        }

        if (SceneManager.Instance.SceneMeta != null)
            ProjectName.text = "Project: \n" + SceneManager.Instance.SceneMeta.Name;
    }

    public void UpdateVisibility() {
        if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.MainScreen ||
            GameManager.Instance.GetGameState() == GameManager.GameStateEnum.Disconnected ||
            MenuManager.Instance.MainMenu.CurrentState == DanielLochner.Assets.SimpleSideMenu.SimpleSideMenu.State.Open) {
            UpdateVisibility(false);
        } else {
            UpdateVisibility(true);
        }
    }

    public void UpdateVisibility(bool visible) {
        CanvasGroup.interactable = visible;
        CanvasGroup.blocksRaycasts = visible;
        CanvasGroup.alpha = visible ? 1 : 0;
    }



    public void FocusButtonClick() {
        MenuManager.Instance.HideAllMenus();
        SelectorMenu.Instance.gameObject.SetActive(true);
        TransformMenu.Instance.Hide();
        SetActiveSubmenu(LeftMenuSelection.None);

    }

    public void RobotButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.None);
        Notifications.Instance.ShowNotification("Not implemented", "");

    }

    public void AddButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Add, !AddButtons.activeInHierarchy);

    }

    public void SettingsButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Settings, !SettingsButtons.activeInHierarchy);


    }

    public void HomeButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Home, !HomeButtons.activeInHierarchy);
    }

    #region Add submenu button click methods

    public void CopyObjectClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if (selectedObject.GetType() == typeof(ActionPoint3D)) {
            WebsocketManager.Instance.CopyActionPoint(selectedObject.GetId(), null);
        } else if (selectedObject.GetType() == typeof(Action3D)) {
            Action3D action = (Action3D) selectedObject;
            List<ActionParameter> parameters = new List<ActionParameter>();
            foreach (Base.Parameter p in action.Parameters.Values) {
                parameters.Add(new ActionParameter(p.ParameterMetadata.Name, p.ParameterMetadata.Type, p.Value));
            }
            WebsocketManager.Instance.AddAction(action.ActionPoint.GetId(), parameters, action.ActionProvider.GetProviderId() + "/" + action.Metadata.Name, action.GetName() + "_copy", action.GetFlows());
        } else if (selectedObject.GetType() == typeof(DummyBox)) {
            DummyBox box = ProjectManager.Instance.AddDummyBox(selectedObject.GetName());
            box.transform.position = selectedObject.transform.position;
            box.transform.rotation = selectedObject.transform.rotation;
            box.SetDimensions(((DummyBox) selectedObject).GetDimensions());
            SelectorMenu.Instance.SetSelectedObject(box, true);
        }
    }

    public void AddActionClick() {
        if (AddActionButton.GetComponent<Image>().enabled) {
            AddActionButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            ActionPicker.SetActive(false);
        } else {
            AddActionButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            ActionPicker.SetActive(true);
        }
    }

    public void AddActionPointClick() {
        GameManager.Instance.AddActionPointExperiment();
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void AddMeshClick() {
        if (AddMeshButton.GetComponent<Image>().enabled) {
            AddMeshButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            MeshPicker.SetActive(false);
        } else {
            AddMeshButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            MeshPicker.SetActive(true);
        }
        
    }

    #endregion

    #region Settings submenu button click methods

    public void SetActionPointParentClick() {
        Notifications.Instance.ShowNotification("Not implemented", "");

    }

    public void MoveClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if (MoveButton.GetComponent<Image>().enabled) {
            MoveButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            TransformMenu.Instance.Hide();
        } else {
            MoveButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            //selectedObject.StartManipulation();
            TransformMenu.Instance.Show(selectedObject);
        }

    }

    public void RenameClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        UpdateVisibility(false);
        SelectorMenu.Instance.gameObject.SetActive(false);

        RenameDialog.Init(selectedObject);
        RenameDialog.Open();
    }

    public void RemoveClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        selectedObject.Remove();
        SetActiveSubmenu(LeftMenuSelection.None);
    }


    #endregion

    #region Home submenu button click methods

    public void CalibrationButtonClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if (selectedObject.GetType() == typeof(Recalibrate)) {
            ((Recalibrate) selectedObject).OnClick(Clickable.Click.TOUCH);
        } else if (selectedObject.GetType() == typeof(CreateAnchor)) {
            ((CreateAnchor) selectedObject).OnClick(Clickable.Click.TOUCH);
        }

        SetActiveSubmenu(LeftMenuSelection.None);
    }

    #endregion

    #region Mesh picker click methods

    public void BlueBoxClick() {
        Notifications.Instance.ShowNotification("Not implemented", "");
        SelectorMenu.Instance.gameObject.SetActive(true);
        MeshPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void CubeClick() {
        SelectorMenu.Instance.gameObject.SetActive(true);
        MeshPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
        SelectorMenu.Instance.SetSelectedObject(ProjectManager.Instance.AddDummyBox("Cube"), true);
    }

    #endregion

    #region Action picker click methods
    public void ActionMoveToClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("MoveTo");
        NamedOrientation o = ((ActionPoint3D) selectedObject).GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "move_type", type: "string_enum", value: "\"JOINTS\""),
            new ActionParameter(name: "velocity", type: "double", value: "30.0"),
            new ActionParameter(name: "acceleration", type: "double", value: "50.0")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);
        WebsocketManager.Instance.AddAction(selectedObject.GetId(), parameters, robotId + "/move", ProjectManager.Instance.GetFreeActionName("MoveTo"), robot.GetActionMetadata("move").GetFlows(name));
        SelectorMenu.Instance.gameObject.SetActive(true);
        ActionPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void ActionPickClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("MoveTo");
        NamedOrientation o = ((ActionPoint3D) selectedObject).GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "pick_pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "vertical_offset", type: "double", value: "0.05")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);
        WebsocketManager.Instance.AddAction(selectedObject.GetId(), parameters, robotId + "/pick", ProjectManager.Instance.GetFreeActionName("Pick"), robot.GetActionMetadata("pick").GetFlows(name));
        SelectorMenu.Instance.gameObject.SetActive(true);
        ActionPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void ActionReleaseClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        string robotId = "";
        foreach (IRobot r in SceneManager.Instance.GetRobots()) {
            robotId = r.GetId();
        }
        string name = ProjectManager.Instance.GetFreeActionName("MoveTo");
        NamedOrientation o = ((ActionPoint3D) selectedObject).GetFirstOrientation();
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "place_pose", type: "pose", value: "\"" + o.Id + "\""),
            new ActionParameter(name: "vertical_offset", type: "double", value: "0.05")
        };
        IActionProvider robot = SceneManager.Instance.GetActionObject(robotId);
        WebsocketManager.Instance.AddAction(selectedObject.GetId(), parameters, robotId + "/place", ProjectManager.Instance.GetFreeActionName("Place"), robot.GetActionMetadata("place").GetFlows(name));
        SelectorMenu.Instance.gameObject.SetActive(true);
        ActionPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }
    #endregion


    private void SetActiveSubmenu(LeftMenuSelection which, bool active = true) {
        DeactivateAllSubmenus();
        if (!active)
            return;
        switch (which) {
            case LeftMenuSelection.None:
                break;
            case LeftMenuSelection.Add:
                AddButtons.SetActive(active);
                AddButton.GetComponent<Image>().enabled = active;
                break;
            case LeftMenuSelection.Settings:
                SettingsButtons.SetActive(active);
                SettingsButton.GetComponent<Image>().enabled = active;
                break;
            case LeftMenuSelection.Home:
                HomeButtons.SetActive(active);
                HomeButton.GetComponent<Image>().enabled = active;
                break;
        }
    }

    private void DeactivateAllSubmenus() {
        SelectorMenu.Instance.gameObject.SetActive(true);
        MeshPicker.SetActive(false);
        ActionPicker.SetActive(false);

        HomeButtons.SetActive(false);
        SettingsButtons.SetActive(false);
        AddButtons.SetActive(false);

        FocusButton.GetComponent<Image>().enabled = false;
        RobotButton.GetComponent<Image>().enabled = false;
        AddButton.GetComponent<Image>().enabled = false;
        SettingsButton.GetComponent<Image>().enabled = false;
        HomeButton.GetComponent<Image>().enabled = false;

        AddMeshButton.GetComponent<Image>().enabled = false;
        MoveButton.GetComponent<Image>().enabled = false;
        AddActionButton.GetComponent<Image>().enabled = false;
    }
}

public enum LeftMenuSelection{
    None, Add, Settings, Home
}

