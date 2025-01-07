using UnityEngine;
using FYFY;

public class ColorManager_wrapper : BaseWrapper
{
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
	}

	public void onRobotColorChangedToValue(UnityEngine.GameObject robot)
	{
		MainLoop.callAppropriateSystemMethod (system, "onRobotColorChangedToValue", robot);
	}

	public void onRobotColorChangedToFirst(UnityEngine.GameObject robot)
	{
		MainLoop.callAppropriateSystemMethod (system, "onRobotColorChangedToFirst", robot);
	}

	public void onKeyColorChanged(UnityEngine.GameObject key)
	{
		MainLoop.callAppropriateSystemMethod (system, "onKeyColorChanged", key);
	}

	public void onDoorColorChanged(UnityEngine.GameObject door)
	{
		MainLoop.callAppropriateSystemMethod (system, "onDoorColorChanged", door);
	}

}
