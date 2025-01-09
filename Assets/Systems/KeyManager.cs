using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FYFY;
using FYFY_plugins.TriggerManager;
using System.Linq;

public class KeyManager : FSystem
{
    private Family f_robotcollision = FamilyManager.getFamily(new AllOfComponents(typeof(Triggered3D)), new AnyOfTags("Player"));

    private Family f_playingMode = FamilyManager.getFamily(new AllOfComponents(typeof(PlayMode)));
    private Family f_editingMode = FamilyManager.getFamily(new AllOfComponents(typeof(EditMode)));

    private GameData gameData;
    private bool activeKey;
   

    protected override void onStart()
    {

        activeKey = false;
        GameObject go = GameObject.Find("GameData");
        if (go != null)
            gameData = go.GetComponent<GameData>();
        
        f_robotcollision.addEntryCallback(onNewCollisionKey);

        f_playingMode.addEntryCallback(delegate { activeKey = true; });
        f_editingMode.addEntryCallback(delegate { activeKey = false; });

    }

    // Colision with key and keyblocks
    private void onNewCollisionKey(GameObject robot)
    {
        
        if(activeKey)
        {
            Triggered3D trigger = robot.GetComponent<Triggered3D>();

            foreach (GameObject target in trigger.Targets)
            {
                if (target.CompareTag("Key"))
                {
                    
                    ColorShifter keyColor = target.GetComponent<ColorShifter>();

                    if (!robot.GetComponent<Camouflages>().disponibles.Contains(keyColor.color)) 
                        robot.GetComponent<Camouflages>().disponibles.Add(keyColor.color);

                    MainLoop.instance.StartCoroutine(keyDestroy(target));

                }

                if(target.CompareTag("KeyBlock"))
                {
                    ColorShifter keyColor = target.GetComponent<ColorShifter>();

                    if (!robot.GetComponent<Camouflages>().disponibles.Contains(keyColor.color))
                        robot.GetComponent<Camouflages>().disponibles.Add(keyColor.color);

                    switch(keyColor.color)
                    {
                        case Colored.Red:
                            gameData.actionBlockLimit["ColorShiftRed"] = gameData.actionBlockLimit["ColorShiftRed"] + 1;
                            break;

                        case Colored.Blue:
                            gameData.actionBlockLimit["ColorShiftBlue"] = gameData.actionBlockLimit["ColorShiftBlue"] + 1;
                            break;
                    }

                    GameObjectManager.addComponent<BlockAdded>(gameData.gameObject);
                
                    MainLoop.instance.StartCoroutine(keyDestroy(target));
                }

            }
        }
    }


    private IEnumerator keyDestroy(GameObject go)
    {
        go.GetComponent<ParticleSystem>().Play();
        go.GetComponent<Renderer>().enabled = false;
        yield return new WaitForSeconds(1f); // let time for animation
        GameObjectManager.setGameObjectState(go, false); // then disabling GameObject
    }

}
