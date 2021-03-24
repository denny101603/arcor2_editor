using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using IO.Swagger.Model;
using Newtonsoft.Json;
using static Base.Clickable;
using UnityEngine.Events;
using RosSharp.RosBridgeClient.MessageTypes.Nav;

namespace Base {
    [RequireComponent(typeof(OutlineOnClick))]
    public class InputOutput : InteractiveObject {
        public Action Action;
        private List<string> logicItemIds = new List<string>();
        [SerializeField]
        private OutlineOnClick outlineOnClick;
        public GameObject Arrow;
        private object _origObject = null;

        public object ifValue;

        private void Start() {
            if (logicItemIds.Count == 0) {
                Hide();
            } else {
                Show();
            }
        }


        public void AddLogicItem(string logicItemId) {
            Debug.Assert(logicItemId != null);
            logicItemIds.Add(logicItemId);
            Show();
        }

        public void RemoveLogicItem(string logicItemId) {
            Debug.Assert(logicItemIds.Contains(logicItemId));
            logicItemIds.Remove(logicItemId);
            Hide();
        }

        public List<LogicItem> GetLogicItems() {
            Debug.Assert(logicItemIds.Count > 0);
            List<LogicItem> items = new List<LogicItem>();
            foreach (string itemId in logicItemIds)
                if (ProjectManager.Instance.LogicItems.TryGetValue(itemId, out LogicItem logicItem)) {
                    items.Add(logicItem);
                } else {
                    throw new ItemNotFoundException("Logic item with ID " + itemId + " does not exists");
                }
            return items;
        }

        protected bool CheckClickType(Click type) {
           
            if (!ControlBoxManager.Instance.ConnectionsToggle.isOn) {
                return false;
            }
            if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
                return false;
            }
            if (type != Click.MOUSE_LEFT_BUTTON && type != Click.TOUCH) {
                return false;
            }
            return true;
        }

        public override async void OnClick(Click type) {
            if (!CheckClickType(type))
                return;
            
            if (ConnectionManagerArcoro.Instance.IsConnecting()) {
                if (typeof(PuckOutput) == GetType() && Action.Data.Id != "START" && Action.Metadata.Returns.Count > 0 && Action.Metadata.Returns[0] == "boolean") {
                    ShowOutputTypeDialog(() => GameManager.Instance.ObjectSelected(this));
                } else {
                    GameManager.Instance.ObjectSelected(this);
                }
                
            } else {
                if (logicItemIds.Count == 0) {
                    if (typeof(PuckOutput) == GetType() && Action.Data.Id != "START" && Action.Metadata.Returns.Count > 0 && Action.Metadata.Returns[0] == "boolean") {
                        ShowOutputTypeDialog(() => CreateNewConnection());
                    } else {
                        CreateNewConnection();
                    }
                } else {
                    // For output:
                    // if there is "Any" connection, no new could be created and this one should be selected
                    // if there are both "true" and "false" connections, no new could be created

                    // For input:
                    // every time show new connection button
                    bool showNewConnectionButton = true;
                    bool conditionValue = false;

                    int howManyConditions = 0;

                    // kterej connection chci, případně chci vytvořit novej
                    //Dictionary<string, LogicItem> items = new Dictionary<string, LogicItem>();
                    if (ProjectManager.Instance.LogicItems.TryGetValue(logicItemIds[0], out LogicItem logicItem)) {
                        SelectedConnection(logicItem);
                    }
                    return;
                    /*foreach (string itemId in logicItemIds) {
                        if (ProjectManager.Instance.LogicItems.TryGetValue(itemId, out LogicItem logicItem)) {
                            Action start = ProjectManager.Instance.GetAction(logicItem.Data.Start);
                            Action end = ProjectManager.Instance.GetAction(logicItem.Data.End);
                            string label = start.Data.Name + " -> " + end.Data.Name;
                            if (!(logicItem.Data.Condition is null)) {
                                label += " (" + logicItem.Data.Condition.Value + ")";
                                ++howManyConditions;
                                conditionValue = Parameter.GetValue<bool>(logicItem.Data.Condition.Value);
                            }
                            items.Add(label, logicItem);
                        } else {
                            throw new ItemNotFoundException("Logic item with ID " + itemId + " does not exists");
                        }
                        
                    }
                    if (GetType() == typeof(PuckOutput)) {
                        if (howManyConditions == 2) {// both true and false are filled
                            showNewConnectionButton = false;
                        }
                        else if(items.Count == 1 && howManyConditions == 0) { // the "any" connection already exists
                            SelectedConnection(items.Values.First());
                            return;
                        }
                    }
                    MenuManager.Instance.ConnectionSelectorDialog.Open(items, showNewConnectionButton, this);*/

                    /*GameObject theOtherOne = ConnectionManagerArcoro.Instance.GetConnectedTo(GetLogicItems().GetConnection(), gameObject);
                        
                    try {
                        await WebsocketManager.Instance.RemoveLogicItem(logicItemIds);
                        ConnectionManagerArcoro.Instance.CreateConnectionToPointer(theOtherOne);
                        if (typeof(PuckOutput) == GetType()) {
                            theOtherOne.GetComponent<PuckInput>().GetOutput();
                        } else {
                            theOtherOne.GetComponent<PuckOutput>().GetInput();
                        }
                    } catch (RequestFailedException ex) {
                        Debug.LogError(ex);
                        Notifications.Instance.SaveLogs("Failed to remove connection");
                        ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
                    }*/
                }
            }
            
        }


        public async void SelectedConnection(LogicItem logicItem) {
            MenuManager.Instance.ConnectionSelectorDialog.Close();
            if (logicItem == null) {
                if (typeof(PuckOutput) == GetType() && Action.Metadata.Returns.Count > 0 && Action.Metadata.Returns[0] == "boolean") {
                    ShowOutputTypeDialog(() => CreateNewConnection());
                } else {
                    CreateNewConnection();
                }
            } else {
            GameObject theOtherOne = ConnectionManagerArcoro.Instance.GetConnectedTo(logicItem.GetConnection(), gameObject);

            try {
                await WebsocketManager.Instance.RemoveLogicItem(logicItem.Data.Id);
                ConnectionManagerArcoro.Instance.CreateConnectionToPointer(theOtherOne);
                if (typeof(PuckOutput) == GetType()) {
                    theOtherOne.GetComponent<PuckInput>().GetOutput(true, Action);
                } else {
                    theOtherOne.GetComponent<PuckOutput>().GetInput(true, Action);
                }
            } catch (RequestFailedException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs("Failed to remove connection");
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
            }
            }
        }


        private void ShowOutputTypeDialog(UnityAction callback) {
            if (logicItemIds.Count == 2) {
                Notifications.Instance.ShowNotification("Failed", "Cannot create any other connection.");
                return;
            } else if (logicItemIds.Count == 1) {
                List<LogicItem> items = GetLogicItems();
                Debug.Assert(items.Count == 1, "There must be exactly one valid logic item!");
                LogicItem item = items[0];
                if (item.Data.Condition is null) {
                    Notifications.Instance.ShowNotification("Failed", "There is already connection which serves all results");
                    return;
                } else {
                    bool condition = JsonConvert.DeserializeObject<bool>(item.Data.Condition.Value);
                    MenuManager.Instance.OutputTypeDialog.Open(this, callback, false, !condition, condition);                
                    return;
                }
            }
            MenuManager.Instance.OutputTypeDialog.Open(this, callback, true, true, true);
        }

        private void CreateNewConnection() {
            ConnectionManagerArcoro.Instance.CreateConnectionToPointer(gameObject);
            if (typeof(PuckOutput) == GetType()) {
                GetInput(false);
            } else {
                GetOutput(false);
            }
        }


        public async void GetInput(bool cancelCallback, object origObject = null) {
            _origObject = origObject;
            /*List<Action> actionList = ProjectManager.Instance.GetAllActions();
            actionList.Add(ProjectManager.Instance.StartAction);
            actionList.Add(ProjectManager.Instance.EndAction);*/
            /*foreach (Action a in actionList) {
                if (!await ConnectionManagerArcoro.Instance.ValidateConnection(this, a.Input)) {
                    a.Input.Disable();
                }
            }*/
            GameManager.Instance.RequestObject(GameManager.EditorStateEnum.SelectingAction, GetInput, "Select next action", ValidateInput, cancelCallback ? (UnityAction) CancelCallbackOutput : null);
        }

        public async void GetOutput(bool cancelCallback, object origObject = null) {
            _origObject = origObject;
            /*List<Action> actionList = ProjectManager.Instance.GetAllActions();
            actionList.Add(ProjectManager.Instance.StartAction);
            actionList.Add(ProjectManager.Instance.EndAction);*/
            /*foreach (Action a in actionList) {
                if (!await ConnectionManagerArcoro.Instance.ValidateConnection(a.Output, this)) {
                    a.Output.Disable();
                }
            }*/
            GameManager.Instance.RequestObject(GameManager.EditorStateEnum.SelectingAction, GetOutput, "Select other action", ValidateOutput, cancelCallback ? (UnityAction) CancelCallbackOutput : null);
        }

        public void CancelCallbackOutput() {
            if (_origObject != null) {
                GetOutput(_origObject);
                _origObject = null;
            }
        }

        public void CancelCallbackInput() {
            if (_origObject != null) {
                GetInput(_origObject);
                _origObject = null;
            }
        }

        private async Task<RequestResult> ValidateInput(object selectedInput) {
            InputOutput input;
            try {
                input = ((Action3D) selectedInput).Input;
                if (input.ConnectionExists())
                    return new RequestResult(false, "Action already connected");
            } catch (InvalidCastException) {
                return new RequestResult(false, "Wrong object type selected");
            } 
            
            RequestResult result = new RequestResult(true, "");
            if (!await ConnectionManagerArcoro.Instance.ValidateConnection(this, input, GetProjectLogicIf())) {
                result.Success = false;
                result.Message = "Invalid connection";
            }
            return result;
        }

        private async Task<RequestResult> ValidateOutput(object selectedOutput) {
            PuckOutput output;
            try {
                output = ((Action3D) selectedOutput).Output;
                if (output.ConnectionExists())
                    return new RequestResult(false, "Action already connected");
            } catch (InvalidCastException) {
                return new RequestResult(false, "Wrong object type selected");
            }
            RequestResult result = new RequestResult(true, "");
            if (!await ConnectionManagerArcoro.Instance.ValidateConnection(output, this, output.GetProjectLogicIf())) {
                result.Success = false;
                result.Message = "Invalid connection";
            }
            return result;
        }

        protected async virtual void GetInput(object selectedInput) {
            Action3D action = (Action3D) selectedInput;
            
            if (selectedInput == null || action == null) {
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
                return;
            }
            InputOutput input = action.Input;

            try {
                await WebsocketManager.Instance.AddLogicItem(Action.Data.Id, input.Action.Data.Id, GetProjectLogicIf(), false);
                ifValue = null;
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
            } catch (RequestFailedException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs("Failed to add connection");
            }

        }

        private async void GetOutput(object selectedOutput) {
            Action3D action = (Action3D) selectedOutput;
           
            
            
            if (selectedOutput == null || action == null) {
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
                return;
            }
            PuckOutput output = action.Output;
            try {
                await WebsocketManager.Instance.AddLogicItem(output.Action.Data.Id, Action.Data.Id, output.GetProjectLogicIf(), false);
                ifValue = null;
                ConnectionManagerArcoro.Instance.DestroyConnectionToMouse();                
            } catch (RequestFailedException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs("Failed to add connection");
            }
        }

        private IO.Swagger.Model.ProjectLogicIf GetProjectLogicIf() {
            if (ifValue is null)
                return null;
            List<Flow> flows = Action.GetFlows();
            string flowName = flows[0].Type.GetValueOrDefault().ToString().ToLower();
            IO.Swagger.Model.ProjectLogicIf projectLogicIf = new ProjectLogicIf(JsonConvert.SerializeObject(ifValue), Action.Data.Id + "/" + flowName + "/0");
            return projectLogicIf;
        }

        public async override void OnHoverStart() {
            if (!Enabled)
                return;
            if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
                return;
            }
            outlineOnClick.Highlight();
            Action.NameText.gameObject.SetActive(true);
            if (!ConnectionManagerArcoro.Instance.IsConnecting())
                return;
            InputOutput theOtherOne = ConnectionManagerArcoro.Instance.GetConnectedToPointer().GetComponent<InputOutput>();
            bool result;
            if (GetType() == typeof(PuckInput)) {
                result = await ConnectionManagerArcoro.Instance.ValidateConnection(theOtherOne, this, theOtherOne.GetProjectLogicIf());
            } else {
                result = await ConnectionManagerArcoro.Instance.ValidateConnection(this, theOtherOne, null);
            }
            if (!result)
                ConnectionManagerArcoro.Instance.DisableConnectionToMouse();
                
        }

        public override void OnHoverEnd() {
            outlineOnClick.UnHighlight();
            Action.NameText.gameObject.SetActive(false);
            if (!ConnectionManagerArcoro.Instance.IsConnecting())
                return;
            ConnectionManagerArcoro.Instance.EnableConnectionToMouse();
        }

        public override void Enable(bool enable) {
            base.Enable(enable);
            if (GameManager.Instance.GreyVsHide) {
                if (enable)
                    foreach (Renderer renderer in outlineOnClick.Renderers) {
                        if (Action.Data.Id == "START")
                            renderer.material.color = Color.green;
                        else if (Action.Data.Id == "END")
                            renderer.material.color = Color.red;
                        else
                            renderer.material.color = new Color(0.9f, 0.84f, 0.27f);
                    }
                else {
                    foreach (Renderer renderer in outlineOnClick.Renderers)
                        renderer.material.color = Color.gray;
                }
            } else {
                foreach (Renderer renderer in outlineOnClick.Renderers)
                    renderer.enabled = enable;
            }

        }

        public override string GetName() {
            //if (logicItemIds.Count > 0) {
                //GameObject theOtherOne = ConnectionManagerArcoro.Instance.GetConnectedTo(ProjectManager.Instance.LogicItems[logicItemIds[0]].GetConnection(), gameObject);
                if (typeof(PuckOutput) == GetType()) {
                    return Action.Data.Name + "/Out";
                } else {
                    return Action.Data.Name + "/In";
                }
            //}
            //return "input/output";
        }

        public void Hide() {
            Arrow.transform.localScale = new Vector3(0, 0, 0);
            Enabled = false;
        }

        public void Show() {
            Arrow.transform.localScale = new Vector3(0.03f, 0.02f, 0.03f);
            Enabled = true;
        }

        public override string GetId() {
            return GetName();
        }

        public override void OpenMenu() {
            throw new NotImplementedException();
        }

        public override bool HasMenu() {
            return false;
        }

        public override bool Movable() {
            return true;
        }

        public override void StartManipulation() {
            OnClick(Click.MOUSE_LEFT_BUTTON);
        }

        public override void Remove() {
            if (logicItemIds.Count > 0)
                _ = WebsocketManager.Instance.RemoveLogicItem(logicItemIds[0]);
        }

        public override bool Removable() {
            return true;
        }

        public override void Rename(string newName) {
            throw new NotImplementedException();
        }

        public bool ConnectionExists() {
            return logicItemIds.Count > 0;
        }
    }

}

