using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Newtonsoft.Json;
using Base;
using System.Threading.Tasks;
using System;

public class RenameDialog : Dialog
{
    //public GameObject CanvasRoot;
    public TMPro.TMP_Text Title;

    [SerializeField]
    private LabeledInput nameInput;
    private GameObject overlay;

    private InteractiveObject selectedObject;

    public void Init(InteractiveObject objectToRename) {
        selectedObject = objectToRename;
        if (objectToRename == null)
            return;

        if (selectedObject is ActionPoint3D)
            Title.text = "Rename Action point";
        else
            Title.text = "Rename dummy box";

        nameInput.SetValue(objectToRename.GetName());
        nameInput.SetLabel("Name", "kde je tohle?");
        nameInput.SetType("string");
    }
    public override async void Confirm() {
        Debug.LogError("called confirm");

        string name = (string) nameInput.GetValue();

        try {
            if (selectedObject is ActionPoint3D) {
                Debug.LogError("calling websocket");
                await WebsocketManager.Instance.RenameActionPoint(selectedObject.GetId(), name);
                Notifications.Instance.ShowToastMessage("Action point renamed");
            } else if (selectedObject is DummyBox dummy) {
                dummy.Rename(name);
                Notifications.Instance.ShowToastMessage("Dummy box renamed");
            }
            Close();

        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to rename action point", e.Message);
        }
    }


    public override void Close() {
        LeftMenu.Instance.gameObject.SetActive(true);
        SelectorMenu.Instance.gameObject.SetActive(true);
        base.Close();
    }

}
