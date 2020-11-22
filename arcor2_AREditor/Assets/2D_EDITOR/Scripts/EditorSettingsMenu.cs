using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;
using Base;
using UnityEngine.EventSystems;

public class EditorSettingsMenu : MonoBehaviour, IMenu {
    public SwitchComponent Visiblity, Interactibility, APOrientationsVisibility, RobotsEEVisible;
    public GameObject ActionObjectsList, ActionPointsList;
    [SerializeField]
    private GameObject ActionPointsScrollable, ActionObjectsScrollable;
    [SerializeField]
    private Slider APSizeSlider;

    private void Start() {
        Debug.Assert(ActionPointsScrollable != null);
        Debug.Assert(ActionObjectsScrollable != null);
        Debug.Assert(APSizeSlider != null);
        Debug.Assert(Visiblity != null);
        Debug.Assert(Interactibility != null);
        Debug.Assert(APOrientationsVisibility != null);
        Debug.Assert(RobotsEEVisible != null);
        Base.SceneManager.Instance.OnLoadScene += OnSceneOrProjectLoaded;
        Base.ProjectManager.Instance.OnLoadProject += OnSceneOrProjectLoaded;
        Base.GameManager.Instance.OnSceneChanged += OnSceneChanged;
        Base.ProjectManager.Instance.OnActionPointAddedToScene += OnActionPointAdded;
        Base.WebsocketManager.Instance.OnActionPointRemoved += OnActionPointRemoved;
        Base.WebsocketManager.Instance.OnActionPointBaseUpdated += OnActionPointBaseUpdated;
        Base.GameManager.Instance.OnGameStateChanged += GameStateChanged;
        Interactibility.SetValue(false);
    }

    private void OnActionPointRemoved(object sender, StringEventArgs args) {
        try {
            ActionButton btn = GetActionPointBtn(args.Data);
            Destroy(btn.gameObject);
        } catch (ItemNotFoundException) {

        }
    }

    private void OnActionPointBaseUpdated(object sender, BareActionPointEventArgs args) {
        try {
            ActionButton btn = GetActionPointBtn(args.ActionPoint.Id);
            btn.SetLabel(args.ActionPoint.Name);
        } catch (ItemNotFoundException) {
            Debug.LogError("Action point button " + args.ActionPoint.Name + " does not exists");
        }
    }


    private void OnActionPointAdded(object sender, ActionPointEventArgs args) {
        AddActionPointButton(args.ActionPoint);
    }

    private void GameStateChanged(object sender, GameStateEventArgs args) {
        ActionPointsScrollable.SetActive(args.Data == GameManager.GameStateEnum.ProjectEditor);
        if (args.Data == GameManager.GameStateEnum.MainScreen || args.Data == GameManager.GameStateEnum.Disconnected ||
            args.Data == GameManager.GameStateEnum.PackageRunning)
            ClearMenu();
    }

    private ActionButton GetActionPointBtn(string id) {
        foreach (Transform t in ActionPointsList.transform) {
            ActionButton apBtn = t.GetComponent<ActionButton>();
            if (apBtn.ObjectId == id)
                return apBtn;
        }
        throw new ItemNotFoundException("Button with id " + id + " does not exists");
    }

    public void UpdateMenu() {
        APSizeSlider.value = ProjectManager.Instance.APSize;
        Visiblity.SetValue(Base.SceneManager.Instance.ActionObjectsVisible);
        Interactibility.SetValue(Base.SceneManager.Instance.ActionObjectsInteractive);
        APOrientationsVisibility.SetValue(Base.ProjectManager.Instance.APOrientationsVisible);
        RobotsEEVisible.SetValue(Base.SceneManager.Instance.RobotsEEVisible);
    }

    public void ShowActionObjects() {
        Base.SceneManager.Instance.ShowActionObjects();
    }

    public void HideActionObjects() {
         Base.SceneManager.Instance.HideActionObjects();
    }

    public void ShowAPOrientations() {
        Base.ProjectManager.Instance.ShowAPOrientations();
    }

    public void HideAPOrientations() {
         Base.ProjectManager.Instance.HideAPOrientations();
    }

    public void InteractivityOn() {
        Base.SceneManager.Instance.SetActionObjectsInteractivity(true);
    }

    public void InteractivityOff() {
         Base.SceneManager.Instance.SetActionObjectsInteractivity(false);
    }

    public void ShowRobotsEE() {
        if (!SceneManager.Instance.ShowRobotsEE()) {
            RobotsEEVisible.SetValue(false);
        }
    }

    public void HideRobotsEE() {
        SceneManager.Instance.HideRobotsEE();
    }

    public void SwitchOnExpertMode() {
        GameManager.Instance.ExpertMode = true;
    }

    public void SwitchOffExpertMode() {
        GameManager.Instance.ExpertMode = false;
    }

    public void OnSceneOrProjectLoaded(object sender, EventArgs eventArgs) {
    }

    public void OnSceneChanged(object sender, EventArgs eventArgs) {
        foreach (Transform t in ActionObjectsList.transform) {
            Destroy(t.gameObject);
        }
        foreach (Base.ActionObject actionObject in Base.SceneManager.Instance.ActionObjects.Values) {
            GameObject btnGO = Instantiate(Base.GameManager.Instance.ButtonPrefab, ActionObjectsList.transform);
            btnGO.transform.localScale = new Vector3(1, 1, 1);
            Button btn = btnGO.GetComponent<Button>();
            btn.GetComponentInChildren<TMPro.TMP_Text>().text = actionObject.Data.Name;
            btn.onClick.AddListener(() => ShowActionObject(actionObject));

            // Add EventTrigger OnPointerEnter and OnPointerExit - to be able to highlight corresponding AO when hovering over button
            OutlineOnClick AOoutline = actionObject.GetComponent<OutlineOnClick>();
            EventTrigger eventTrigger = btnGO.AddComponent<EventTrigger>();
            // Create OnPointerEnter entry
            EventTrigger.Entry OnPointerEnter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            OnPointerEnter.callback.AddListener((eventData) => AOoutline.Highlight());
            eventTrigger.triggers.Add(OnPointerEnter);

            // Create OnPointerExit entry
            EventTrigger.Entry OnPointerExit = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerExit
            };
            OnPointerExit.callback.AddListener((eventData) => AOoutline.UnHighlight());
            eventTrigger.triggers.Add(OnPointerExit);
        }
    }

    public void ClearMenu() {
        foreach (Transform t in ActionPointsList.transform) {
            Destroy(t.gameObject);
        }
    }

    private void AddActionPointButton(Base.ActionPoint actionPoint) {
        ActionButton btn = Instantiate(Base.GameManager.Instance.ButtonPrefab, ActionPointsList.transform).GetComponent<ActionButton>();
        btn.transform.localScale = new Vector3(1, 1, 1);
        btn.SetLabel(actionPoint.Data.Name);
        btn.Button.onClick.AddListener(() => ShowActionPoint(actionPoint));
        btn.ObjectId = actionPoint.Data.Id;
        // Add EventTrigger OnPointerEnter and OnPointerExit - to be able to highlight corresponding AP when hovering over button
        OutlineOnClick APoutline = actionPoint.GetComponent<OutlineOnClick>();
        EventTrigger eventTrigger = btn.gameObject.AddComponent<EventTrigger>();
        // Create OnPointerEnter entry
        EventTrigger.Entry OnPointerEnter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        OnPointerEnter.callback.AddListener((eventData) => APoutline.Highlight());
        eventTrigger.triggers.Add(OnPointerEnter);

        // Create OnPointerExit entry
        EventTrigger.Entry OnPointerExit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        OnPointerExit.callback.AddListener((eventData) => APoutline.UnHighlight());
        eventTrigger.triggers.Add(OnPointerExit);
    }

    private void ShowActionPoint(ActionPoint actionPoint) {
        MenuManager.Instance.ActionObjectSettingsMenu.Close();
        actionPoint.ShowMenu();
        Base.SceneManager.Instance.SetSelectedObject(actionPoint.gameObject);
        // Select(force = true) to force selection and not losing AP highlight upon EditorSettingsMenu closing 
        actionPoint.SendMessage("Select", true);
    }

    private void ShowActionObject(Base.ActionObject actionObject) {
        MenuManager.Instance.ActionObjectSettingsMenu.Close();
        actionObject.ShowMenu();
        Base.SceneManager.Instance.SetSelectedObject(actionObject.gameObject);
        actionObject.SendMessage("Select", true);
    }

    public void OnAPSizeChange(float value) {
        ProjectManager.Instance.SetAPSize(value);
    }




}
