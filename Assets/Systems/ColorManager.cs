using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FYFY;
using FYFY_plugins.TriggerManager;
using System.Linq;

/*
 * Manages color changes for ColorShifters when a ColorShifted component is created
 */

public class ColorManager : FSystem
{    
    private Family f_changedRobotColorToValue = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShiftedToValue)), new AnyOfTags("Player"));

    private Family f_changedRobotColorToFirst = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShiftedToFirst)), new AnyOfTags("Player"));

    private Family f_changedKeyColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("Key"));
    private Family f_changedDoorColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("ColoredDoor"));
    private Family f_changedKeyBlockColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("KeyBlock"));


    protected override void onStart()
    {
        f_changedDoorColor.addEntryCallback(onDoorColorChanged);
        f_changedKeyColor.addEntryCallback(onKeyColorChanged);
        f_changedKeyBlockColor.addEntryCallback(onKeyColorChanged);

        f_changedRobotColorToFirst.addEntryCallback(onRobotColorChangedToFirst);
        f_changedRobotColorToValue.addEntryCallback(onRobotColorChangedToValue);


    }
    public void onRobotColorChangedToValue(GameObject robot)
    {
        Colored color = robot.GetComponent<ColorShiftedToValue>().color;
        robot.GetComponent<ColorShifter>().color = color;
        displayNewRobotColor(robot);
        GameObjectManager.removeComponent<ColorShiftedToValue>(robot);
    }

    public void onRobotColorChangedToFirst(GameObject robot)
    {
        Colored color = robot.GetComponent<Camouflages>().disponibles.First();
        robot.GetComponent<ColorShifter>().color = color;

        displayNewRobotColor(robot);

        GameObjectManager.removeComponent<ColorShiftedToFirst>(robot);

    }

    private void switchRobotColor(GameObject robot, Colored color)
    {
        SkinnedMeshRenderer mesh = robot.transform.Find("Robot2").GetComponent<SkinnedMeshRenderer>();
        mesh.SetMaterials(new List<Material> { GetMaterial(color) });
    }

    private void displayNewRobotColor(GameObject robot)
    {
        SkinnedMeshRenderer mesh = robot.transform.Find("Robot2").GetComponent<SkinnedMeshRenderer>();
        mesh.SetMaterials(new List<Material> { GetMaterial(robot.GetComponent<ColorShifter>().color) });
    }

    public void onKeyColorChanged(GameObject key)
    {
        Material m = GetMaterial(key.GetComponent<ColorShifter>().color);
        var li = new List<Material>();
        li.Add(m);
        key.GetComponent<MeshRenderer>().SetMaterials(li);

        GameObjectManager.removeComponent<ColorShifted>(key);
    }

    public void onDoorColorChanged(GameObject door)
    {
        Material m = GetMaterial(door.GetComponent<ColorShifter>().color);
        var li = new List<Material>();
        li.Add(m);
        
        door.transform.GetChild(0).GetComponent<MeshRenderer>().SetMaterials(li);

        GameObjectManager.removeComponent<ColorShifted>(door);
    }

    public Material GetMaterial(Colored color)
    {
        switch(color)
        {
            case Colored.Red:
                return Resources.Load<Material>("Materials/Red");

            case Colored.Yellow:
                return Resources.Load<Material>("Materials/Yellow"); ;

            case Colored.Green:
                return Resources.Load<Material>("Materials/Green");

            case Colored.Blue:
                return Resources.Load<Material>("Materials/Blue");

            case Colored.Orange:
                return Resources.Load<Material>("Materials/Orange");

            case Colored.Pink:
                return Resources.Load<Material>("Materials/Pink");

            case Colored.Purple:
                return Resources.Load<Material>("Materials/Purple");
                
            case Colored.RobotDefault:

            default:
                return Resources.Load<Material>("Materials/Default"); ;
        }
    }
}
