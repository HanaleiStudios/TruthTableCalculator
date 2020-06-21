/*
 *
 *  Written by: Luke Harris of Hanalei Studios
 *  5/6/2020
 *
 */

 //TODO: fix content scaling
 //TODO: extra input validation

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class Token
{
    public int precedence;
    public char name;
    public bool variable;
}

public class LogicCalculator : MonoBehaviour
{

    #region variables

    [Header("UI")]

    public TMP_InputField logicStatement;

    public TMP_Text postfixStringOutput;

    public Toggle startTrueToggle;

    public Button calculateButton;

    public TMP_Text calculateButtonText;

    public Button copyFinalResultsButton;

    [Header("Content Placement")]

    public RectTransform[] operandOutputs;

    public RectTransform resultOutput;

    public RectTransform stepOutput;

    List<Vector2> operandOutputsStartingSize = new List<Vector2>();

    Vector2 resultOutputStartingSize;

    Vector2 stepOutputStartingSize;

    [Header("Prefabs")]

    public RectTransform stepPrefab;

    public RectTransform operandPrefab;

    [Header("Logic")]

    public string legalOperands;

    //Misc

    bool startTrue;

    public int stepUpTo;

    List<Token> tokens = new List<Token>();

    int numOfVariables;

    string postfixString = "";

    Dictionary<char, List<bool>> operandValues = new Dictionary<char, List<bool>>();

    #endregion

    private void Start()
    {
        startTrue = startTrueToggle.isOn;
        foreach(RectTransform rt in operandOutputs)
        {
            operandOutputsStartingSize.Add(rt.sizeDelta);
        }

        resultOutputStartingSize = resultOutput.sizeDelta;
        stepOutputStartingSize = stepOutput.sizeDelta;
    }

    #region UI

    public void CheckForInvalidCharaters()
    {
        foreach(char c in logicStatement.text)
        {
            if(legalOperands.Contains(c) || char.IsLetter(c))
            {
                calculateButton.interactable = true;
                calculateButtonText.text = "Calculate!";
            }
            else
            {
                calculateButton.interactable = false;
                calculateButtonText.text = "invalid character";
            }
        }
    }

    //TODO: fix this
    public void CheckIfValidEquation()
    {
        ScanString();
        ReturnPostfixString();
        if (postfixString == null)
        {
            calculateButton.interactable = false;
            calculateButtonText.text = "equation is invalid";
            ResetEverything();
        }
        else
        {
            calculateButton.interactable = true;
            calculateButtonText.text = "Calculate!";
        }
    }

    public void SetStartTrue()
    {
        startTrue = startTrueToggle.isOn;
    }

    public void AddSymbol(string symbol)
    {
        logicStatement.text += symbol;
    }

    public void OutputOperandValuesToScreen(RectTransform rectToOutputTo)
    {
        foreach(KeyValuePair<char,List<bool>> kvp in operandValues)
        {
            rectToOutputTo.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(
                rectToOutputTo.parent.GetComponent<RectTransform>().sizeDelta.x + operandPrefab.sizeDelta.x,
                rectToOutputTo.parent.GetComponent<RectTransform>().sizeDelta.y);

            RectTransform rt = Instantiate(operandPrefab, rectToOutputTo);
            rt.name = kvp.Key.ToString();

            rt.GetComponent<OperandValueAssigner>().SetValues(kvp.Key.ToString(), ListToString(kvp.Value),kvp.Value.Count);
        }
    }

    void OutputResultsToScreen(List<bool> results)
    {
        RectTransform rect = Instantiate(operandPrefab, resultOutput);
        string resultToString = ListToString(results);
        rect.GetComponent<OperandValueAssigner>().SetValues("results", resultToString,results.Count);
        copyFinalResultsButton.onClick.AddListener(delegate
        {
            CopyToClipboard(rect.GetChild(1).GetComponent<TMP_Text>());
        });
            
    }

    //TODO: scale content correctly
    void OutputStep(int stepNum, string operands, string results, int length, bool final = false)
    {
        // set text size ✓
        // calculate total size 
        // scale content
        // make sure is placing correctly
        RectTransform rt = Instantiate(stepPrefab, stepOutput);
        if(!final)
        {
            rt.GetComponent<StepValueAssigner>().SetValues("Step: " + stepNum, "calculating: " + operands, results, length);
        }
        else
        {
            rt.GetComponent<StepValueAssigner>().SetValues("Final results: ", " ", results, length);
        }
        rt.parent.parent.GetComponent<RectTransform>().sizeDelta += new Vector2
                (
                0,
                rt.GetComponent<StepValueAssigner>().SizeToScaleTo()
                );
        Debug.Log(rt.GetComponent<StepValueAssigner>().SizeToScaleTo());
        rt.transform.parent.GetComponent<VerticalLayoutGroup>().spacing = rt.GetComponent<StepValueAssigner>().SizeToScaleTo();

        stepUpTo++;
    }

    public void LoadScene(string sceneToLoad)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }

    #endregion

    #region Shunting Yard

    //TODO: do a scan for brackets to make sure that they're all good, if not then stop right there buddy
    public void ScanString()
    {
        foreach (char c in logicStatement.text)
        {
            if (legalOperands.Contains(c) || char.IsLetter(c))
            {
                AddSymbol(c);
            }
            else
            {
                Debug.LogError("something is wrong!!");
            }
        }

        //calculates number of operands to use
        string variables = "";
        foreach (Token t in tokens)
        {
            //add to num of variables of hasn't been read yet
            if (char.IsLetter(t.name) && !variables.Contains(t.name))
            {
                variables += t.name;
                numOfVariables++;
            }
        }
    }

    public void AddSymbol(char symbol)
    {
        if (char.IsLetter(symbol))
        {
            tokens.Add(new Token { name = symbol, variable = true });
        }
        else
        {
            switch (symbol)
            {
                case '|':
                    tokens.Add(new Token { precedence = 1, name = '|' });
                    break;

                case '&':
                    tokens.Add(new Token { precedence = 2, name = '&' });
                    break;

                case '!':
                    tokens.Add(new Token { precedence = 3, name = '!' });
                    break;

                case '(':
                    tokens.Add(new Token { name = '(' });
                    break;

                case ')':
                    tokens.Add(new Token { name = ')' });
                    break;

                default:
                    Debug.LogError("invalid token!");
                    break;
            }
        }
    }

    public IEnumerable<Token> ShuntingYard(IEnumerable<Token> toks)
    {
        //int itterationCounter = 0;
        Stack<Token> stack = new Stack<Token>();
        foreach (Token t in toks)
        {
            if (t.variable || t.name == '!')
            {
                yield return t;
            }
            else if (t.name == '(')
            {
                stack.Push(t);
            }
            else if (!t.variable && t.name != ')')
            {
                while ((stack.Any() && (stack.Peek().precedence > t.precedence)
                    || (stack.Any() && stack.Peek().precedence == t.precedence))
                && (stack.Any() && stack.Peek().name != '('))
                {
                    yield return stack.Pop();
                    break;
                }
                stack.Push(t);
            }
            else if (t.name == ')')
            { 
                while (stack.Peek().name != '(')
                {
                    yield return stack.Pop();
                }
                if (stack.Peek().name == '(')
                {
                    stack.Pop();
                }
            }
            else
            {
                Debug.LogError("bunk input");
                yield return null;
                break;
            }
        }
        //TODO: error handle this
        while (stack.Any())
        {
            //itterationCounter++;
            //Debug.Log("itteration " + itterationCounter + ": " + stack.Peek().name + " | statement 4");
            yield return stack.Pop();
        }
    }

    //TODO: fix this
    public void ReturnPostfixString()
    {
        foreach (Token t in ShuntingYard(tokens))
        {
            postfixString += t.name;
        }
        postfixStringOutput.text = postfixString;
    }

    #endregion

    #region Variable Generation

    public void VariableGenerator()
    {
        int numberOfRows = Convert.ToInt32(Math.Pow(2, numOfVariables));
        int rowPointer = 0;

        bool valueToAssign;

        if (startTrue)
        {
            valueToAssign = true;
        }
        else
        {
            valueToAssign = false;
        }

        int flipFrequency = 2;

        int maxFlips = Convert.ToInt32(Math.Pow(2, numOfVariables));

        foreach (char c in postfixString)
        {
            if (char.IsLetter(c) && !operandValues.ContainsKey(c))
            {
                List<bool> valuesToAssign = new List<bool>();

                while (rowPointer < numberOfRows)
                {
                    if (flipFrequency < maxFlips)
                    {
                        for (int i = 0; i < numberOfRows; i += flipFrequency)
                        {
                            rowPointer++;
                            valuesToAssign.Add(valueToAssign);
                        }
                        valueToAssign = !valueToAssign;
                    }
                    //handles neatly flipping the variable for the last itteration
                    else if (flipFrequency <= maxFlips)
                    {
                        if (rowPointer == 0)
                        {
                            valuesToAssign.Add(valueToAssign);
                            rowPointer++;
                        }
                        else
                        {
                            valueToAssign = !valueToAssign;
                            valuesToAssign.Add(valueToAssign);
                            rowPointer++;
                        }
                    }
                    else
                    {
                        Debug.LogError("broke the variable generator ya dungus");
                        break;
                    }
                }

                //sets up for next loop
                operandValues.Add(c, valuesToAssign);
                rowPointer = 0;
                if (flipFrequency > maxFlips)
                {
                    flipFrequency = maxFlips;
                }
                else
                {
                    flipFrequency *= 2;
                }

                if (startTrue)
                {
                    valueToAssign = true;
                }
                else
                {
                    valueToAssign = false;
                }
            }
        }

        foreach(RectTransform rt in operandOutputs)
        {
            OutputOperandValuesToScreen(rt);
        }    
    }

    #endregion

    #region operators

    List<bool> And(List<bool> op1, List<bool> op2)
    {
        List<bool> returnable =  new List<bool>();
       if(op1.Count != op2.Count || op2.Count != op1.Count)
        {
            return null;
        }
       else
        {
            for(int i = 0; i < op1.Count; i++)
            {
                if(op1[i] && op2[i])
                {
                    returnable.Add(true);
                }
                else
                {
                    returnable.Add(false);
                }
            }
        }
        return returnable;
    }

    List<bool> Or(List<bool> op1, List<bool> op2)
    {
        List<bool> returnable = new List<bool>();
        if (op1.Count != op2.Count || op2.Count != op1.Count)
        {
            return null;
        }
        else
        {
            for (int i = 0; i < op1.Count; i++)
            {
                if (op1[i] || op2[i])
                {
                    returnable.Add(true);
                }
                else
                {
                    returnable.Add(false);
                }
            }
        }
        return returnable;
    }

    List<bool> Not(List<bool> op1)
    {
        List<bool> returnable = new List<bool>();
        if(op1 == null)
        {
            return null;
        }
        else
        {
            foreach(bool b in op1)
            {
                returnable.Add(!b);
            }
        }
        return returnable;
    }

    #endregion

    #region CalculateTruthTable

    public void CalculateTruthTable()
    {
        Stack<List<bool>> resultsStack = new Stack<List<bool>>();
        int step = 0;

        //TODO: there is more than likely a way more elegant way to do this 
        //while there are more than 2 operators
        while(NumOfOperands(postfixString) >= 2)
        {
            string opsAndOpsInUse = "";
            switch (PeekNextOperator(postfixString))
            {
                case '&':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString).ToString() + PeekNextOperand(postfixString, 1).ToString() + " " +  PeekNextOperator(postfixString);

                    resultsStack.Push(And(operandValues[PopNextOperand(postfixString)], operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '!':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString, 1) + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(Not(operandValues[PopNextOperand(postfixString,1)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '|':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString).ToString() + PeekNextOperand(postfixString, 1).ToString() + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(Or(operandValues[PopNextOperand(postfixString)], operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                default:
                    Debug.LogError("something went wrong while building the results stack chief! | " + postfixString);
                    postfixString = "";
                    break;
            }
        }

        while(resultsStack.Any() && NumOfOperands(postfixString) == 1)
        {
            string opsAndOpsInUse = "";
            switch (PeekNextOperator(postfixString))
            {
                case '&':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString) + " previous result " + PeekNextOperator(postfixString);  

                    resultsStack.Push(And(operandValues[PopNextOperand(postfixString)], resultsStack.Pop()));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '!':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString) + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(Not(operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '|':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString) + " previous result " + PeekNextOperator(postfixString);

                    resultsStack.Push(Or(operandValues[PopNextOperand(postfixString)], resultsStack.Pop()));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                default:
                    Debug.LogError("something went wrong while building the results stack chief! | " + postfixString);
                    postfixString = "";
                    break;
            }
        }

        while(resultsStack.Any() && NumOfOperands(postfixString) > 0)
        {
            string opsAndOpsInUse = "";
            switch (PeekNextOperator(postfixString))
            {
                case '&':
                    step++;
                    opsAndOpsInUse += "previous result " + PeekNextOperand(postfixString) + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(And(resultsStack.Pop(), operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '!':
                    step++;
                    opsAndOpsInUse += PeekNextOperand(postfixString) + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(Not(operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                case '|':
                    step++;
                    opsAndOpsInUse += "previous result " + PeekNextOperand(postfixString) + " " + PeekNextOperator(postfixString);

                    resultsStack.Push(Or(resultsStack.Pop(), operandValues[PopNextOperand(postfixString)]));
                    PopNextOperator(postfixString);

                    OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
                    break;
                default:
                    Debug.LogError("something went wrong while clearing the results stack chief! | " + postfixString);
                    postfixString = "";
                    break;
            }
        }

        while(resultsStack.Any() && NumOfOperators(postfixString) > 0)
        {
            string opsAndOpsInUse = "";
            if (PeekNextOperator(postfixString) == '&')
            {
                step++;
                opsAndOpsInUse += "previous results " + PeekNextOperator(postfixString);

                resultsStack.Push(And(resultsStack.Pop(), resultsStack.Pop()));
                PopNextOperator(postfixString);

                OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
            }
            else if (PeekNextOperator(postfixString) == '|')
            {
                step++;
                opsAndOpsInUse += "previous results " + PeekNextOperator(postfixString);

                resultsStack.Push(Or(resultsStack.Pop(), resultsStack.Pop()));
                PopNextOperator(postfixString);

                OutputStep(step, opsAndOpsInUse, ListToString(resultsStack.Peek()), resultsStack.Peek().Count);
            }
            else
            {
                Debug.LogError("something went wrong while clearing the results stack chief! | " + postfixString);
                postfixString = "";
            }
        }

        while(resultsStack.Any())
        {
            OutputStep(0, "", ListToString(resultsStack.Peek()), resultsStack.Peek().Count, true);
            OutputResultsToScreen(resultsStack.Pop());
        }
    }

    #endregion

    #region helpers

    bool CheckStackForBrackets(Stack<Token> stackToCheck, char bracketToCheckFor)
    {
        foreach(Token t in stackToCheck)
        {
            if(t.name == bracketToCheckFor)
            {
                return true;
            }
        }
        return false;
    }

    int NumOfOperators(string str)
    {
        int i = 0;
        foreach(char c in str)
        {
            switch (c)
            {
                case '|':
                    i++;
                    break;
                case '!':
                    i++;
                    break;
                case '&':
                    i++;
                    break;
            }
        }
        return i;
    }

    int NumOfOperands(string str)
    {
        int i = 0;
        foreach(char c in str)
        {
            if(char.IsLetter(c))
            {
                i++;
            }
        }
        return i;
    }

    public char? PeekNextOperator(string str)
    {
        foreach(char c in str)
        {
            switch(c)
            {
                case '|':
                    return c;
                case '!':
                    return c;
                case '&':
                    return c;
            }
        }
        return null;
    }

    //used to get the next operator
    public char? PopNextOperator(string str)
    {
        for(int i = 0; i < postfixString.Length; i++)
        {
            char? c = str[i];

            switch(c)
            {
                case '|':
                    postfixString = postfixString.Remove(i, 1);
                    return c;
                case '!':
                    postfixString = postfixString.Remove(i, 1);
                    return c;
                case '&':
                    postfixString = postfixString.Remove(i, 1);
                    return c;
            }
            
        }
        return null;
    }

    public char PeekNextOperand(string str, int ahead = 0)
    {
        for(int i = 0; i < str.Length; i++)
        {
            char c = str[i + ahead];

            if(char.IsLetter(c))
            {
                return c;
            }
        }
        return '*';
    }

    public char PopNextOperand(string str, int not = 0)
    {
        for (int i = 0; i < postfixString.Length; i++)
        {
            char c = str[i + not];

            if (char.IsLetter(c))
            {
                postfixString = postfixString.Remove(i + not, 1);
                return c;
            }
        }
        return '*';
    }

    //used to determin if there is another operand in a string
    public bool IsThereAnotherOperand(string str)
    {
        foreach(char c in str)
        {
            if(char.IsLetter(c))
            {
                return true;
            }
        }
        return false;
    }

    public static string ListToString(List<bool> list)
    {
        bool started = false;
        string valueToReturn = "";
        if (list.Count != 0)
        {
            foreach (bool b in list)
            {
                if (b && !started)
                {
                    valueToReturn += "T";
                    started = true;
                }
                else if (b && started)
                {
                    valueToReturn += "\nT";
                }
                else if (!b && !started)
                {
                    valueToReturn += "F";
                    started = true;
                }
                else if (!b && started)
                {
                    valueToReturn += "\nF";
                }
            }
            return valueToReturn;
        }
        else
        {
            return "";
        }
    }

    public static void PrintList(List<bool> list, string name = "list name:")
    {
        string printOut = "";
        if(list.Count != 0)
        {
            foreach(bool b in list)
            {
                printOut += "\n" + b.ToString();
            }
            Debug.Log(name + ": " + printOut);
        }
        else
        {
            Debug.LogError("list empty!");
        }
    }

    public void CopyToClipboard(TMP_Text textToCopy)
    {
        TextEditor te = new TextEditor();
        te.text = textToCopy.text;
        te.SelectAll();
        te.Copy();

    }

    public void ResetEverything()
    {
        //clear all the values!
        tokens.Clear();
        postfixString = "";
        numOfVariables = 0;
        operandValues.Clear();
        postfixStringOutput.text = "";

        //destroy all the children
        DeleteAllChildren(resultOutput);
        DeleteAllChildren(stepOutput);

        //reset content sizes
        foreach (RectTransform t in operandOutputs)
        {
            DeleteAllChildren(t);
            int i = Array.IndexOf(operandOutputs, t);
            t.sizeDelta = operandOutputsStartingSize[i];
        }

        resultOutput.sizeDelta = resultOutputStartingSize;
        stepOutput.sizeDelta = stepOutputStartingSize;
        
        //reset the copy final results button
        copyFinalResultsButton.onClick.RemoveAllListeners();
    }

    public void DeleteAllChildren(Transform transformToDeleteFrom)
    {
        for (int i = 0; i < transformToDeleteFrom.childCount; i++)
        {
            Destroy(transformToDeleteFrom.GetChild(i).gameObject);
        }
    }

    public void RunTruthTableCalculator()
    {
        ResetEverything();

        ScanString();
        ReturnPostfixString();
        if (postfixString != null && !postfixString.Contains("(") && !postfixString.Contains(")"))
        {
            VariableGenerator();
            CalculateTruthTable();
            calculateButtonText.text = "Calculate!";
            calculateButton.interactable = true;
        }
        else
        {
            ResetEverything();
            calculateButtonText.text = "mismatched brackets!";
            calculateButton.interactable = false;
            Debug.LogError("fix your input bud");
        }
    }    

    #endregion

}
