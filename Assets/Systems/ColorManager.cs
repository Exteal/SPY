using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FYFY;
using FYFY_plugins.TriggerManager;

/*
 * Manages color changes for ColorShifters when a ColorShifted component is created
 */

public class ColorManager : FSystem
{
    private Family f_changedRobotColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("Player"));
    private Family f_changedKeyColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("Key"));
    private Family f_changedDoorColor = FamilyManager.getFamily(new AllOfComponents(typeof(ColorShifted)), new AnyOfTags("ColoredDoor"));


    protected override void onStart()
    {
        f_changedRobotColor.addEntryCallback(onRobotColorChanged);
        f_changedDoorColor.addEntryCallback(onDoorColorChanged);
        f_changedKeyColor.addEntryCallback(onKeyColorChanged);
      
    }
    public void onRobotColorChanged(GameObject robot)
    {
        SkinnedMeshRenderer mesh = robot.transform.Find("Robot2").GetComponent<SkinnedMeshRenderer>();
        Colored color = robot.GetComponent<ColorShifter>().color;
        mesh.SetMaterials(new List<Material> { GetMaterial(color) }) ;
        
        GameObjectManager.removeComponent<ColorShifted>(robot);
    }

    public void onKeyColorChanged(GameObject key)
    {
        //Material m = Resources.Load<Material>("Materials/" + stringColorToMaterialName(color_name));

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

        //GameObjectManager.removeComponent<ColorShifted>(door);
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

            case Colored.RobotDefault:

            default:
                return Resources.Load<Material>("Materials/Default"); ;
        }
    }
}
