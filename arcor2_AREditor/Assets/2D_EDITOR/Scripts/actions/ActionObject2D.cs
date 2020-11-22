using Base;
using IO.Swagger.Model;
using UnityEngine;


public class ActionObject2D : Base.ActionObject {


    private Vector3 offset;
    private ActionObjectMenu actionObjectMenu;
    private ActionObjectMenuProjectEditor actionObjectMenuProjectEditor;

    protected override void Start() {
        base.Start();
        transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        actionObjectMenu = MenuManager.Instance.ActionObjectMenuSceneEditor.gameObject.GetComponent<ActionObjectMenu>();
        actionObjectMenuProjectEditor = MenuManager.Instance.ActionObjectMenuProjectEditor.gameObject.GetComponent<ActionObjectMenuProjectEditor>();
    }

    public override void OnClick(Click type) {
        if (Base.GameManager.Instance.GetGameState() == Base.GameManager.GameStateEnum.SceneEditor) {
            actionObjectMenu.CurrentObject = this;
            actionObjectMenu.UpdateMenu();
            MenuManager.Instance.ShowMenu(MenuManager.Instance.ActionObjectMenuSceneEditor);
        } else if (Base.GameManager.Instance.GetGameState() == Base.GameManager.GameStateEnum.ProjectEditor) {
            actionObjectMenuProjectEditor.CurrentObject = this;
            MenuManager.Instance.ShowMenu(MenuManager.Instance.ActionObjectMenuProjectEditor);
        }
    }

    public void OnMouseDown() {
        if (SceneInteractable()) {
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
        } else {
            offset = new Vector3(0, 0, 0);
        }

    }

    private void OnMouseDrag() {
        if (!SceneInteractable() || ActionObjectMetadata.Robot)
            return;
        
        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Vector3 transformedPosition = Camera.main.ScreenToWorldPoint(newPosition) + offset;
        transformedPosition.z = transform.position.z; // we only want to update x and y in 2D
        transform.position = transformedPosition;
    }

    private void OnMouseUp() {
        if (!SceneInteractable())
            return;
        //GameManager.Instance.UpdateScene();
        //TODO: update position of the object via RPC

    }

    public override Vector3 GetScenePosition() {
        return Vector3.Scale(DataHelper.PositionToVector3(Data.Pose.Position), new Vector3(100, 100, 1));
    }

    public override void SetScenePosition(Vector3 position) {
        Data.Pose.Position = DataHelper.Vector3ToPosition(Vector3.Scale(position, new Vector3(0.01f, 0.01f, 1)));
    }

    protected override void Update() {
        base.Update();
    }

    public override Quaternion GetSceneOrientation() {
        return DataHelper.OrientationToQuaternion(Data.Pose.Orientation);
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        Data.Pose.Orientation = DataHelper.QuaternionToOrientation(orientation);
    }

    public override bool SceneInteractable() {
        return base.SceneInteractable() && !MenuManager.Instance.IsAnyMenuOpened;
    }

    public override void InitActionObject(string id, string type, Vector3 position, Quaternion orientation, string uuid, ActionObjectMetadata actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResources = true) {
        throw new System.NotImplementedException();
    }

    public override void SetVisibility(float value, bool forceShaderChange = false) {
        throw new System.NotImplementedException();
    }

    public override void Show() {
        throw new System.NotImplementedException();
    }

    public override void Hide() {
        throw new System.NotImplementedException();
    }

    public override void SetInteractivity(bool interactive) {
        throw new System.NotImplementedException();
    }

    public override void CreateModel(CollisionModels customCollisionModels = null) {
        throw new System.NotImplementedException();
    }

    public override GameObject GetModelCopy() {
        throw new System.NotImplementedException();
    }


    public override void OnHoverStart() {

    }

    public override void OnHoverEnd() {

    }
}




