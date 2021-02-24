using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TwoStatesToggle : MonoBehaviour
{

    private void Awake() {
        icon1 = BigImage.sprite;
        icon2 = SmallImage.sprite;
        Toggle(false);
    }
    public Image BigImage, SmallImage;
    private Sprite icon1, icon2;
    public string State1, State2;
    [HideInInspector]
    public string CurrentState;

    public UnityEvent OnState1, OnState2;

    public void Toggle(bool invokeEvents) {
        if (CurrentState == State1) {
            CurrentState = State2;
            BigImage.sprite = icon2;
            SmallImage.sprite = icon1;
            if (invokeEvents) {
                OnState2?.Invoke();
            }
        } else {
            CurrentState = State1;
            BigImage.sprite = icon1;
            SmallImage.sprite = icon2;
            if (invokeEvents) {
                OnState1?.Invoke();
            }
        }
    }
}
