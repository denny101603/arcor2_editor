using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSizeDialog : Dialog {
    public TransformWheel TransformWheel;

    [SerializeField]
    private LabeledInput inputX, inputY, inputZ;

    private DummyBox dummy;
    private char selectedDimension = 'x';
    private string xUnit = "cm";
    private string yUnit = "cm";
    private string zUnit = "cm";

    public void Init(DummyBox dummy) {
        this.dummy = dummy;
        if (dummy == null)
            return;

        Vector3 dimension = dummy.GetDimensions();
        inputX.SetValue(GetIntegerAndUnits(dimension.x, 'x') + xUnit);
        inputY.SetValue(GetIntegerAndUnits(dimension.y, 'y') + yUnit);
        inputZ.SetValue(GetIntegerAndUnits(dimension.z, 'z') + zUnit);

        OnSelectX();
    }

    /// <summary>
    /// NOT SURE IT WORKS PROPERLY ALL THE TIMES!!!
    /// </summary>
    /// <param name="valueInMeters"></param>
    /// <param name="dimension"></param>
    /// <returns></returns>
    private int GetIntegerAndUnits(float valueInMeters, char dimension) {
        string m = String.Format("{0:0.0000000}", Math.Round(valueInMeters) / 1f);
        string cm = String.Format("{0:0.0000000}", Math.Round(valueInMeters * 100f) / 100f); 
        string mm = String.Format("{0:0.0000000}", Math.Round(valueInMeters * 1000f) / 1000f); 
        string mikro = String.Format("{0:0.0000000}", Math.Round(valueInMeters * 1000000f) / 1000000f);

        int val;
        string unit;
        if (m == cm) {
            val = (int) Math.Round(valueInMeters);
            unit = "m";
           // Debug.LogError(m + "m=cm" + cm);
        } else if (cm == mm) {
            val = (int) Math.Round(valueInMeters * 100);
            unit = "cm";
          //  Debug.LogError(cm + "cm=mm" + mm);
        } else if (mm == mikro) {
            val = (int) Math.Round(valueInMeters * 1000);
            unit = "mm";
          //  Debug.LogError(mm + "mm=mikro" + mikro);
        } else {
            val = (int) Math.Round(valueInMeters * 1000000);
            unit = "μc";
          //  Debug.LogError("else" + mikro);
        }

        SetUnit(dimension, unit);
        return val;
    }

    public void OnUnitChange() {
        SetUnit(selectedDimension, TransformWheel.Units.GetValue());
    }

    private void SetUnit(char dimension, string unit) {
        switch (dimension) {
            case 'x':
                xUnit = unit;
                break;
            case 'y':
                yUnit = unit;
                break;
            case 'z':
                zUnit = unit;
                break;
        }
    }

    private void Update() {
        switch (selectedDimension) {
            case 'x':
                inputX.SetValue(TransformWheel.GetValue() + xUnit);
                break;
            case 'y':
                inputY.SetValue(TransformWheel.GetValue() + yUnit);
                break;
            case 'z':
                inputZ.SetValue(TransformWheel.GetValue() + zUnit);
                break;
        }
    }

    private void SetVisualsUnselected() {
        inputX.Text.fontStyle = TMPro.FontStyles.Normal;
        inputY.Text.fontStyle = TMPro.FontStyles.Normal;
        inputZ.Text.fontStyle = TMPro.FontStyles.Normal;

    }

    public void OnSelectX() {
        selectedDimension = 'x';
        TransformWheel.InitList(GetValueFromString((string) inputX.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(xUnit));

        SetVisualsUnselected();
        inputX.Text.fontStyle = TMPro.FontStyles.Bold;
    }

    public void OnSelectY() {
        selectedDimension = 'y';
        TransformWheel.InitList(GetValueFromString((string) inputY.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(yUnit));

        SetVisualsUnselected();
        inputY.Text.fontStyle = TMPro.FontStyles.Bold;
    }
    public void OnSelectZ() {
        selectedDimension = 'z';
        TransformWheel.InitList(GetValueFromString((string) inputZ.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(zUnit));

        SetVisualsUnselected();
        inputZ.Text.fontStyle = TMPro.FontStyles.Bold;
    }

    private int GetValueFromString(string stringWithUnit) {
        char[] charsToTrim = { 'μ', 'm', 'c' };
        return int.Parse(stringWithUnit.Trim(charsToTrim));
    }

    private float GetValueInMeters(float value, string unit) {
        switch (unit) {
            case "cm":
                return (value / 100);
            case "mm":
                return (value / 1000);
            case "μm":
                return (value / 1000000);
            default:
                return value;
        };
    }
    public override async void Confirm() {
        int x = GetValueFromString((string) inputX.GetValue());
        int y = GetValueFromString((string) inputY.GetValue());
        int z = GetValueFromString((string) inputZ.GetValue());

        dummy.SetDimensions(GetValueInMeters(x, xUnit), GetValueInMeters(y, yUnit), GetValueInMeters(z, zUnit));
        CleanAndClose();
    }

    public void CleanAndClose() {
        LeftMenu.Instance.SetActiveSubmenu(LeftMenuSelection.Settings);
        Close();
    }

    public override void Close() {
        base.Close();
    }
}
