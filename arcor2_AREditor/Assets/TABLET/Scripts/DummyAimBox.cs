using System;
using System.Runtime.Remoting.Messaging;
using Base;
using UnityEngine;

public class DummyAimBox : DummyBox, IActionPointParent {
    public bool Visible;
    public ActionPoint ActionPoint;

    protected override void Awake() {
        base.Awake();
        Base.ProjectManager.Instance.OnActionPointAddedToScene += OnActionPointAddedToScene;
        Name = "BlueBox";
    }

    private void Start() {
    }

    private void OnActionPointAddedToScene(object sender, Base.ActionPointEventArgs args) {
        if (args.ActionPoint.Data.Name == "dabap") {
            ActionPoint = args.ActionPoint;
            transform.SetParent(ActionPoint.transform);
            transform.localPosition = Vector3.zero;
        }
    }

    protected override void Update() {
        base.Update();
        if (Visible)
            Visual.SetActive(true);
        else
            Visual.SetActive(false);
    }



    public async void AimFinished() {
        SetVisibility(true);
    }

    public void SetVisibility(bool visible) {
        Visible = visible;
        PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/visible", visible);
    }

    public override async void Remove() {
        try {
            await WebsocketManager.Instance.RemoveActionPoint(ActionPoint.Data.Id);
            PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/visible", false);
            PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/inScene", false);
            Destroy(gameObject);
            for (int i = 0; i < 4; ++i)
                PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/PointAimed/" + i, false);
            SelectorMenu.Instance.ForceUpdateMenus();
        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to remove BlueBox", e.Message);
        }
        
    }

    public bool IsActionObject() {
        return false;
    }

    public ActionObject GetActionObject() {
        throw new NotImplementedException();
    }

    public Transform GetTransform() {
        return ActionPoint.transform;
    }

    public GameObject GetGameObject() {
        return ActionPoint.gameObject;
    }

    public void OnDestroy() {
        Base.ProjectManager.Instance.OnActionPointAddedToScene -= OnActionPointAddedToScene;
    }
}
