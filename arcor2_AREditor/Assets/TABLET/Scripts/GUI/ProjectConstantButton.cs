using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Base;
using Newtonsoft.Json;

public class ProjectConstantButton : MonoBehaviour
{
    public Button Button;
    public TMPro.TMP_Text Name, Value;
    public string Id;

    // Start is called before the first frame update
    void Start()
    {
        WebsocketManager.Instance.OnProjectConstantUpdated += OnConstantUpdated;
    }

    private void OnConstantUpdated(object sender, ProjectConstantEventArgs args) {
        if (args.ProjectConstant.Id != Id)
            return;

        SetName(args.ProjectConstant.Name);
        SetValue(ProjectConstantsMenu.GetValue(args.ProjectConstant.Value, ProjectConstantsMenu.ConvertStringConstantToEnum(args.ProjectConstant.Type)));
    }

    private void OnDestroy() {
        WebsocketManager.Instance.OnProjectConstantUpdated -= OnConstantUpdated;
    }

    internal void SetName(string name) {
        Name.SetText(name);
    }

    internal void SetValue(string value) {
        Value.SetText(value);
    }
    internal void SetValue(object value) {
        SetValue(value.ToString());
    }
}
