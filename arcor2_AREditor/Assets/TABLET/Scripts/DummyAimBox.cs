using System;
using UnityEngine;

public class DummyAimBox : DummyBox {
    public bool Visible;
    public ActionPoint3D ActionPoint;
    private bool waitingForActionPoint = false;

    private void Awake() {
        Base.ProjectManager.Instance.OnActionPointAddedToScene += OnActionPointAddedToScene;
        Name = "BlueBox";
    }

    private void Start() {
    }

    private void OnActionPointAddedToScene(object sender, Base.ActionPointEventArgs args) {
        if (waitingForActionPoint) {
            ActionPoint = (ActionPoint3D) args.ActionPoint;
            transform.SetParent(ActionPoint.transform);
            transform.localPosition = Vector3.zero;
            waitingForActionPoint = false;
            Visible = true;
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
        waitingForActionPoint = true;
        SetVisibility(true);
        await Base.GameManager.Instance.AddActionPoint("dabap", "", new IO.Swagger.Model.Position((decimal) -0.3, 0, 0));
    }

    public void SetVisibility(bool visible) {
        Visible = visible;
        PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/visible", visible);
    }

    public override void Remove() {
        PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/visible", false);
        PlayerPrefsHelper.SaveBool(Base.ProjectManager.Instance.ProjectMeta.Id + "/BlueBox/inScene", false);

        Destroy(gameObject);

        SelectorMenu.Instance.ForceUpdateMenus();
    }


}
