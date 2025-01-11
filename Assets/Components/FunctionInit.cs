using UnityEngine;
using System.Collections.Generic;

public class FunctionInit : ControlElement {
	public string functionName; // The name of the function 
	public bool inExcecution = false;   // param to tell if the function is being executed or not
    public GameObject functionBody; // The body of the function 
}
