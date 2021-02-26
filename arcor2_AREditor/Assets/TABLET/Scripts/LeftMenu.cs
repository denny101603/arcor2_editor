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
    public Button AddMeshButton, MoveButton, RemoveButton, SetActionPointParentButton,
        AddActionButton, RenameButton, CalibrationButton, ResizeCubeButton;
    public GameObject HomeButtons, SettingsButtons, AddButtons, MeshPicker, ActionPicker;
    public RenameDialog RenameDialog;
    public CubeSizeDialog CubeSizeDialog;
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
            ResizeCubeButton.interactable = false;
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
            ResizeCubeButton.interactable = selectedObject is DummyBox;
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

    public void ResizeCubeClick() {
        InteractiveObject selectedObject = SelectorMenu.Instance.GetSelectedObject();
        if (selectedObject is null || !(selectedObject is DummyBox))
            return;
        //todo cervene podbarveni a toggle chování
        SelectorMenu.Instance.gameObject.SetActive(false);

        CubeSizeDialog.Init((DummyBox) selectedObject);
        CubeSizeDialog.Open();
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
        Notifications.Instance.ShowNotification("Not implemented", "");
        SelectorMenu.Instance.gameObject.SetActive(true);
        ActionPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void ActionPickClick() {
        Notifications.Instance.ShowNotification("Not implemented", "");
        SelectorMenu.Instance.gameObject.SetActive(true);
        ActionPicker.SetActive(false);
        SetActiveSubmenu(LeftMenuSelection.None);
    }

    public void ActionReleaseClick() {
        Notifications.Instance.ShowNotification("Not implemented", "");
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

