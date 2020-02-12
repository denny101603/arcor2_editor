using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Base {
    public abstract class ActionObject : Clickable, IActionProvider {

        // string == IO.Swagger.Model.SceneObject Data.Uuid
        public Dictionary<string, ActionPoint> ActionPoints = new Dictionary<string, ActionPoint>();
        public GameObject ActionPointsSpawn;
        [System.NonSerialized]
        public int CounterAP = 0;

        public IO.Swagger.Model.SceneObject Data = new IO.Swagger.Model.SceneObject("", DataHelper.CreatePose(new Vector3(), new Quaternion()), "");
        public ActionObjectMetadata ActionObjectMetadata;
        public List<string> EndEffectors = new List<string>();

        protected virtual void Start() {
        }
        
        public virtual void UpdateId(string newId, bool updateScene = true) {
            Data.Id = newId;

            if (updateScene) {
                GameManager.Instance.UpdateScene();
            }
        }

        protected virtual void Update() {
            if (gameObject.transform.hasChanged) {
                SetScenePosition(transform.localPosition);
                SetSceneOrientation(transform.localRotation);
                transform.hasChanged = false;
            }
        }

        public void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger) {
            Data = actionObjectSwagger;
            // update position and rotation based on received data from swagger
            transform.localPosition = GetScenePosition();
            transform.localRotation = GetSceneOrientation();
        }

        public virtual bool SceneInteractable() {
            return (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.SceneEditor);
        }

        public async void LoadEndEffectors() {
            List<IO.Swagger.Model.IdValue> idValues = new List<IO.Swagger.Model.IdValue>();
            //idValues.Add(new IO.Swagger.Model.IdValue(id: "robot_id", value: Data.Id));
            EndEffectors = await GameManager.Instance.GetActionParamValues(Data.Uuid, "end_effector_id", idValues);
        }

                
        public abstract Vector3 GetScenePosition();

        public abstract void SetScenePosition(Vector3 position);

        public abstract Quaternion GetSceneOrientation();

        public abstract void SetSceneOrientation(Quaternion orientation);

        public void SetWorldPosition(Vector3 position) {
            Data.Pose.Position = DataHelper.Vector3ToPosition(position);
        }

        public Vector3 GetWorldPosition() {
            return DataHelper.PositionToVector3(Data.Pose.Position);
        }
        public void SetWorldOrientation(Quaternion orientation) {
            Data.Pose.Orientation = DataHelper.QuaternionToOrientation(orientation);
        }

        public Quaternion GetWorldOrientation() {
            return DataHelper.OrientationToQuaternion(Data.Pose.Orientation);
        }

        public string GetProviderName() {
            return Data.Id;
        }


        public ActionMetadata GetActionMetadata(string action_id) {
            if (ActionObjectMetadata.ActionsLoaded) {
                if (ActionObjectMetadata.ActionsMetadata.TryGetValue(action_id, out ActionMetadata actionMetadata)) {
                    return actionMetadata;
                } else {
                    throw new ItemNotFoundException("Metadata not found");
                }
            }
            return null; //TODO: throw exception
        }

        public List<string> GetEndEffectors() {
            return EndEffectors;
        }

        public bool IsRobot() {
            return ActionObjectMetadata.Robot;
        }

        public void DeleteActionObject() {
            // Remove all actions of this action point
            RemoveActionPoints();
            
            // Remove this ActionObject reference from the scene ActionObject list
            Scene.Instance.ActionObjects.Remove(this.Data.Uuid);

            Destroy(gameObject);
        }
        
        public void RemoveActionPoints() {
            // Remove all action points of this action object
            foreach (string actionPointUUID in ActionPoints.Keys.ToList<string>()) {
                RemoveActionPoint(actionPointUUID);
            }
            ActionPoints.Clear();
        }

        public void RemoveActionPoint(string uuid) {
            ActionPoints[uuid].DeleteAP(false);
        }
    }

}
