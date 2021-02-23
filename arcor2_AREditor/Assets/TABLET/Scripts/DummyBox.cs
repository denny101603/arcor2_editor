using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DummyBox : InteractiveObject
{
    public string Name = "";

    protected virtual void Update() {
        if (Name == "")
            return;
        if (gameObject.transform.hasChanged) {
            PlayerPrefsHelper.SaveVector3(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxPos/" + Name, transform.position);
            PlayerPrefsHelper.SaveQuaternion(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxRot/" + Name, transform.rotation);
            transform.hasChanged = false;
        }
    }

    public void Init(string name) {
        transform.position = PlayerPrefsHelper.LoadVector3(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxPos/" + Name, new Vector3());
        transform.rotation = PlayerPrefsHelper.LoadQuaternion(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxRot/" + Name, new Quaternion());
        Name = name;
        Vector3 dim = PlayerPrefsHelper.LoadVector3(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxDim/" + Name, new Vector3(0.5f, 0.5f, 0.5f));
        SetDimensions(dim.x, dim.y, dim.z);
        SelectorMenu.Instance.ForceUpdateMenus();
    }

    public void Init(string name, float x, float y, float z) {
        PlayerPrefsHelper.SaveVector3(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxPos/" + Name, transform.position);
        PlayerPrefsHelper.SaveQuaternion(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxRot/" + Name, transform.rotation);

        Name = name;
        SetDimensions(x, y, z);
        string dummyBoxes = PlayerPrefsHelper.LoadString(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxes", "");
        if (string.IsNullOrEmpty(dummyBoxes))
            dummyBoxes = name;
        else
            dummyBoxes += ";" + name;
        PlayerPrefsHelper.SaveString(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxes", dummyBoxes);
        SelectorMenu.Instance.ForceUpdateMenus();
    }

    public override string GetId() {
        return Name;
    }

    public override string GetName() {
        return Name;
    }

    public override bool HasMenu() {
        return false;
    }

    public override bool Movable() {
        return true;
    }

    public override void OnClick(Click type) {
        
    }

    public override void OnHoverEnd() {

    }

    public override void OnHoverStart() {

    }

    public override void OpenMenu() {
        throw new System.NotImplementedException();
    }

    public void SetDimensions(float x, float y, float z) {

        PlayerPrefsHelper.SaveVector3(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxDim/" + Name, new Vector3(x, y, z));
        transform.localScale = new Vector3(x, y, z);
    }

    public override void StartManipulation() {
        throw new System.NotImplementedException();
    }

    public override void Remove() {
        string dummyBoxes = PlayerPrefsHelper.LoadString(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxes", "");
        if (!string.IsNullOrEmpty(dummyBoxes)) {
            List<string> boxes = dummyBoxes.Split(';').ToList();
            boxes.Remove(Name);
            PlayerPrefsHelper.SaveString(Base.ProjectManager.Instance.ProjectMeta.Id + "/DummyBoxes", string.Join(";", boxes));
        }
        Destroy(gameObject);

        SelectorMenu.Instance.ForceUpdateMenus();
    }

    public override bool Removable() {
        return true;
    }
}
