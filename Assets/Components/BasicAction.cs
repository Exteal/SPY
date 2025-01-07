using UnityEngine;

public class BasicAction : BaseElement {
	// Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).
    public enum ActionType { Forward, TurnLeft, TurnRight, Wait, Activate, TurnBack,
        OpenColored, ColorShift, ColorShiftRed, ColorShiftBlue, ColorShiftToValue, ColorShiftVariable, Test};
    
    public ActionType actionType;

    public string variable;
}