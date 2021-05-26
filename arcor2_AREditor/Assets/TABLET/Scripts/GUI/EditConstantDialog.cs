using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Newtonsoft.Json;
using Base;
using System.Threading.Tasks;
using System;
using UnityEngine.Events;
using IO.Swagger.Model;
using Michsky.UI.ModernUIPack;

public class EditConstantDialog : Dialog
{
    public TMPro.TMP_Text Title;

    [SerializeField]
    private LabeledInput nameInput, valueInput;
    [SerializeField]
    private DropdownParameter dropdown;

    private GameObject overlay;

    private ProjectConstant constant;
    private bool isNewConstant;
    private List<string> constantTypes = new List<string> { "int", "float", "str", "bool" };
    private string selected;
    public ButtonWithTooltip CloseBtn, ConfirmButton;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public async void Init(ProjectConstant constant = null) {
        //todo lock constant?
        Debug.LogError("init");
        this.constant = constant;
        isNewConstant = constant == null;

        foreach (string type in constantTypes) {
            CustomDropdown.Item item = new CustomDropdown.Item {
                itemName = type,
                OnItemSelection = new UnityEvent()
            };
            item.OnItemSelection.AddListener(() => OnTypeSelected(type));
            dropdown.Dropdown.dropdownItems.Add(item);
        }
        Debug.LogError("init done");

        //todo Title.text = "Rename " + selectedObject.GetObjectTypeName();
        /*
        nameInput.SetValue(objectToRename.GetName());
        nameInput.SetLabel("Name", "New name");
        nameInput.SetType("string");
        */
    }

    public override void Open() {
        base.Open();
    }

    private void OnTypeSelected(string type) {
        selected = type;
        //todo upravit co jde napsat do inputu value
    }

    public void ValidateInput() {
        //TODO
        //if (isNewConstant) {
        //    ConfirmButton.SetInteractivity(true);
        //    return;
        //}

        //bool valid = ((string) nameInput.GetValue()) != selectedObject.GetName();

        //ConfirmButton.SetInteractivity(valid, "Name has not been changed");
    }
   
    public override async void Confirm() {
        //string name = (string) nameInput.GetValue();

        //if (name == selectedObject.GetName()) { //for new objects, without changing name
        //    Cancel();
        //    return;
        //}

        //    try {
        //    await selectedObject.Rename(name);
        //    Close();
        //} catch (RequestFailedException) {
        //    //notification already shown, nothing else to do
        //}
    }

    public override async void Close() {
        base.Close();
        dropdown.Dropdown.dropdownItems.Clear();
        constant = null;
    }

    public async void Cancel() {
        //if (selectedObject == null)
        //    return;

        //await selectedObject.WriteUnlock();
        Close();
    }


}
