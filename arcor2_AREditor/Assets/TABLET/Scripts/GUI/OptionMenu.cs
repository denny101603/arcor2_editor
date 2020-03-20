using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DanielLochner.Assets.SimpleSideMenu;

public abstract class OptionMenu : MonoBehaviour
{
    [SerializeField]
    protected SimpleSideMenu menu;
    [SerializeField]
    protected TMPro.TMP_Text label;

    protected virtual void Open(string title) {
        SetLabel(title);
        menu.Open();
    }

    protected virtual void SetLabel(string label) {
        this.label.text = label;
    }

    public string GetLabel() {
        return label.text;
    }
    public void Close() {
        menu.Close();
    }

}
