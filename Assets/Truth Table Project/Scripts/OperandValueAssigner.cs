using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OperandValueAssigner : MonoBehaviour
{
    [SerializeField]TMP_Text nameText;
    [SerializeField]TMP_Text valuesText;

    public void SetValues(string name, string values, int lenght)
    {
        nameText.text = name;
        valuesText.text = values;
        valuesText.rectTransform.sizeDelta =new Vector2(valuesText.rectTransform.sizeDelta.x, 25 * lenght);
        valuesText.rectTransform.anchoredPosition = new Vector2(valuesText.rectTransform.anchoredPosition.x, -(valuesText.rectTransform.sizeDelta.y/2) - 28);

        valuesText.transform.parent.parent.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(valuesText.transform.parent.parent.parent.GetComponent<RectTransform>().sizeDelta.x, valuesText.rectTransform.sizeDelta.y);

    }

}
