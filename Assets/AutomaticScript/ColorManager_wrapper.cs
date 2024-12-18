using UnityEngine;
using FYFY;

public class ColorManager_wrapper : BaseWrapper
{
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
	}

	public void onRobotColorChanged(UnityEngine.GameObject robot)
	{
		MainLoop.callAppropriateSystemMethod (system, "onRobotColorChanged", robot);
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
