using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSizeDialog : Dialog {
    public TransformWheel TransformWheel;

    [SerializeField]
    private LabeledInput inputX, inputY, inputZ;
    private GameObject overlay;

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
        inputX.SetValue(dimension.x * 100 + xUnit);
        inputY.SetValue(dimension.y * 100 + xUnit);
        inputZ.SetValue(dimension.z * 100 + xUnit);
        //inputX.SetType("string");

        TransformWheel.InitList((int) (dimension.x * 100)); //todo fix casting
    }

    public void OnUnitChange() {
        switch (selectedDimension) {
            case 'x':
                xUnit = TransformWheel.Units.GetValue();
                break;
            case 'y':
                yUnit = TransformWheel.Units.GetValue();
                break;
            case 'z':
                zUnit = TransformWheel.Units.GetValue();
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

    public void OnSelectX() {
        selectedDimension = 'x';
        TransformWheel.InitList(GetValueFromString((string) inputX.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(xUnit));
    }

    public void OnSelectY() {
        selectedDimension = 'y';
        TransformWheel.InitList(GetValueFromString((string) inputY.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(yUnit));
    }
    public void OnSelectZ() {
        selectedDimension = 'z';
        TransformWheel.InitList(GetValueFromString((string) inputZ.GetValue()));
        TransformWheel.Units.SetIndex(TransformWheel.Units.Units.IndexOf(zUnit));
    }

    private int GetValueFromString(string stringWithUnit) {
        char[] charsToTrim = { 'μ', 'm', 'c' };
        Debug.LogError( int.Parse(stringWithUnit.Trim(charsToTrim)));
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
        Close();
    }

    public override void Close() {
        SelectorMenu.Instance.gameObject.SetActive(true);

        base.Close();
    }
}
