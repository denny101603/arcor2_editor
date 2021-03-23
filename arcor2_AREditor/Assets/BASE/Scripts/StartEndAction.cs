using System;
using Base;
using UnityEngine;
using RuntimeGizmos;

public abstract class StartEndAction : Action3D {

    protected string playerPrefsKey;

    

    public virtual void Init(IO.Swagger.Model.Action projectAction, Base.ActionMetadata metadata, Base.ActionPoint ap, IActionProvider actionProvider, string actionType) {
        base.Init(projectAction, metadata, ap, actionProvider);

        if (!Base.ProjectManager.Instance.ProjectMeta.HasLogic) {
            Destroy(gameObject);
            return;
        }
        playerPrefsKey = "project/" + ProjectManager.Instance.ProjectMeta.Id + "/" + actionType;
        
    }

    private void Update() {
        if (gameObject.transform.hasChanged) {
            PlayerPrefsHelper.SaveVector3(playerPrefsKey, transform.localPosition);
            transform.hasChanged = false;
        }
    }

    public override void OnHoverStart() {
        base.OnHoverStart();
    }

    public override void OnHoverEnd() {
        base.OnHoverEnd();
    }

    public override bool Movable() {
        return true;
    }

    public override bool HasMenu() {
        return false;
    }

    public override void StartManipulation() {
        TransformGizmo.Instance.AddTarget(Visual.transform);
        outlineOnClick.GizmoHighlight();
    }


    }
