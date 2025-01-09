using UnityEngine;
using FYFY;
using UnityEngine.Playables;

/// <summary>
/// This system executes new currentActions
/// </summary>
public class CurrentActionExecutor : FSystem {
	private Family f_wall = FamilyManager.getFamily(new AllOfComponents(typeof(Position)), new AnyOfTags("Wall", "Door"), new AnyOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family f_activableConsole = FamilyManager.getFamily(new AllOfComponents(typeof(Activable),typeof(Position),typeof(AudioSource)));
    private Family f_newCurrentAction = FamilyManager.getFamily(new AllOfComponents(typeof(CurrentAction), typeof(BasicAction)));

    private GameData gameData;

    private Family f_agent = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef), typeof(Position)));
	private Family f_coloredDoors = FamilyManager.getFamily(new AnyOfTags("ColoredDoor"));

	protected override void onStart()
	{
		f_newCurrentAction.addEntryCallback(onNewCurrentAction);

        GameObject go = GameObject.Find("GameData");
        if (go != null)
            gameData = go.GetComponent<GameData>();

        Pause = true;
	}

	protected override void onProcess(int familiesUpdateCount)
	{
		foreach (GameObject agent in f_agent)
		{
			// count inaction if a robot have no CurrentAction
			if (agent.tag == "Player" && agent.GetComponent<ScriptRef>().executableScript.GetComponentInChildren<CurrentAction>(true) == null)
				agent.GetComponent<ScriptRef>().nbOfInactions++;
			// Cancel move if target position is used by another agent
			bool conflict = true;
			while (conflict)
			{
				conflict = false;
				foreach (GameObject agent2 in f_agent)
					if (agent != agent2 && agent.tag == agent2.tag && agent.tag == "Player")
					{
						Position r1Pos = agent.GetComponent<Position>();
						Position r2Pos = agent2.GetComponent<Position>();
						// check if the two robots move on the same position => forbiden
						if (r2Pos.targetX != -1 && r2Pos.targetY != -1 && r1Pos.targetX == r2Pos.targetX && r1Pos.targetY == r2Pos.targetY)
						{
							r2Pos.targetX = -1;
							r2Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent2);
						}
						// one robot doesn't move and the other try to move on its position => forbiden
						else if (r2Pos.targetX == -1 && r2Pos.targetY == -1 && r1Pos.targetX == r2Pos.x && r1Pos.targetY == r2Pos.y)
						{
							r1Pos.targetX = -1;
							r1Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent);
						}
						// the two robot want to exchange their position => forbiden
						else if (r1Pos.targetX == r2Pos.x && r1Pos.targetY == r2Pos.y && r1Pos.x == r2Pos.targetX && r1Pos.y == r2Pos.targetY)
                        {
							r1Pos.targetX = -1;
							r1Pos.targetY = -1;
							r2Pos.targetX = -1;
							r2Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent);
							GameObjectManager.addComponent<ForceMoveAnimation>(agent2);
						}

					}
			}
		}

		// Record valid movements
		foreach (GameObject robot in f_agent)
		{
			Position pos = robot.GetComponent<Position>();
			if (pos.targetX != -1 && pos.targetY != -1)
			{
				pos.x = pos.targetX;
				pos.y = pos.targetY;
				pos.targetX = -1;
				pos.targetY = -1;
			}
		}
		Pause = true;
	}

	// each time a new currentAction is added, 
	private void onNewCurrentAction(GameObject currentAction) {
		Pause = false; // activates onProcess to identify inactive robots
		
		CurrentAction ca = currentAction.GetComponent<CurrentAction>();
		
        // process action depending on action type
        switch (currentAction.GetComponent<BasicAction>().actionType){
			case BasicAction.ActionType.Forward:
				ApplyForward(ca.agent);
				break;
			case BasicAction.ActionType.TurnLeft:
				ApplyTurnLeft(ca.agent);
				break;
			case BasicAction.ActionType.TurnRight:
				ApplyTurnRight(ca.agent);
				break;
			case BasicAction.ActionType.TurnBack:
				ApplyTurnBack(ca.agent);
				break;
			case BasicAction.ActionType.Wait:
				break;
			case BasicAction.ActionType.Activate:
				Position agentPos = ca.agent.GetComponent<Position>();
				foreach ( GameObject actGo in f_activableConsole){
					if(actGo.GetComponent<Position>().x == agentPos.x && actGo.GetComponent<Position>().y == agentPos.y){
						actGo.GetComponent<AudioSource>().Play();
						// toggle activable GameObject
						if (actGo.GetComponent<TurnedOn>())
							GameObjectManager.removeComponent<TurnedOn>(actGo);
						else
							GameObjectManager.addComponent<TurnedOn>(actGo);
					}
				}
				ca.agent.GetComponent<Animator>().SetTrigger("Action");
				break;
			
			case BasicAction.ActionType.OpenColored:
                Position agentP = ca.agent.GetComponent<Position>();
                foreach (GameObject go in f_coloredDoors) {
					var door = go.transform.Find("Door");

                    if (door != null && door.gameObject.activeInHierarchy)
					{
						if (isInFront(agentP, go.GetComponentInChildren<Position>()) && go.GetComponent<ColorShifter>().color == ca.agent.GetComponent<ColorShifter>().color)
						{
							go.GetComponent<AudioSource>().Play();
                            go.GetComponent<Animator>().SetTrigger("Open");
							go.GetComponent<Animator>().speed = gameData.gameSpeed_current;
                        }
                    }
                    
                }
				break;

			case BasicAction.ActionType.ColorShiftRed:
                GameObjectManager.addComponent<ColorShiftedToValue>(ca.agent, new { color = Colored.Red });
                break;

            case BasicAction.ActionType.ColorShiftBlue:
                GameObjectManager.addComponent<ColorShiftedToValue>(ca.agent, new { color = Colored.Blue });
                break;

            case BasicAction.ActionType.ColorShift:
                GameObjectManager.addComponent<ColorShiftedToFirst>(ca.agent);
                break;
		   
			case BasicAction.ActionType.ColorChange:
                string color = ca.GetComponent<BasicAction>().variable;
                GameObjectManager.addComponent<ColorShiftedToValue>(ca.agent, new { color = parseColor(color) });
                break;
        }
        ca.StopAllCoroutines();
		if (ca.gameObject.activeInHierarchy)
			ca.StartCoroutine(Utility.pulseItem(ca.gameObject));
		// notify agent moving
		if (ca.agent.CompareTag("Drone") && !ca.agent.GetComponent<Moved>())
			GameObjectManager.addComponent<Moved>(ca.agent);
	}

    private Colored parseColor(string color)
    {
        switch (color)
        {
            case "red":
			case "rouge":
                return Colored.Red;

            case "blue":
			case "bleu":
                return Colored.Blue;

            case "green":
            case "vert":
                return Colored.Green;


            case "yellow":
            case "jaune":
                return Colored.Yellow;

            case "orange":
                return Colored.Orange;

            case "pink":
            case "rose":
                return Colored.Pink;

            case "purple":
            case "violet":
                return Colored.Purple;


            case "default":
            default:
                return Colored.RobotDefault;
        }
    }

    private bool isInFront(Position agentPos, Position doorPos)
	{
		return doorPos.x - 1 == agentPos.x && doorPos.y == agentPos.y
			|| doorPos.x + 1 == agentPos.x && doorPos.y == agentPos.y
			|| doorPos.x == agentPos.x && doorPos.y - 1 == agentPos.y
			|| doorPos.x == agentPos.x && doorPos.y + 1 == agentPos.y;

    }
	private void ApplyForward(GameObject go){
		Position pos = go.GetComponent<Position>();
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				if (!checkObstacle(pos.x, pos.y - 1))
				{
					pos.targetX = pos.x;
					pos.targetY = pos.y - 1;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.South:
				if(!checkObstacle(pos.x,pos.y + 1)){
					pos.targetX = pos.x;
					pos.targetY = pos.y + 1;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.East:
				if(!checkObstacle(pos.x + 1, pos.y)){
					pos.targetX = pos.x + 1;
					pos.targetY = pos.y;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.West:
				if(!checkObstacle(pos.x - 1, pos.y)){
					pos.targetX = pos.x - 1;
					pos.targetY = pos.y;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
		}
	}

	private void ApplyTurnLeft(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
		}
	}

	private void ApplyTurnRight(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
		}
	}

	private void ApplyTurnBack(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
		}
	}

	private bool checkObstacle(int x, int z){
		foreach( GameObject go in f_wall){
			if(go.GetComponent<Position>().x == x && go.GetComponent<Position>().y == z)
				return true;
		}
		return false;
	}
}
