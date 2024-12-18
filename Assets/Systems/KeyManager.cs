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

                    robot.GetComponent<ColorShifter>().color = keyColor.color;
                    GameObjectManager.addComponent<ColorShifted>(robot);

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
