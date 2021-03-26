using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using UnityEngine;
using UnityEngine.UI;
using IO.Swagger.Model;
using System.Globalization;

public class VelocityDialog : Dialog
{
    public TransformWheel TransformWheel;

    [SerializeField]
    private LabeledInput VelocityInput;

    private Base.Action action;

    public void Init(Base.Action action) {
        this.action = action;
        if (action == null)
            return;

        VelocityInput.SetValue(action.Parameters["velocity"].Value);
        TransformWheel.SetValue(int.Parse(action.Parameters["velocity"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US")));
    }

    private void Update() {
        if (action == null)
            return;

        if (TransformWheel.GetValue() < 0)
            TransformWheel.SetValue(0);
        else if (TransformWheel.GetValue() > 100)
            TransformWheel.SetValue(100);

        VelocityInput.SetValue(TransformWheel.GetValue());
    }


    public override async void Confirm() {
        List<ActionParameter> parameters = new List<ActionParameter> {
            new ActionParameter(name: "velocity", type: "double", value: TransformWheel.GetValue().ToString("#.0", CultureInfo.GetCultureInfo("en-US"))),
        };
        foreach (KeyValuePair<string, Base.Parameter> param in action.Parameters) {
            if (param.Value.Name == "velocity")
                continue;
            parameters.Add(new ActionParameter(name: param.Value.Name, type: param.Value.Type, value: param.Value.Value));
        }

        Debug.Assert(ProjectManager.Instance.AllowEdit);
        try {
            await WebsocketManager.Instance.UpdateAction(action.Data.Id, parameters, action.GetFlows());
            Base.Notifications.Instance.ShowToastMessage("Parameters saved");
        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to update action ", e.Message);
        }

        CleanAndClose();
    }

    public void CleanAndClose() {
        LeftMenu.Instance.SetActiveSubmenu(LeftMenuSelection.Settings);
        Close();
    }

    public override void Close() {
        action = null;
        base.Close();
    }

    public void Cancel(bool clean = true) {
        if (clean)
            CleanAndClose();
        else
            Close();
    }
}
