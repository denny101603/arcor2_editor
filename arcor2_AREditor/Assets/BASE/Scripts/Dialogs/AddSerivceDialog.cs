using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using System;
/*
public class AddSerivceDialog : Dialog
{
    public string ServiceToBeAdded;
    public GameObject ToggleGroup;
    public GameObject TogglePrefab;

    public void UpdateMenu(string serviceToBeAdded) {
        ServiceToBeAdded = serviceToBeAdded;
        if (Base.ActionsManager.Instance.ServicesMetadata.TryGetValue(serviceToBeAdded, out Base.ServiceMetadata service)) {
            UpdateToggleGroup(TogglePrefab, ToggleGroup, service.ConfigurationIds);
        }
        
    }

    public void AddServiceToScene() {
        try {
            string configId = GetSelectedValue(ToggleGroup);
            Base.GameManager.Instance.AddServiceToScene(type: ServiceToBeAdded, configId: configId);
            Close();
        } catch (Exception ex) when (ex is Base.ItemNotFoundException) {
            Base.NotificationsModernUI.Instance.ShowNotification("Failed to add service", ex.Message);
        }
    }

    public override void Confirm() {
        AddServiceToScene();
    }
}
*/
