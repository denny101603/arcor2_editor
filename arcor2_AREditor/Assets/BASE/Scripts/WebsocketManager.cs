using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;
using System.IO;


namespace Base {
    public class WebsocketManager : Singleton<WebsocketManager> {
        public string APIDomainWS = "";

        private ClientWebSocket clientWebSocket;

        private List<string> actionObjectsToBeUpdated = new List<string>();
        private Queue<KeyValuePair<int, string>> sendingQueue = new Queue<KeyValuePair<int, string>>();
        private string waitingForObjectActions;

        private bool waitingForMessage = false;

        private string receivedData;

        private bool readyToSend, ignoreProjectChanged, connecting;

        private Dictionary<int, string> responses = new Dictionary<int, string>();

        int requestID = 1;

        private void Awake() {
            waitingForMessage = false;
            readyToSend = true;
            ignoreProjectChanged = false;
            connecting = false;

            receivedData = "";
            waitingForObjectActions = "";
        }

        private void Start() {

        }

        public async Task<bool> ConnectToServer(string domain, int port) {
            GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Connecting;
            connecting = true;
            APIDomainWS = GetWSURI(domain, port);
            clientWebSocket = new ClientWebSocket();
            Debug.Log("[WS]:Attempting connection.");
            try {
                Uri uri = new Uri(APIDomainWS);
                await clientWebSocket.ConnectAsync(uri, CancellationToken.None);

                Debug.Log("[WS][connect]:" + "Connected");
            } catch (Exception e) {
                Debug.Log("[WS][exception]:" + e.Message);
                if (e.InnerException != null) {
                    Debug.Log("[WS][inner exception]:" + e.InnerException.Message);
                }
            }

            connecting = false;
            
            return clientWebSocket.State == WebSocketState.Open;
        }

        async public void DisconnectFromSever() {
            Debug.Log("Disconnecting");
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            clientWebSocket = null;
        }



        // Update is called once per frame
        async void Update() {
            if (clientWebSocket == null)
                return;
            if (clientWebSocket.State == WebSocketState.Open && GameManager.Instance.ConnectionStatus == GameManager.ConnectionStatusEnum.Disconnected) {
                GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Connected;
            } else if (clientWebSocket.State != WebSocketState.Open && GameManager.Instance.ConnectionStatus == GameManager.ConnectionStatusEnum.Connected) {
                GameManager.Instance.ConnectionStatus = GameManager.ConnectionStatusEnum.Disconnected;
            }

            if (!waitingForMessage && clientWebSocket.State == WebSocketState.Open) {
                WebSocketReceiveResult result = null;
                waitingForMessage = true;
                ArraySegment<byte> bytesReceived = WebSocket.CreateClientBuffer(8192, 8192);
                MemoryStream ms = new MemoryStream();
                do {
                    result = await clientWebSocket.ReceiveAsync(
                        bytesReceived,
                        CancellationToken.None
                    );

                    if (bytesReceived.Array != null)
                        ms.Write(bytesReceived.Array, bytesReceived.Offset, result.Count);
                    
                } while (!result.EndOfMessage);
                receivedData = Encoding.Default.GetString(ms.ToArray());
                HandleReceivedData(receivedData);
                receivedData = "";
                waitingForMessage = false;

            }

            if (sendingQueue.Count > 0 && readyToSend) {
                SendDataToServer();
            }

        }

        public string GetWSURI(string domain, int port) {
            return "ws://" + domain + ":" + port.ToString();
        }

        void OnApplicationQuit() {
            DisconnectFromSever();
        }

        public void SendDataToServer(string data, int key = -1, bool storeResult = false) {
            if (key < 0) {
                key = requestID++;
            }
            Debug.Log("Sending data to server: " + data);

            if (storeResult) {
                responses[key] = null;
            }
            sendingQueue.Enqueue(new KeyValuePair<int, string>(key, data));
        }

        async public void SendDataToServer() {
            if (sendingQueue.Count == 0)
                return;
            KeyValuePair<int, string> keyVal = sendingQueue.Dequeue();
            readyToSend = false;
            if (clientWebSocket.State != WebSocketState.Open)
                return;

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                         Encoding.UTF8.GetBytes(keyVal.Value)
                     );
            await clientWebSocket.SendAsync(
                bytesToSend,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            readyToSend = true;
        }

        public void UpdateObjectTypes() {
            SendDataToServer(new IO.Swagger.Model.GetObjectTypesRequest(request: "GetObjectTypes").ToJson());
        }

        public void UpdateObjectActions(string ObjectId) {
            SendDataToServer(new IO.Swagger.Model.GetActionsRequest(request: "GetActions", args: new IO.Swagger.Model.TypeArgs(type: ObjectId)).ToJson());
        }

        public void UpdateScene(IO.Swagger.Model.Scene scene) {
            //ARServer.Models.EventSceneChanged eventData = new ARServer.Models.EventSceneChanged();
            IO.Swagger.Model.SceneChangedEvent eventData = new IO.Swagger.Model.SceneChangedEvent {
                Event = "SceneChanged",
                
            };
            if (scene != null) {
                eventData.Data = scene;
            }
            SendDataToServer(eventData.ToJson());
        }

        // TODO: add action parameters
        public void UpdateProject(IO.Swagger.Model.Project project) {
            IO.Swagger.Model.ProjectChangedEvent eventData = new IO.Swagger.Model.ProjectChangedEvent {
                Event = "ProjectChanged"
            };
            if (project != null) {
                eventData.Data = project;
            }
            SendDataToServer(eventData.ToJson());

        }


        private void HandleReceivedData(string data) {
            var dispatchType = new {
                id = 0,
                response = "",
                @event = "",
                request = ""
            };
            Debug.Log("Recieved new data: " + data);
            var dispatch = JsonConvert.DeserializeAnonymousType(data, dispatchType);

            JSONObject jsonData = new JSONObject(data);

            if (dispatch?.response == null && dispatch?.request == null && dispatch?.@event == null)
                return;
            if (dispatch.response != null) {
                switch (dispatch.response) {
                    case "OpenProject":
                        HandleOpenProject(data);
                        break;
                    default:
                        if (responses.ContainsKey(dispatch.id)) {
                            responses[dispatch.id] = data;
                        }
                        break;
                }
            } else if (dispatch.@event != null) {
                switch (dispatch.@event) {
                    case "SceneChanged":
                        HandleSceneChanged(jsonData);
                        break;
                    case "CurrentAction":
                        HandleCurrentAction(jsonData);
                        break;
                    case "ProjectChanged":
                        if (ignoreProjectChanged)
                            ignoreProjectChanged = false;
                        else
                            HandleProjectChanged(jsonData);
                        break;
                }
            }

        }

        /*private async void WaitForResult(string key) {

        }*/

        private async Task<T> WaitForResult<T>(int key) {
            if (responses.TryGetValue(key, out string value)) {
                if (value == null) {
                    value = await WaitForResponseReady(key);
                }
                return JsonConvert.DeserializeObject<T>(value);
            } else {
                return default;
            }
        }

        // TODO: add timeout!
        private Task<string> WaitForResponseReady(int key) {
            return Task.Run(() => {
                while (true) {
                    if (responses.TryGetValue(key, out string value)) {
                        if (value != null) {
                            return value;
                        } else {
                            Thread.Sleep(100);
                        }
                    }
                }
            });
        }

        void HandleProjectChanged(JSONObject obj) {

            try {
                if (obj["event"].str != "ProjectChanged") {
                    return;
                }                

                if (obj["data"].GetType() != typeof(JSONObject)) {
                    GameManager.Instance.ProjectUpdated(null);
                }


                ARServer.Models.EventProjectChanged eventProjectChanged = JsonConvert.DeserializeObject<ARServer.Models.EventProjectChanged>(obj.ToString());
                GameManager.Instance.ProjectUpdated(eventProjectChanged.Project);


            } catch (NullReferenceException e) {
                Debug.Log("Parse error in HandleProjectChanged()");
                GameManager.Instance.ProjectUpdated(null);

            }

        }

        void HandleCurrentAction(JSONObject obj) {
            string puck_id;
            try {
                if (obj["event"].str != "CurrentAction" || obj["data"].GetType() != typeof(JSONObject)) {
                    return;
                }

                puck_id = obj["data"]["action_id"].str;



            } catch (NullReferenceException e) {
                Debug.Log("Parse error in HandleProjectChanged()");
                return;
            }

            Action puck = GameManager.Instance.FindPuck(puck_id);

            //Arrow.transform.SetParent(puck.transform);
            //Arrow.transform.position = puck.transform.position + new Vector3(0f, 1.5f, 0f);
        }

        void HandleSceneChanged(JSONObject obj) {
            ARServer.Models.EventSceneChanged eventSceneChanged = JsonConvert.DeserializeObject<ARServer.Models.EventSceneChanged>(obj.ToString());
            GameManager.Instance.SceneUpdated(eventSceneChanged.Scene);
        }

       
        private void HandleOpenProject(string data) {
            IO.Swagger.Model.OpenProjectResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.OpenProjectResponse>(data);
        }

        public async Task<List<IO.Swagger.Model.ObjectTypeMeta>> GetObjectTypes() {
            int id = requestID++;
            SendDataToServer(new IO.Swagger.Model.GetObjectTypesRequest(id: id, request: "GetObjectTypes").ToJson(), id, true);
            IO.Swagger.Model.GetObjectTypesResponse response = await WaitForResult<IO.Swagger.Model.GetObjectTypesResponse>(id);
            if (response.Result)
                return response.Data;
            else {
                throw new RequestFailedException("Failed to load object types");
            }

        }

        public async Task<List<IO.Swagger.Model.ObjectAction>> GetActions(string name) {
            int id = requestID++;
            SendDataToServer(new IO.Swagger.Model.GetActionsRequest(id: id, request: "GetActions", args: new IO.Swagger.Model.TypeArgs(type: name)).ToJson(), id, true);
            IO.Swagger.Model.GetActionsResponse response = await WaitForResult<IO.Swagger.Model.GetActionsResponse>(id);
            if (response.Result)
                return response.Data;
            else
                throw new RequestFailedException("Failed to load actions for object/service " + name);
        }

        public async Task<IO.Swagger.Model.SaveSceneResponse> SaveScene() {
            int id = requestID++;
            SendDataToServer(new IO.Swagger.Model.SaveSceneRequest(id: id, request: "SaveScene").ToJson(), id, true);
            return await WaitForResult<IO.Swagger.Model.SaveSceneResponse>(id);
        }

        public async Task<IO.Swagger.Model.SaveProjectResponse> SaveProject() {
            int id = requestID++;
            SendDataToServer(new IO.Swagger.Model.SaveProjectRequest(id: id, request: "SaveProject").ToJson(), id, true);
            return await WaitForResult<IO.Swagger.Model.SaveProjectResponse>(id);
        }

        public void OpenProject(string id) {
            SendDataToServer(new IO.Swagger.Model.OpenProjectRequest(request: "OpenProject", args: new IO.Swagger.Model.IdArgs(id: id)).ToJson());
        }

        public void RunProject(string projectId) {
            SendDataToServer(new IO.Swagger.Model.RunProjectRequest(request: "RunProject", args: new IO.Swagger.Model.IdArgs(id: projectId)).ToJson());
        }

        public void StopProject() {
            SendDataToServer(new IO.Swagger.Model.StopProjectRequest(request: "StopProject").ToJson());
        }

        public void PauseProject() {
            SendDataToServer(new IO.Swagger.Model.PauseProjectRequest(request: "PauseProject").ToJson());
        }

        public void ResumeProject() {
            SendDataToServer(new IO.Swagger.Model.ResumeProjectRequest(request: "ResumeProject").ToJson());
        }

        public async Task UpdateActionPointPosition(string actionPointId, string robotId, string endEffectorId, string orientationId, bool updatePosition) {
            int r_id = ++requestID;
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId: robotId, endEffector: endEffectorId);
            IO.Swagger.Model.UpdateActionPointPoseRequestArgs args = new IO.Swagger.Model.UpdateActionPointPoseRequestArgs(id: actionPointId,
                orientationId: orientationId, robot: robotArg, updatePosition: updatePosition);
            IO.Swagger.Model.UpdateActionPointPoseRequest request = new IO.Swagger.Model.UpdateActionPointPoseRequest(id: r_id, request: "UpdateActionPointPose", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointPoseResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointPoseResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateActionPointJoints(string actionPointId, string robotId, string jointsId) {
            int r_id = ++requestID;
            IO.Swagger.Model.UpdateActionPointJointsRequestArgs args = new IO.Swagger.Model.UpdateActionPointJointsRequestArgs(id: actionPointId,
                jointsId: jointsId, robotId: robotId);
            IO.Swagger.Model.UpdateActionPointJointsRequest request = new IO.Swagger.Model.UpdateActionPointJointsRequest(id: r_id, request: "UpdateActionPointJoints", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionPointJointsResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionPointJointsResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task UpdateActionObjectPosition(string actionObjectId, string robotId, string endEffectorId) {
            int r_id = ++requestID;
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(robotId: robotId, endEffector: endEffectorId);
            IO.Swagger.Model.UpdateActionObjectPoseRequestArgs args = new IO.Swagger.Model.UpdateActionObjectPoseRequestArgs(id: actionObjectId, robot: robotArg);
            IO.Swagger.Model.UpdateActionObjectPoseRequest request = new IO.Swagger.Model.UpdateActionObjectPoseRequest(id: r_id, request: "UpdateActionObjectPose", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.UpdateActionObjectPoseResponse response = await WaitForResult<IO.Swagger.Model.UpdateActionObjectPoseResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task CreateNewObjectType(IO.Swagger.Model.ObjectTypeMeta objectType) {
            int r_id = ++requestID;
            IO.Swagger.Model.NewObjectTypeRequest request = new IO.Swagger.Model.NewObjectTypeRequest(id: r_id, request: "NewObjectType", args: objectType);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.NewObjectTypeResponse response = await WaitForResult<IO.Swagger.Model.NewObjectTypeResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task StartObjectFocusing(string objectId, string robotId, string endEffector) {
            int r_id = ++requestID;
            IO.Swagger.Model.RobotArg robotArg = new IO.Swagger.Model.RobotArg(endEffector, robotId);
            IO.Swagger.Model.FocusObjectStartRequestArgs args = new IO.Swagger.Model.FocusObjectStartRequestArgs(objectId: objectId, robot: robotArg);
            IO.Swagger.Model.FocusObjectStartRequest request = new IO.Swagger.Model.FocusObjectStartRequest(id: r_id, request: "FocusObjectStart", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectStartResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectStartResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task SavePosition(string objectId, int pointIdx) {
            int r_id = ++requestID;
            IO.Swagger.Model.FocusObjectRequestArgs args = new IO.Swagger.Model.FocusObjectRequestArgs(objectId: objectId, pointIdx: pointIdx);
            IO.Swagger.Model.FocusObjectRequest request = new IO.Swagger.Model.FocusObjectRequest(id: r_id, request: "FocusObject", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task FocusObjectDone(string objectId) {
            int r_id = ++requestID;
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: objectId);
            IO.Swagger.Model.FocusObjectDoneRequest request = new IO.Swagger.Model.FocusObjectDoneRequest(id: r_id, request: "FocusObjectDone", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.FocusObjectDoneResponse response = await WaitForResult<IO.Swagger.Model.FocusObjectDoneResponse>(r_id);
            if (!response.Result)
                throw new RequestFailedException(response.Messages);
        }

        public async Task<List<IO.Swagger.Model.IdDesc>> LoadScenes() {
            IO.Swagger.Model.ListScenesRequest request = new IO.Swagger.Model.ListScenesRequest(id: ++requestID, request: "ListScenes");
            SendDataToServer(request.ToJson(), requestID, true);
            IO.Swagger.Model.ListScenesResponse response = await WaitForResult<IO.Swagger.Model.ListScenesResponse>(requestID);
            return response.Data;
        }

        public async Task<List<IO.Swagger.Model.ListProjectsResponseData>> LoadProjects() {
            IO.Swagger.Model.ListProjectsRequest request = new IO.Swagger.Model.ListProjectsRequest(id: ++requestID, request: "ListProjects");
            SendDataToServer(request.ToJson(), requestID, true);
            IO.Swagger.Model.ListProjectsResponse response = await WaitForResult<IO.Swagger.Model.ListProjectsResponse>(requestID);
            return response.Data;
        }

        public async Task<IO.Swagger.Model.AddObjectToSceneResponse> AddObjectToScene(IO.Swagger.Model.SceneObject sceneObject) {
            IO.Swagger.Model.AddObjectToSceneRequest request = new IO.Swagger.Model.AddObjectToSceneRequest(id: ++requestID, request: "AddObjectToScene", args: sceneObject);
            SendDataToServer(request.ToJson(), requestID, true);
            return await WaitForResult<IO.Swagger.Model.AddObjectToSceneResponse>(requestID);
        }

        public async Task<IO.Swagger.Model.AddServiceToSceneResponse> AddServiceToScene(IO.Swagger.Model.SceneService sceneService) {
            IO.Swagger.Model.AddServiceToSceneRequest request = new IO.Swagger.Model.AddServiceToSceneRequest(id: ++requestID, request: "AddServiceToScene", args: sceneService);
            SendDataToServer(request.ToJson(), requestID, true);
            return await WaitForResult<IO.Swagger.Model.AddServiceToSceneResponse>(requestID);
        }
        public async Task<IO.Swagger.Model.AutoAddObjectToSceneResponse> AutoAddObjectToScene(string objectType) {
            IO.Swagger.Model.TypeArgs args = new IO.Swagger.Model.TypeArgs(type: objectType);
            IO.Swagger.Model.AutoAddObjectToSceneRequest request = new IO.Swagger.Model.AutoAddObjectToSceneRequest(id: ++requestID, request: "AutoAddObjectToScene", args: args);
            SendDataToServer(request.ToJson(), requestID, true);
            return await WaitForResult<IO.Swagger.Model.AutoAddObjectToSceneResponse>(requestID);
            
        }

        public async Task<bool> AddServiceToScene(string configId, string serviceType) {
            IO.Swagger.Model.SceneService sceneService = new IO.Swagger.Model.SceneService(configurationId: configId, type: serviceType);
            IO.Swagger.Model.AddServiceToSceneRequest request = new IO.Swagger.Model.AddServiceToSceneRequest(id: ++requestID, request: "AddServiceToScene", args: sceneService);
            SendDataToServer(request.ToJson(), requestID, true);
            IO.Swagger.Model.AddServiceToSceneResponse response = await WaitForResult<IO.Swagger.Model.AddServiceToSceneResponse>(requestID);
            return response.Result;
        }

        public async Task<IO.Swagger.Model.RemoveFromSceneResponse> RemoveFromScene(string id) {
            IO.Swagger.Model.RemoveFromSceneRequest request = new IO.Swagger.Model.RemoveFromSceneRequest(id: ++requestID, request: "RemoveFromScene", new IO.Swagger.Model.IdArgs(id: id));
            SendDataToServer(request.ToJson(), requestID, true);
            return await WaitForResult<IO.Swagger.Model.RemoveFromSceneResponse>(requestID);
            
        }

        public async Task<List<IO.Swagger.Model.ServiceTypeMeta>> GetServices() {
            int r_id = ++requestID;
            IO.Swagger.Model.GetServicesRequest request = new IO.Swagger.Model.GetServicesRequest(id: r_id, request: "GetServices");
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.GetServicesResponse response = await WaitForResult<IO.Swagger.Model.GetServicesResponse>(r_id);
            if (response.Result)
                return response.Data;
            else
                return new List<IO.Swagger.Model.ServiceTypeMeta>();
        }

        public async Task<IO.Swagger.Model.OpenSceneResponse> OpenScene(string scene_id) {
            int r_id = ++requestID;
            IO.Swagger.Model.IdArgs args = new IO.Swagger.Model.IdArgs(id: scene_id);
            IO.Swagger.Model.OpenSceneRequest request = new IO.Swagger.Model.OpenSceneRequest(id: r_id, request: "OpenScene", args: args);
            SendDataToServer(request.ToJson(), r_id, true);
            return await WaitForResult<IO.Swagger.Model.OpenSceneResponse>(r_id);
        }

        public async Task<List<string>> GetActionParamValues(string actionProviderId, string param_id, List<IO.Swagger.Model.IdValue> parent_params) {
            int r_id = ++requestID;
            IO.Swagger.Model.ActionParamValuesArgs args = new IO.Swagger.Model.ActionParamValuesArgs(id: actionProviderId, paramId: param_id, parentParams: parent_params);
            IO.Swagger.Model.ActionParamValuesRequest request = new IO.Swagger.Model.ActionParamValuesRequest(id: r_id, request: "ActionParamValues", args);
            SendDataToServer(request.ToJson(), r_id, true);
            IO.Swagger.Model.ActionParamValuesResponse response = await WaitForResult<IO.Swagger.Model.ActionParamValuesResponse>(r_id);
            if (response.Result)
                return response.Data;
            else
                return new List<string>();
        }
    }
}
