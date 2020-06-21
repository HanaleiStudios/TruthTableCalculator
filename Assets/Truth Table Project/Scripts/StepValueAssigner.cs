using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StepValueAssigner : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text operatorsAndOperands;
    [SerializeField] TMP_Text valuesText;

    [SerializeField] RectTransform[] objectsToMeasure;

    public void SetValues(string name, string ops, string values, int length)
    {
        valuesText.rectTransform.sizeDelta = new Vector2(valuesText.rectTransform.sizeDelta.x, 13 * length);
        nameText.text = name;
        operatorsAndOperands.text = ops;
        valuesText.text = values;
    }

    public float SizeToScaleTo()
    {
        float size = 0;
        foreach(RectTransform rt in objectsToMeasure)
        {
            size += rt.sizeDelta.y;
        }
        size += valuesText.rectTransform.sizeDelta.y;
        return size;
    }

}
