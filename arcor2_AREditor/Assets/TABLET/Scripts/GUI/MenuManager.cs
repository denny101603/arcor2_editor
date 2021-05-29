using System;
using System.Collections.Generic;
using Base;
using DanielLochner.Assets.SimpleSideMenu;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : Base.Singleton<MenuManager> {
    public SimpleSideMenu ActionObjectMenuSceneEditor, ActionPointMenu, MainMenu, NewObjectTypeMenu,
        ActionObjectMenuProjectEditor, EditorSettingsMenu, ActionPointAimingMenu, NotificationMenu,
        AddOrientationMenu, AddJointsMenu, OrientationJointsDetailMenu;
    SimpleSideMenu MenuOpened;
    public GameObject ActionPointMenuPrefab, ButtonPrefab;

    public OutputTypeDialog OutputTypeDialog;
    public ConnectionSelectorDialog ConnectionSelectorDialog;
    public LeftMenuScene LeftMenuScene;
    public LeftMenuProject LeftMenuProject;
    public Dialog InputDialog, ConfirmationDialog, InputDialogWithToggle, EditConstantDialog;


    private void Start() {
        GameManager.Instance.OnCloseProject += OnCloseSceneOrProject;
        GameManager.Instance.OnCloseScene += OnCloseSceneOrProject;
    }

    private void OnCloseSceneOrProject(object sender, EventArgs e) {
        HideAllMenus();
    }

    public bool IsAnyMenuOpened {
        get;
        private set;
    } = false;

    private bool CheckIsAnyMenuOpened() {
        return ActionObjectMenuSceneEditor.CurrentState == SimpleSideMenu.State.Open ||
            ActionPointMenu.CurrentState == SimpleSideMenu.State.Open ||
            MainMenu.CurrentState == SimpleSideMenu.State.Open ||
            NewObjectTypeMenu.CurrentState == SimpleSideMenu.State.Open ||
            EditorSettingsMenu.CurrentState == SimpleSideMenu.State.Open ||
            ActionObjectMenuProjectEditor.CurrentState == SimpleSideMenu.State.Open ||
            NotificationMenu.CurrentState == SimpleSideMenu.State.Open ||
            ActionPointAimingMenu.CurrentState == SimpleSideMenu.State.Open ||
            AddOrientationMenu.CurrentState == SimpleSideMenu.State.Open ||
            AddJointsMenu.CurrentState == SimpleSideMenu.State.Open ||
            OrientationJointsDetailMenu.CurrentState == SimpleSideMenu.State.Open;
    }
    public bool CheckIsAnyRightMenuOpened() {
        return ActionObjectMenuSceneEditor.CurrentState == SimpleSideMenu.State.Open ||
            ActionPointMenu.CurrentState == SimpleSideMenu.State.Open ||
            NewObjectTypeMenu.CurrentState == SimpleSideMenu.State.Open ||
            EditorSettingsMenu.CurrentState == SimpleSideMenu.State.Open ||
            ActionObjectMenuProjectEditor.CurrentState == SimpleSideMenu.State.Open ||
            NotificationMenu.CurrentState == SimpleSideMenu.State.Open ||
            ActionPointAimingMenu.CurrentState == SimpleSideMenu.State.Open ||
            AddOrientationMenu.CurrentState == SimpleSideMenu.State.Open ||
            AddJointsMenu.CurrentState == SimpleSideMenu.State.Open ||
            OrientationJointsDetailMenu.CurrentState == SimpleSideMenu.State.Open;
    }


    public void ShowMenu(SimpleSideMenu menu) {
        Debug.Assert(menu != null); 
        HideAllMenus();
        menu.Open();
        menu.gameObject.GetComponent<IMenu>().UpdateMenu();
        MenuOpened = menu;
    }

    public void HideAllMenus() {
        if (ActionObjectMenuSceneEditor.CurrentState == SimpleSideMenu.State.Open) {
            ActionObjectMenuSceneEditor.Close();
        }
        if (ActionObjectMenuProjectEditor.CurrentState == SimpleSideMenu.State.Open) {
            ActionObjectMenuProjectEditor.Close();
        }
        if (ActionPointMenu.CurrentState == SimpleSideMenu.State.Open) {
            ActionPointMenu.Close();
        }
        if (ActionPointAimingMenu.CurrentState == SimpleSideMenu.State.Open) {
            ActionPointAimingMenu.Close();
        }
        if (AddOrientationMenu.CurrentState == SimpleSideMenu.State.Open) {
            AddOrientationMenu.Close();
        }
        if (AddJointsMenu.CurrentState == SimpleSideMenu.State.Open) {
            AddJointsMenu.Close();
        }
        if (OrientationJointsDetailMenu.CurrentState == SimpleSideMenu.State.Open) {
            OrientationJointsDetailMenu.Close();
        }
        if (MainMenu.CurrentState == SimpleSideMenu.State.Open) {
            MainMenu.Close();
        }
        ConfirmationDialog.Close();
        InputDialog.Close();
        InputDialogWithToggle.Close();
        EditConstantDialog.Close();
    }

    public void DisableAllMenus() {
        MainMenu.gameObject.SetActive(false);
        ActionObjectMenuSceneEditor.gameObject.SetActive(false);
        ActionPointMenu.gameObject.SetActive(false);
        ActionObjectMenuProjectEditor.gameObject.SetActive(false);
        ActionPointAimingMenu.gameObject.SetActive(false);
        AddOrientationMenu.gameObject.SetActive(false);
        AddJointsMenu.gameObject.SetActive(false);
        OrientationJointsDetailMenu.gameObject.SetActive(false);
    }

    public void EnableAllWindows() {
        MainMenu.gameObject.SetActive(true);
        ActionObjectMenuSceneEditor.gameObject.SetActive(true);
        ActionPointMenu.gameObject.SetActive(true);
        ActionObjectMenuProjectEditor.gameObject.SetActive(true);
        ActionPointAimingMenu.gameObject.SetActive(true);
        AddOrientationMenu.gameObject.SetActive(true);
        AddJointsMenu.gameObject.SetActive(true);
        OrientationJointsDetailMenu.gameObject.SetActive(true);
    }

    public void HideMenu() {
        if (MenuOpened != null) {
            MenuOpened.Close();
            MenuOpened = null;
        }
    }

    public void OnMenuStateChanged(SimpleSideMenu menu) {
        switch (menu.CurrentState) {
            case SimpleSideMenu.State.Open:
                IsAnyMenuOpened = true;
                GameManager.Instance.InvokeSceneInteractable(false);
                break;
            case SimpleSideMenu.State.Closed:
                if (!CheckIsAnyMenuOpened()) {
                    IsAnyMenuOpened = false;
                    // no menus are opened, scene should be interactable
                    // invoke an event from GameManager to let everyone know, that scene is interactable
                    GameManager.Instance.InvokeSceneInteractable(true);
                }

                if (menu == ActionPointMenu) {
                    menu.GetComponent<ActionPointMenu>().HideMenu();
                } else if (menu == ActionObjectMenuSceneEditor) {
                    menu.GetComponent<ActionObjectMenuSceneEditor>().HideMenu();
                } else if (menu == ActionObjectMenuProjectEditor) {
                    menu.GetComponent<ActionObjectMenuProjectEditor>().HideMenu();
                } else if (menu == ActionPointAimingMenu) {
                    menu.GetComponent<ActionPointAimingMenu>().Close();
                } else if (menu == OrientationJointsDetailMenu) {
                    menu.GetComponent<OrientationJointsDetailMenu>().HideMenu();
                }

                break;
        }
    }
}
