using System;
using UnityEngine;

public class DummyAimBox : DummyBox
{
    public bool Visible = false;
    public string APId = "";

    protected override void Update() {
        base.Update();
        if (Visible)
            Visual.SetActive(true);
        else
            Visual.SetActive(false);
    }
}
