using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ProjectConstantsMenu : Singleton<ProjectConstantsMenu> {
    public GameObject Content, ConstantButtonPrefab;
    public CanvasGroup CanvasGroup;
    private Action3D currentAction;
    public ButtonWithTooltip SaveParametersBtn;
    private List<IParameter> actionParameters = new List<IParameter>();
    private bool parametersChanged;
    public VerticalLayoutGroup DynamicContentLayout;
    public GameObject CanvasRoot;
    public EditConstantDialog EditConstantDialog;
    private bool isMenuOpened;

    public void Show() {
        if (isMenuOpened) {
            Hide();
            return;
        }

        GenerateConstantButtons();

        WebsocketManager.Instance.OnProjectConstantAdded += OnConstantAdded;
        WebsocketManager.Instance.OnProjectConstantRemoved += OnConstantRemoved;

        EditorHelper.EnableCanvasGroup(CanvasGroup, true);
        isMenuOpened = true;
        //return true;
    }

    private void OnConstantRemoved(object sender, ProjectConstantEventArgs args) {
        ProjectConstantButton[] btns = Content.GetComponentsInChildren<ProjectConstantButton>();
        if (btns != null) {
            foreach (ProjectConstantButton btn in btns.Where(o => o.Id == args.ProjectConstant.Id)){
                Destroy(btn.gameObject);
                return;
            }
        }
    }

    private void OnConstantAdded(object sender, ProjectConstantEventArgs args) {
        GenerateConstantButton(args.ProjectConstant);
    }

    private void GenerateConstantButtons() {
        foreach (var constant in ProjectManager.Instance.ProjectConstants) {
            GenerateConstantButton(constant);
        }
    }

    private ProjectConstantButton GenerateConstantButton(ProjectConstant constant) {
        ProjectConstantButton btn = Instantiate(ConstantButtonPrefab, Content.transform).GetComponent<ProjectConstantButton>();
        btn.Id = constant.Id;
        btn.SetName(constant.Name);
        btn.SetValue(Base.Parameter.GetValue<string>(constant.Value)); //TODO fix other types than string
        btn.Button.onClick.AddListener(async () => {
            if (! await EditConstantDialog.Init(Show, constant))
                return;
            Hide();
            EditConstantDialog.Open();
        });
        return btn;
    }

    public async void Hide(bool unlock = true) {
        DestroyConstantButtons();

        EditorHelper.EnableCanvasGroup(CanvasGroup, false);
        if (currentAction != null) {
            currentAction.CloseMenu();
            if (unlock)
                await currentAction.WriteUnlock();
            currentAction = null;
        }

        WebsocketManager.Instance.OnProjectConstantAdded -= OnConstantAdded;
        WebsocketManager.Instance.OnProjectConstantRemoved -= OnConstantRemoved;

        isMenuOpened = false;
    }

    private void DestroyConstantButtons() {
        RectTransform[] transforms = Content.GetComponentsInChildren<RectTransform>();
        if (transforms != null) {
            foreach (RectTransform o in transforms) {
                if (o.gameObject.tag != "Persistent") {
                    Destroy(o.gameObject);
                }
            }
        }
    }

    public bool IsVisible() {
        return CanvasGroup.alpha > 0;
    }

    public async void ShowNewConstantDialog() {
        Hide();
        if (!await EditConstantDialog.Init(Show))
            return;
        Hide();
        EditConstantDialog.Open();
    }

    public static ProjectConstantTypes ConvertStringConstantToEnum(string type) {
        return (ProjectConstantTypes) Enum.Parse(typeof(ProjectConstantTypes), type);
    }

    public static object GetValue(string value, ProjectConstantTypes type) {
        object toReturn = null;
        switch (type) {
            case ProjectConstantTypes.integer:
                toReturn = JsonConvert.DeserializeObject<int>(value);
                break;
            case ProjectConstantTypes.@string:
                toReturn = JsonConvert.DeserializeObject<string>(value);
                break;
            case ProjectConstantTypes.boolean:
                toReturn = JsonConvert.DeserializeObject<bool>(value);
                break;
            case ProjectConstantTypes.@double:
                toReturn = JsonConvert.DeserializeObject<double>(value);
                break;
        }
        return toReturn;
    }


    //public void OnChangeParameterHandler(string parameterId, object newValue, string type, bool isValueValid = true) {
    //    if (!isValueValid) {
    //        SaveParametersBtn.SetInteractivity(false, "Some parameter has invalid value");
    //    } else if (currentAction.Parameters.TryGetValue(parameterId, out Parameter actionParameter)) {
    //        try {
    //            if (JsonConvert.SerializeObject(newValue) != actionParameter.Value) {
    //                parametersChanged = true;
    //                SaveParametersBtn.SetInteractivity(true);
    //            }
    //        } catch (JsonReaderException) {
    //            SaveParametersBtn.SetInteractivity(false, "Some parameter has invalid value");
    //        }

    //    }

    //}

    //public async void SaveParameters() {
    //    if (Parameter.CheckIfAllValuesValid(actionParameters)) {
    //        List<IO.Swagger.Model.ActionParameter> parameters = new List<IO.Swagger.Model.ActionParameter>();
    //        foreach (IParameter actionParameter in actionParameters) {
    //            IO.Swagger.Model.ParameterMeta metadata = currentAction.Metadata.GetParamMetadata(actionParameter.GetName());
    //            string value;
    //            /*if (actionParameter.GetCurrentType() == "link")
    //                value = actionParameter.GetValue().ToString();
    //            else*/
    //                value = JsonConvert.SerializeObject(actionParameter.GetValue());
    //            IO.Swagger.Model.ActionParameter ap = new IO.Swagger.Model.ActionParameter(name: actionParameter.GetName(), value: value, type: actionParameter.GetCurrentType());
    //            parameters.Add(ap);
    //        }
    //        Debug.Assert(ProjectManager.Instance.AllowEdit);
    //        try {
    //            await WebsocketManager.Instance.UpdateAction(currentAction.Data.Id, parameters, currentAction.GetFlows());
    //            Notifications.Instance.ShowToastMessage("Parameters saved");
    //            SaveParametersBtn.SetInteractivity(false, "Parameters unchanged");
    //            parametersChanged = false;
    //            /*if (string.IsNullOrEmpty(GameManager.Instance.ExecutingAction))
    //                await UpdateExecuteAndStopBtns();*/
    //        } catch (RequestFailedException e) {
    //            Notifications.Instance.ShowNotification("Failed to update action ", e.Message);
    //        }
    //    }
    //}


}
