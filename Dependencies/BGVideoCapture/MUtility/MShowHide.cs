using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MShowHide : MonoBehaviour
{
    public RectTransform movedPart;

    public RectTransform visiblePos;
    public RectTransform hiddenPos;

    public float smoothTime = 0.2f;

    public bool alsoFade = false;
    public bool alsoToggleGO = false;

    [Header("Set both to 0 to have this hidden at start.")]
    public float actualShownness;
    public float targetShownness;

    public bool useCurve = false;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private float vel;


    public Func<bool> extraHideConditions;


    private void Start()
    {
        if(movedPart == null) {
            movedPart = transform as RectTransform; //this only works for fading
        }
    }

    void Update()
    {
        UpdatePos();
    }

    public void Hide(bool instant = false) {
        targetShownness = 0f;
        if (instant) {
            actualShownness = 0f;
            UpdatePos();
        }
    }
    public void Show(bool instant = false) {
        targetShownness = 1f;
        if (instant) {
            actualShownness = 1f;
            UpdatePos();
        }
    }

    private void UpdatePos() {
        var finalTargShownness = targetShownness;
        if (extraHideConditions != null) {
            var extra = extraHideConditions();
            if (extra) finalTargShownness = 0f;
        }

        /*if(name == "vaki") {
            Debug.Log("finalTargShownness:" + finalTargShownness);
        }*/

        var evalled = finalTargShownness;
        if (useCurve) evalled = curve.Evaluate(evalled);

        actualShownness = Mathf.SmoothDamp(actualShownness, evalled, ref vel, smoothTime);

        bool didAnything = false;
        if(hiddenPos != null) {
            movedPart.anchoredPosition = Vector3.Lerp(hiddenPos.anchoredPosition, visiblePos.anchoredPosition, actualShownness);
            didAnything = true;
        }

        if(alsoFade) {
            var alphaEval = alphaCurve.Evaluate(actualShownness);
            movedPart.GetOrAddComponent<CanvasGroup>().alpha = alphaEval;
            didAnything = true;
        }

        if(alsoToggleGO) {
            var shouldBeActive = finalTargShownness > 0.001f;
            if(movedPart.gameObject.activeSelf != shouldBeActive) {
                movedPart.gameObject.SetActive(shouldBeActive);
            }
            didAnything = true;
        }

        if (!didAnything) {
            Debug.LogError("MShowHide: no modes set, nothing is being done! " + transform.GetHieararchyPath(true), this);
        }
    }
}
