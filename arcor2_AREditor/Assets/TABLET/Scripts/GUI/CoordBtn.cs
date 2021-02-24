using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoordBtn : MonoBehaviour
{
    private void Awake() {
        Background.color = Color.clear;
    }

    public Image Background, Outline;
    public TMP_Text Value, Delta;
    public string Axis;
    public void Deselect() {
        Background.color = new Color(Outline.color.r, Outline.color.g, Outline.color.b, 0f);
    }

    public void Select() {
        Background.color = new Color(Outline.color.r, Outline.color.g, Outline.color.b, 0.5f);
    }

    public void OnClick() {

    }
}
