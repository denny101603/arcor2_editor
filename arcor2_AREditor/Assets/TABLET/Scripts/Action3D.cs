using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(OutlineOnClick))]
public class Action3D : Base.Action {

    public TextMeshPro NameText;
    public Renderer Visual;

    private Color32 colorDefault = new Color32(229, 215, 68, 255);
    private Color32 colorRunnning = new Color32(255, 0, 255, 255);

    private bool selected = false;
    private OutlineOnClick outlineOnClick;

    private void Start() {
        GameManager.Instance.OnStopPackage += OnProjectStop;
        outlineOnClick = GetComponent<OutlineOnClick>();
    }

    private void OnEnable() {
        GameManager.Instance.OnSceneInteractable += OnDeselect;
    }

    private void OnDisable() {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnSceneInteractable -= OnDeselect;
        }
    }

    private void OnProjectStop(object sender, System.EventArgs e) {
        StopAction();
    }

    public override void RunAction() {
        Visual.material.color = colorRunnning;
    }

    public override void StopAction() {
        if (Visual != null) {
            Visual.material.color = colorDefault;
        }
    }

    public override void UpdateName(string newName) {
        base.UpdateName(newName);
        NameText.text = newName;
    }

    public override void ActionUpdateBaseData(IO.Swagger.Model.Action aData = null) {
        base.ActionUpdateBaseData(aData);
        NameText.text = aData.Name;
    }

    public bool CheckClick() {
        if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingAction) {
            GameManager.Instance.ObjectSelected(this);
            return false;
        }
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal) {
            return false;
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
            Notifications.Instance.ShowNotification("Not allowed", "Editation of action only allowed in project editor");
            return false;
        }
        return true;
    }

    public override void OnClick(Click type) {
        if (CheckClick())
            return;
        if (type == Click.MOUSE_RIGHT_BUTTON || (type == Click.TOUCH && !(ControlBoxManager.Instance.UseGizmoMove || ControlBoxManager.Instance.UseGizmoRotate))) {
            ActionMenu.Instance.CurrentAction = this;
            MenuManager.Instance.ShowMenu(MenuManager.Instance.PuckMenu);
            selected = true;
            ActionPoint.HighlightAP(true);
        }
    }

    private void OnDeselect(object sender, EventArgs e) {
        if (selected) {
            ActionPoint.HighlightAP(false);
            selected = false;
        }
    }

    public override void OnHoverStart() {
        outlineOnClick.Highlight();
        NameText.gameObject.SetActive(true);
    }

    public override void OnHoverEnd() {
        outlineOnClick.UnHighlight();
        NameText.gameObject.SetActive(false);
    }

}
