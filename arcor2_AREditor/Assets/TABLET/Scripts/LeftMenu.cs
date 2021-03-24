using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using UnityEngine;
using UnityEngine.UI;
using static Base.GameManager;

[RequireComponent(typeof(CanvasGroup))]
public class LeftMenu : Base.Singleton<LeftMenu> {

    private CanvasGroup CanvasGroup;

    public Button FavoritesButton, RobotButton, AddButton, SettingsButton, HomeButton;
    public Button AddMeshButton, MoveButton, MoveButton2, RemoveButton, SetActionPointParentButton,
        AddActionButton, AddActionButton2, RenameButton, CalibrationButton, ResizeCubeButton,
        AddConnectionButton, AddConnectionButton2, RunButton, RunButton2; //Buttons with number 2 are duplicates in favorites submenu
    public GameObject FavoritesButtons, HomeButtons, SettingsButtons, AddButtons, MeshPicker, ActionPicker;
    public RenameDialog RenameDialog;
    public CubeSizeDialog CubeSizeDialog;
    public ConfirmationDialog ConfirmationDialog;
    public TMPro.TMP_Text ProjectName, SelectedObjectText;

    private bool isVisibilityForced = false;
    private ActionPoint3D selectedActionPoint;
    private LeftMenuSelection currentSubmenuOpened;

    private void Awake() {
        CanvasGroup = GetComponent<CanvasGroup>();
        GameManager.Instance.OnEditorStateChanged += OnEditorStateChanged;
    }

    private bool updateButtonsInteractivity = false;

    private bool requestingObject = false;

    private void OnEditorStateChanged(object sender, EditorStateEventArgs args) {
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
        if (!isVisibilityForced)
            UpdateVisibility();

        if (!updateButtonsInteractivity)
            return;

        if (MenuManager.Instance.CheckIsAnyRightMenuOpened()) {
            SetActiveSubmenu(LeftMenuSelection.Favorites);

            RobotButton.interactable = false;
            AddButton.interactable = false;
            SettingsButton.interactable = false;
            HomeButton.interactable = false;
            return;
        }

        RobotButton.interactable = true;
        AddButton.interactable = true;
        SettingsButton.interactable = true;
        HomeButton.interactable = true;



        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (requestingObject || selectedObject == null) {
            SelectedObjectText.text = "";
            MoveButton.interactable = false;
            MoveButton2.interactable = false;
            RemoveButton.interactable = false;
            SetActionPointParentButton.interactable = false;
            AddActionButton.interactable = false;
            AddActionButton2.interactable = false;
            RenameButton.interactable = false;
            CalibrationButton.interactable = false;
            ResizeCubeButton.interactable = false;
            AddConnectionButton.interactable = false;
            AddConnectionButton2.interactable = false;
            RunButton.interactable = false;
            RunButton2.interactable = false;
        } else {
            SelectedObjectText.text = selectedObject.GetName() + "\n" + selectedObject.GetType();

            MoveButton.interactable = selectedObject.Movable();
            MoveButton2.interactable = selectedObject.Movable();
            RemoveButton.interactable = selectedObject.Removable();

            SetActionPointParentButton.interactable = selectedObject is ActionPoint3D;
            AddActionButton.interactable = selectedObject is ActionPoint3D;
            AddActionButton2.interactable = selectedObject is ActionPoint3D;
            RenameButton.interactable = selectedObject is ActionPoint3D ||
                selectedObject is DummyBox ||
                selectedObject is Action3D;
            CalibrationButton.interactable = selectedObject.GetType() == typeof(Recalibrate) ||
                selectedObject.GetType() == typeof(CreateAnchor);
            ResizeCubeButton.interactable = selectedObject is DummyBox;
            AddConnectionButton.interactable = (selectedObject.GetType() == typeof(Action3D) ||
                selectedObject.GetType() == typeof(StartAction)) && !((Action3D) selectedObject).Output.ConnectionExists();
            AddConnectionButton2.interactable = AddConnectionButton.interactable;
            RunButton.interactable = selectedObject.GetType() == typeof(StartAction) || selectedObject.GetType() == typeof(Action3D) || selectedObject.GetType() == typeof(ActionPoint3D);
            RunButton2.interactable = RunButton.interactable;
        }

        if (SceneManager.Instance.SceneMeta != null)
            ProjectName.text = "Project: \n" + SceneManager.Instance.SceneMeta.Name;
    }

    public void UpdateVisibility() {
        if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.MainScreen ||
            GameManager.Instance.GetGameState() == GameManager.GameStateEnum.Disconnected ||
            GameManager.Instance.GetEditorState() == EditorStateEnum.SelectingActionPointParent ||
            MenuManager.Instance.MainMenu.CurrentState == DanielLochner.Assets.SimpleSideMenu.SimpleSideMenu.State.Open) {
            UpdateVisibility(false);
        } else {
            UpdateVisibility(true);
        }
    }

    public void UpdateVisibility(bool visible, bool force = false) {
        isVisibilityForced = force;

        CanvasGroup.interactable = visible;
        CanvasGroup.blocksRaycasts = visible;
        CanvasGroup.alpha = visible ? 1 : 0;
    }



    public void FavoritesButtonClick() {
        MenuManager.Instance.HideAllMenus();
        SetActiveSubmenu(LeftMenuSelection.Favorites);

    }

    public void RobotButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.None);
        Notifications.Instance.ShowNotification("Not implemented", "");

    }

    public void AddButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Add);

    }

    public void SettingsButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Settings);


    }

    public void HomeButtonClick() {
        SetActiveSubmenu(LeftMenuSelection.Home);
    }

    #region Add submenu button click methods

    public void CopyObjectClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if (selectedObject.GetType() == typeof(ActionPoint3D)) {
            ProjectManager.Instance.SelectAPNameWhenCreated = "copy_of_" + selectedObject.GetName();
            WebsocketManager.Instance.CopyActionPoint(selectedObject.GetId(), null);
        } else if (selectedObject.GetType() == typeof(Action3D)) {
            Action3D action = (Action3D) selectedObject;
            List<ActionParameter> parameters = new List<ActionParameter>();
            foreach (Base.Parameter p in action.Parameters.Values) {
                parameters.Add(new ActionParameter(p.ParameterMetadata.Name, p.ParameterMetadata.Type, p.Value));
            }
            WebsocketManager.Instance.AddAction(action.ActionPoint.GetId(), parameters, action.ActionProvider.GetProviderId() + "/" + action.Metadata.Name, action.GetName() + "_copy", action.GetFlows());
        } else if (selectedObject.GetType() == typeof(DummyBox)) {
            DummyBox box = ProjectManager.Instance.AddDummyBox(selectedObject.GetName() + "_copy");
            box.transform.position = selectedObject.transform.position;
            box.transform.rotation = selectedObject.transform.rotation;
            box.SetDimensions(((DummyBox) selectedObject).GetDimensions());
            SelectorMenu.Instance.SetSelectedObject(box, true);
        }
    }

    public void AddConnectionClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;
        if ((selectedObject.GetType() == typeof(Action3D) ||
                selectedObject.GetType() == typeof(StartAction) ||
                selectedObject.GetType() == typeof(EndAction))) {
            ((Action3D) selectedObject).OnClick(Clickable.Click.TOUCH);
        }
    }

    public void AddActionClick() {
        //was clicked the button in favorites or settings submenu?
        Button clickedButton = AddActionButton;
        if (currentSubmenuOpened == LeftMenuSelection.Favorites) {
            clickedButton = AddActionButton2;
        }

        if (!SelectorMenu.Instance.gameObject.activeSelf && !clickedButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(currentSubmenuOpened); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        if (clickedButton.GetComponent<Image>().enabled) {
            clickedButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            ActionPicker.SetActive(false);
        } else {
            clickedButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            ActionPicker.SetActive(true);
        }
    }

    public void AddActionPointClick() {
        GameManager.Instance.AddActionPointExperiment();
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
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null || !(selectedObject is ActionPoint3D))
            return;

        if (!SelectorMenu.Instance.gameObject.activeSelf) { //other menu/dialog opened
            SetActiveSubmenu(LeftMenuSelection.None); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        selectedActionPoint = (ActionPoint3D) selectedObject;
        Action<object> action = AssignToParent;
        GameManager.Instance.RequestObject(GameManager.EditorStateEnum.SelectingActionPointParent, action,
            "Select new parent (action object)", ValidateParent, UntieActionPointParent);
    }

    public void MoveClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        if (selectedObject.GetType() == typeof(PuckInput) ||
            selectedObject.GetType() == typeof(PuckOutput)) {
            selectedObject.StartManipulation();
            return;
        }


        //was clicked the button in favorites or settings submenu?
        Button clickedButton = MoveButton;
        if (currentSubmenuOpened == LeftMenuSelection.Favorites)
            clickedButton = MoveButton2;

        if (!SelectorMenu.Instance.gameObject.activeSelf && !clickedButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(currentSubmenuOpened); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        if (clickedButton.GetComponent<Image>().enabled) {
            clickedButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            TransformMenu.Instance.Hide();
        } else {
            clickedButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            //selectedObject.StartManipulation();
            TransformMenu.Instance.Show(selectedObject, selectedObject.GetType() == typeof(DummyAimBox));
        }

    }

    public void ResizeCubeClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null || !(selectedObject is DummyBox))
            return;

        if (!SelectorMenu.Instance.gameObject.activeSelf && !ResizeCubeButton.GetComponent<Image>().enabled) { //other menu/dialog opened
            SetActiveSubmenu(LeftMenuSelection.Settings); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        if (ResizeCubeButton.GetComponent<Image>().enabled) {
            ResizeCubeButton.GetComponent<Image>().enabled = false;
            SelectorMenu.Instance.gameObject.SetActive(true);
            CubeSizeDialog.Cancel(false);
        } else {
            ResizeCubeButton.GetComponent<Image>().enabled = true;
            SelectorMenu.Instance.gameObject.SetActive(false);
            CubeSizeDialog.Init((DummyBox) selectedObject);
            CubeSizeDialog.Open();
        }
    }

    public void RenameClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        if (!SelectorMenu.Instance.gameObject.activeSelf) { //other menu/dialog opened
            SetActiveSubmenu(LeftMenuSelection.Settings); //close all other opened menus/dialogs and takes care of red background of buttons
        }

        UpdateVisibility(false, true);
        SelectorMenu.Instance.gameObject.SetActive(false);

        RenameDialog.Init(selectedObject);
        RenameDialog.Open();
    }

    public void RemoveClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        UpdateVisibility(false, true);
        SelectorMenu.Instance.gameObject.SetActive(false);
        ConfirmationDialog.Open("Remove object",
                         "Are you sure you want to remove " + selectedObject.GetName() + "?",
                         () => {
                             selectedObject.Remove();
                             UpdateVisibility(true);
                             SelectorMenu.Instance.gameObject.SetActive(true);
                             ConfirmationDialog.Close();
                         },
                         () => {
                             UpdateVisibility(true);
                             SelectorMenu.Instance.gameObject.SetActive(true);
                             ConfirmationDialog.Close();
                             });
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

    public async void RunButtonClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null)
            return;

        if (selectedObject.GetType() == typeof(StartAction)) {
            await GameManager.Instance.SaveProject();
            GameManager.Instance.ShowLoadingScreen("Running project", true);
            try {
                await Base.WebsocketManager.Instance.TemporaryPackage();
                MenuManager.Instance.MainMenu.Close();
            } catch (RequestFailedException ex) {
                Base.Notifications.Instance.ShowNotification("Failed to run temporary package", "");
                Debug.LogError(ex);
                GameManager.Instance.HideLoadingScreen(true);
            }
        } else if (selectedObject.GetType() == typeof(Action3D)) {
            try {
                await WebsocketManager.Instance.ExecuteAction(selectedObject.GetId(), false);
            } catch (RequestFailedException ex) {
                Notifications.Instance.ShowNotification("Failed to execute action", ex.Message);
                return;
            }
        } else if (selectedObject.GetType() == typeof(ActionPoint3D)) {
            string robotId = "";
            foreach (IRobot r in SceneManager.Instance.GetRobots()) {
                robotId = r.GetId();
            }
            NamedOrientation o = ((ActionPoint3D) selectedObject).GetFirstOrientation();
            IRobot robot = SceneManager.Instance.GetRobot(robotId);
            await WebsocketManager.Instance.MoveToActionPointOrientation(robot.GetId(), (await robot.GetEndEffectorIds())[0], 0.5m, o.Id, false);
        }            
        
    }

    #endregion

    #region Mesh picker click methods

    public void BlueBoxClick() {
        SelectorMenu.Instance.gameObject.SetActive(true);
        MeshPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
        SelectorMenu.Instance.SwitchToNoPose();
        SelectorMenu.Instance.SetSelectedObject(ProjectManager.Instance.AddDummyAimBox(), true);
        ToggleGroupIconButtons.Instance.SelectButton(ToggleGroupIconButtons.Instance.Buttons[2]);
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


    public void SetActiveSubmenu(LeftMenuSelection which, bool active = true) {
        DeactivateAllSubmenus();
        currentSubmenuOpened = which;
        if (!active)
            return;
        switch (which) {
            case LeftMenuSelection.None:
                break;
            case LeftMenuSelection.Favorites:
                FavoritesButtons.SetActive(active);
                FavoritesButton.GetComponent<Image>().enabled = active;
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
        CubeSizeDialog.Cancel(false);
        if (RenameDialog.isActiveAndEnabled)
            RenameDialog.Close();
        TransformMenu.Instance.Hide();

        MeshPicker.SetActive(false);
        ActionPicker.SetActive(false);

        FavoritesButtons.SetActive(false);
        HomeButtons.SetActive(false);
        SettingsButtons.SetActive(false);
        AddButtons.SetActive(false);

        FavoritesButton.GetComponent<Image>().enabled = false;
        RobotButton.GetComponent<Image>().enabled = false;
        AddButton.GetComponent<Image>().enabled = false;
        SettingsButton.GetComponent<Image>().enabled = false;
        HomeButton.GetComponent<Image>().enabled = false;

        AddMeshButton.GetComponent<Image>().enabled = false;
        MoveButton.GetComponent<Image>().enabled = false;
        MoveButton2.GetComponent<Image>().enabled = false;
        AddActionButton.GetComponent<Image>().enabled = false;
        AddActionButton2.GetComponent<Image>().enabled = false;
        ResizeCubeButton.GetComponent<Image>().enabled = false;
    }

    private async Task<RequestResult> ValidateParent(object selectedParent) {
        IActionPointParent parent = (IActionPointParent) selectedParent;
        RequestResult result = new RequestResult(true, "");
        if (parent.GetId() == selectedActionPoint.GetId()) {
            result.Success = false;
            result.Message = "Action point cannot be its own parent!";
        }

        return result;
    }
    private async void AssignToParent(object selectedObject) {
        IActionPointParent parent = (IActionPointParent) selectedObject;
        
        if (parent == null)
            return;
        string id = "";
        if (parent.GetType() == typeof(DummyAimBox)) {
            id = ((DummyAimBox) selectedObject).ActionPoint.GetId();
        } else {
            id = parent.GetId();
        }
        bool result = await Base.GameManager.Instance.UpdateActionPointParent(selectedActionPoint, id);
        if (result) {
            //
        }
    }

    private async void UntieActionPointParent() {
        Debug.Assert(selectedActionPoint != null);
        if (selectedActionPoint.GetParent() == null)
            return;

        if (await Base.GameManager.Instance.UpdateActionPointParent(selectedActionPoint, "")) {
            Notifications.Instance.ShowToastMessage("Parent of action point untied");
        }
    }
}

public enum LeftMenuSelection{
    None, Favorites, Add, Settings, Home
}

