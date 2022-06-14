using UnityEngine;
using FYFY;
using FYFY_plugins.PointerManager;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

/// <summary>
/// Manage dialogs at the begining of the level
/// Manage InGame UI (Play/Pause/Stop, reset, go back to main menu...)
/// Manage history
/// Manage end panel (compute Score and stars)
/// </summary>
public class UISystem : FSystem {
	private Family requireEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)), new NoneOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family displayedEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd), typeof(AudioSource)), new AllOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family playerGO = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef), typeof(Position)), new AnyOfTags("Player"));
	private Family actions = FamilyManager.getFamily(new AllOfComponents(typeof(PointerSensitive), typeof(UIActionType)));
	private Family currentActions = FamilyManager.getFamily(new AllOfComponents(typeof(BasicAction), typeof(UIActionType), typeof(CurrentAction)));
	private Family newEnd_f = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)));
	private Family resetBlocLimit_f = FamilyManager.getFamily(new AllOfComponents(typeof(ResetBlocLimit)));
	private Family scriptIsRunning = FamilyManager.getFamily(new AllOfComponents(typeof(PlayerIsMoving)));
	private Family emptyPlayerExecution = FamilyManager.getFamily(new AllOfComponents(typeof(EmptyExecution)));
	private Family agents = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef)));
	private Family viewportContainerPointed_f = FamilyManager.getFamily(new AllOfComponents(typeof(PointerOver), typeof(ViewportContainer))); // Les container contenant les container éditable
	private Family viewportContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(ViewportContainer))); // Les containers viewport
	private Family scriptContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor")); // Les containers scripts qui ne sont pas des block d'action
	private Family agent_f = FamilyManager.getFamily(new AllOfComponents(typeof(AgentEdit), typeof(ScriptRef))); // On récupére les agents pouvant être édité
	private Family resetButton_f = FamilyManager.getFamily(new AnyOfTags("ResetButton")); // Les boutons reset

	private Family inventoryBlocks = FamilyManager.getFamily(new AllOfComponents(typeof(ElementToDrag)), new AllOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));

	private GameData gameData;
	private int nDialog = 0;
	private GameObject lastEditedScript;

	public GameObject buttonPlay;
	public GameObject buttonContinue;
	public GameObject buttonStop;
	public GameObject buttonPause;
	public GameObject buttonStep;
	public GameObject buttonSpeed;
	public GameObject menuEchap;
	public GameObject endPanel;
	public GameObject dialogPanel;
	public GameObject canvas;
	public GameObject editableScriptContainer;
	public GameObject libraryPanel;
	public GameObject EditableContainer;
	public GameObject prefabViewportScriptContainer;
	private string nameContainerSelected; // Nom du container selectionné
	private GameObject containerSelected; // Le container selectionné

	public static UISystem instance;

	public UISystem(){
		instance = this;
	}

	protected override void onStart()
	{
		GameObject go = GameObject.Find("GameData");
		if (go != null)
			gameData = go.GetComponent<GameData>();

		GameObjectManager.setGameObjectState(endPanel.transform.parent.gameObject, false);
		GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, false);

		requireEndPanel.addEntryCallback(displayEndPanel);
		displayedEndPanel.addEntryCallback(onDisplayedEndPanel);
		actions.addEntryCallback(linkTo);
		newEnd_f.addEntryCallback(levelFinished);
		resetBlocLimit_f.addEntryCallback(delegate (GameObject go) { destroyScript(go, true); });
		scriptIsRunning.addExitCallback(delegate { setExecutionState(true); });
		scriptIsRunning.addExitCallback(saveHistory);
		emptyPlayerExecution.addEntryCallback(delegate { setExecutionState(true); });
		emptyPlayerExecution.addEntryCallback(delegate { GameObjectManager.removeComponent<EmptyExecution>(MainLoop.instance.gameObject); });
		
		currentActions.addEntryCallback(onNewCurrentAction);

		inventoryBlocks.addEntryCallback(delegate { forceUIRefresh(); });
		inventoryBlocks.addExitCallback(delegate { forceUIRefresh(); });

		lastEditedScript = null;

		loadHistory();

		// Afin de mettre en rouge les noms qui ne sont pas en lien dés le début
		MainLoop.instance.StartCoroutine(tcheckLinkName());
	}


	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount)
	{
		//Activate DialogPanel if there is a message
		if (gameData != null && gameData.dialogMessage.Count > 0 && !dialogPanel.transform.parent.gameObject.activeSelf)
		{
			showDialogPanel();
		}

        //Active/désactive le menu echap si on appuie su echap
        if (Input.GetKeyDown(KeyCode.Escape))
        {
			setActiveEscapeMenu();
        }
	}


	// Active ou désactive le bouton play si il y a ou non des actions dans un container script
	private IEnumerator updatePlayButton()
	{
		yield return null;
		foreach (GameObject container in scriptContainer_f)
		{
			buttonPlay.GetComponent<Button>().interactable = !(container.transform.childCount < 3);
		}
	}


	// Vérifie si les noms des containers correspond à un agent et vice-versa
	// Si non, Fait apparaitre le nom en rouge
	private IEnumerator tcheckLinkName()
	{
		yield return null;

		// On parcours les containers et si aucun nom ne correspond alors on met leur nom en gras rouge
		foreach (GameObject container in scriptContainer_f)
		{
			bool nameSame = false;
			foreach (GameObject agent in agent_f)
			{
				if (container.GetComponent<UITypeContainer>().associedAgentName == agent.GetComponent<AgentEdit>().agentName)
				{
					nameSame = true;
				}
			}

			// Si même nom trouver on met l'arriére plan blanc
			if (nameSame)
			{
				container.transform.Find("ContainerName").GetComponent<TMP_InputField>().image.color = Color.white;
			}
			else // sinon rouge 
			{
				container.transform.Find("ContainerName").GetComponent<TMP_InputField>().image.color = new Color(1f, 0.4f, 0.28f, 1f);
			}
		}

		// On fait la même chose pour les agents
		foreach (GameObject agent in agent_f)
		{
			bool nameSame = false;
			foreach (GameObject container in scriptContainer_f)
			{
				if (container.GetComponent<UITypeContainer>().associedAgentName == agent.GetComponent<AgentEdit>().agentName)
				{
					nameSame = true;
				}
			}

			// Si même nom trouver on met l'arriére transparent
			if (nameSame)
			{
				agent.GetComponent<ScriptRef>().uiContainer.GetComponentInChildren<TMP_InputField>().image.color = new Color(1f, 1f, 1f, 1f);
			}
			else // sinon rouge 
			{
				agent.GetComponent<ScriptRef>().uiContainer.GetComponentInChildren<TMP_InputField>().image.color = new Color(1f, 0.4f, 0.28f, 1f);
			}
		}
	}


	///  ????
	IEnumerator GetTextureWebRequest(Image img, string path)
	{
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(www.error);
		}
		else
		{
			Texture2D tex2D = ((DownloadHandlerTexture)www.downloadHandler).texture;
			img.sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0, 0), 100.0f);
		}
	}


	// Permet de lancer la corroutine "updatePlayButton" depuis l'extérieur du systéme
	public void startUpdatePlayButton()
    {
		MainLoop.instance.StartCoroutine(updatePlayButton());
	}

	// Rafraichit certain boutton de l'UI
	public void refreshUIButton()
	{
		//Refresh Containers size
		foreach (GameObject container in scriptContainer_f)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
		}
		MainLoop.instance.StartCoroutine(updatePlayButton());
	}

	// Rafraichit le nom des containers
	public void refreshUINameContainer()
	{
		MainLoop.instance.StartCoroutine(tcheckLinkName());
	}

	private void forceUIRefresh()
    {
		LayoutRebuilder.ForceRebuildLayoutImmediate(libraryPanel.GetComponent<RectTransform>());
		RebuildInventory();
	}

	// Met les hauteurs de chaque partie de l'inventaire à la bonne taille selon le nombre de bloc activé par partie
	private void RebuildInventory()
    {
		// Pour chaque partie on va compter le nombre de bloc activé
		// Tous les 3 block on ajoute +40unité pour la taille
		// Avec une taille de départ de 25 pour 0 block et 65 pour 1 block
    }

	// ?????
	private void onNewCurrentAction(GameObject go){
		if (go.activeInHierarchy)
		{
			Vector3 v = GetGUIElementOffset(go.GetComponent<RectTransform>());
			if (v != Vector3.zero)
			{ // if not visible in UI
				ScrollRect containerScrollRect = go.GetComponentInParent<ScrollRect>();
				containerScrollRect.content.localPosition += GetSnapToPositionToBringChildIntoView(containerScrollRect, go.GetComponent<RectTransform>());
			}
		}
	}


	// ?????
	public Vector3 GetSnapToPositionToBringChildIntoView(ScrollRect scrollRect, RectTransform child){
		Canvas.ForceUpdateCanvases();
		Vector3 viewportLocalPosition = scrollRect.viewport.localPosition;
		Vector3 childLocalPosition   = child.localPosition;
		Vector3 result = new Vector3(
			0,
			0 - (viewportLocalPosition.y + childLocalPosition.y),
			0
		);
		return result;
	}

	
	// ?????
	public Vector3 GetGUIElementOffset(RectTransform rect){
        Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);
        Vector3[] objectCorners = new Vector3[4];
        rect.GetWorldCorners(objectCorners);


		var xnew = 0f;
        var ynew = 0f;
        var znew = 0f;
 
        for (int i = 0; i < objectCorners.Length; i++){
			if (objectCorners[i].x < screenBounds.xMin)
                xnew = screenBounds.xMin - objectCorners[i].x;

            if (objectCorners[i].x > screenBounds.xMax)
                xnew = screenBounds.xMax - objectCorners[i].x;

            if (objectCorners[i].y < screenBounds.yMin)
                ynew = screenBounds.yMin - objectCorners[i].y;

            if (objectCorners[i].y > screenBounds.yMax)
                ynew = screenBounds.yMax - objectCorners[i].y;
				
        }
 
        return new Vector3(xnew, ynew, znew);
 
    }

	// On affiche ou non la partie librairie/programmation sequence en fonction de la valeur reçue
	public void setExecutionState(bool value){
		GameObjectManager.setGameObjectState(libraryPanel, !value);
		GameObjectManager.setGameObjectState(EditableContainer.transform.Find("EditableContainers").gameObject, !value); 
		GameObjectManager.setGameObjectState(canvas.transform.Find("Scrollbar Vertical").gameObject, !value);
		GameObjectManager.setGameObjectState(canvas.transform.Find("AgentCanvas").gameObject, value);
		if (value)
        {
			EditableContainer.transform.position = new Vector3(0.0f, EditableContainer.transform.position.y, 0.0f);
		}
        else
        {
			float calculeReplacement = libraryPanel.GetComponent<RectTransform>().sizeDelta.x + canvas.transform.Find("Scrollbar Vertical").GetComponent<RectTransform>().sizeDelta.x;
			EditableContainer.transform.position = new Vector3(calculeReplacement, EditableContainer.transform.position.y, 0.0f);
		}
	}
	

	// Permet de sauvegarder l'historique de la sequence créée
	private void saveHistory(int unused = 0){
		if(gameData.actionsHistory == null){
			gameData.actionsHistory = lastEditedScript;
		}
		else{
			foreach(Transform child in lastEditedScript.transform){
				Transform copy = UnityEngine.GameObject.Instantiate(child);
				copy.SetParent(gameData.actionsHistory.transform);
				GameObjectManager.bind(copy.gameObject);				
			}
			GameObjectManager.refresh(gameData.actionsHistory);
		}	
	}


	// Charge la sequence d'action sauvegarder dans le script container
	private void loadHistory(){
		if(gameData != null && gameData.actionsHistory != null){
			for(int i = 0 ; i < gameData.actionsHistory.transform.childCount ; i++){
				Transform child = UnityEngine.GameObject.Instantiate(gameData.actionsHistory.transform.GetChild(i));
				child.SetParent(editableScriptContainer.transform);
				GameObjectManager.bind(child.gameObject);
				GameObjectManager.refresh(editableScriptContainer);
			}
			LevelGenerator.computeNext(gameData.actionsHistory);
			foreach(BaseElement act in editableScriptContainer.GetComponentsInChildren<BaseElement>()){
				GameObjectManager.addComponent<Dropped>(act.gameObject);
			}
			//destroy history
			GameObject.Destroy(gameData.actionsHistory);
			LayoutRebuilder.ForceRebuildLayoutImmediate(editableScriptContainer.GetComponent<RectTransform>());
		}
	}


	// ????
	private void restoreLastEditedScript(){
		List<Transform> childrenList = new List<Transform>();
		if(lastEditedScript != null)
        {
			foreach (Transform child in lastEditedScript.transform)
			{
				childrenList.Add(child);
			}
			foreach (Transform child in childrenList)
			{
				child.SetParent(editableScriptContainer.transform);
				GameObjectManager.bind(child.gameObject);
			}
			GameObjectManager.refresh(editableScriptContainer);
		}
	}


	// Lors d'une fin d'exécution de séquence, gére les différents éléments à ré-afficher ou si il faut sauvegarder que la progression du joueur
	private void levelFinished (GameObject go){
		// On réaffiche les différent panel pour la création de séquence
		setExecutionState(true);

		// En cas de fin de niveau
		if(go.GetComponent<NewEnd>().endType == NewEnd.Win){
			// Affichage de l'historique de l'ensemble des actions exécutés
			loadHistory();
			// Sauvegarde de l'état d'avancement des niveaux pour le jour (niveau et étoile)
			PlayerPrefs.SetInt(gameData.levelToLoad.Item1, gameData.levelToLoad.Item2+1);
			PlayerPrefs.Save();
		}
		else if(go.GetComponent<NewEnd>().endType == NewEnd.Detected){
			//copy player container into editable container
			restoreLastEditedScript();
		}
		else if (go.GetComponent<NewEnd>().endType == NewEnd.BadCondition)
		{
			//copy player container into editable container
			restoreLastEditedScript();
		}
	}


	// ?????
	private void linkTo(GameObject go){
		if(go.GetComponent<UIActionType>().linkedTo == null){
			if(go.GetComponent<BasicAction>()){
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find(go.GetComponent<BasicAction>().actionType.ToString());
			}			
			else if(go.GetComponent<IfAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("If");
			else if(go.GetComponent<ForAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("For");
		}
	}


	// Permet la gestion de l'affiche du panel de fin de niveau
    private void displayEndPanel(GameObject endPanel)
    {
        GameObjectManager.setGameObjectState(endPanel.transform.parent.gameObject, true);
    }


	// Permet de switcher entre les affichages différents de fin de niveau
	// Cas 1 : Un ennemie à repéré le robot
	// Cas 2 : Le robot est sortie du labyrinth
	// Cas 3 : Le joueur à mal remplit une condition
    private void onDisplayedEndPanel (GameObject endPanel)
    { 
        switch (endPanel.GetComponent<NewEnd>().endType)
        {
            case 1:
                endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().text = "Vous avez été repéré !";
                GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, false);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadLevel").gameObject, true);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, true);
				endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/LoseSound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = true;
                endPanel.GetComponent<AudioSource>().Play();
                break;
            case 2: 
				int score = (10000 / (gameData.totalActionBloc + 1) + 5000 / (gameData.totalStep + 1) + 6000 / (gameData.totalExecute + 1) + 5000 * gameData.totalCoin);
                Transform verticalCanvas = endPanel.transform.Find("VerticalCanvas");
				verticalCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "Bravo vous avez gagné !\nScore: " + score;
                setScoreStars(score, verticalCanvas.Find("ScoreCanvas"));

				endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/VictorySound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = false;
                endPanel.GetComponent<AudioSource>().Play();
				GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, true);
                //End
                if (gameData.levelToLoad.Item2 >= gameData.levelList[gameData.levelToLoad.Item1].Count - 1)
                {
                    GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, false);
					GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadLevel").gameObject, true);
					GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, false);
                }
                break;
			case 3:
				endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().text = "Une condition est mal remplie !";
				GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, false);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadLevel").gameObject, false);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, true);
				endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/LoseSound") as AudioClip;
				endPanel.GetComponent<AudioSource>().loop = true;
				endPanel.GetComponent<AudioSource>().Play();
				break;
        }
    }

	// Gére le nombre d'étoile à afficher selon le score obtenue
	private void setScoreStars(int score, Transform scoreCanvas){
		// Détermine le nombre d'étoile à afficher
		int scoredStars = 0;
		if(gameData.levelToLoadScore != null){
			//check 0, 1, 2 or 3 stars
			if(score >= gameData.levelToLoadScore[0]){
				scoredStars = 3;
			}
			else if(score >= gameData.levelToLoadScore[1]){
				scoredStars = 2;
			}
			else {
				scoredStars = 1;
			}			
		}
		
		// Affiche le nombre d'étoile désiré
		for (int nbStar = 0 ; nbStar < 4 ; nbStar++){
			if(nbStar == scoredStars)
				GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, true);
			else
				GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, false);
		}

		//save score only if better score
		int savedScore = PlayerPrefs.GetInt(gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+gameData.levelToLoad.Item2+gameData.scoreKey, 0);
		if(savedScore < scoredStars){
			PlayerPrefs.SetInt(gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+gameData.levelToLoad.Item2+gameData.scoreKey, scoredStars);
			PlayerPrefs.Save();			
		}
	}


	// Empty the script window
	// See ResetButton in editor
	public void resetScript(bool refund = false){
		// On récupére le contenaire pointer lors du clique poubelle
		GameObject scriptContainerPointer = viewportContainerPointed_f.First().transform.Find("ScriptContainer").gameObject;

		// On parcourt le script container pour détruire toutes les actions
		for (int i = 0 ; i < scriptContainerPointer.transform.childCount ; i++){
			if (scriptContainerPointer.transform.GetChild(i).GetComponent<BaseElement>()){
				destroyScript(scriptContainerPointer.transform.GetChild(i).gameObject, refund);				
			}
		
		}
		refreshUIButton();
	}


	//Recursive script destroyer
	private void destroyScript(GameObject go,  bool refund = false){
		GameObjectManager.unbind(go);
		UnityEngine.Object.Destroy(go);
	}


	// Affiche l'image associée au dialogue
	public void setImageSprite(Image img, string path){
		if (Application.platform == RuntimePlatform.WebGLPlayer)
		{
			MainLoop.instance.StartCoroutine(GetTextureWebRequest(img, path));
		}
		else
		{
			Texture2D tex2D = new Texture2D(2, 2); //create new "empty" texture
			byte[] fileData = File.ReadAllBytes(path); //load image from SPY/path
			if (tex2D.LoadImage(fileData))
				img.sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0, 0), 100.0f);
		}
	}


	// Affiche le panneau de dialoge au début de niveau (si besoin)
	public void showDialogPanel(){
		GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, true);
		nDialog = 0;
		dialogPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[0].Item1;
		GameObject imageGO = dialogPanel.transform.Find("Image").gameObject;
		if(gameData.dialogMessage[0].Item2 != null){
			GameObjectManager.setGameObjectState(imageGO,true);
			setImageSprite(imageGO.GetComponent<Image>(), Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Levels" +
			Path.DirectorySeparatorChar + gameData.levelToLoad.Item1 + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + gameData.dialogMessage[0].Item2);
		}
		else
			GameObjectManager.setGameObjectState(imageGO,false);

		if(gameData.dialogMessage.Count > 1){
			setActiveOKButton(false);
			setActiveNextButton(true);
		}
		else{
			setActiveOKButton(true);
			setActiveNextButton(false);
		}
	}


	// See NextButton in editor
	// Permet d'afficher la suite du dialogue
	public void nextDialog(){
		nDialog++; // On incrémente le nombre de dialogue
		dialogPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[nDialog].Item1;
		GameObject imageGO = dialogPanel.transform.Find("Image").gameObject;
		if(gameData.dialogMessage[nDialog].Item2 != null){
			GameObjectManager.setGameObjectState(imageGO,true);
			setImageSprite(imageGO.GetComponent<Image>(), Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Levels" +
			Path.DirectorySeparatorChar + gameData.levelToLoad.Item1 + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + gameData.dialogMessage[nDialog].Item2);
		}
		else
			GameObjectManager.setGameObjectState(imageGO,false);

		// Si il reste des dialogue à afficher ensuite
		if(nDialog + 1 < gameData.dialogMessage.Count){
			setActiveOKButton(false);
			setActiveNextButton(true);
		}
		else{
			setActiveOKButton(true);
			setActiveNextButton(false);
		}
	}


	// Active ou non le bouton Ok du panel dialogue
	public void setActiveOKButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.Find("Buttons").Find("OKButton").gameObject, active);
	}


	// Active ou non le bouton next du panle dialogue
	public void setActiveNextButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.Find("Buttons").Find("NextButton").gameObject, active);
	}


	// See OKButton in editor
	// Désactive le panel de dialogue et réinitialise le nombre de dialogue à 0
	public void closeDialogPanel(){
		nDialog = 0;
		gameData.dialogMessage = new List<(string,string)>();
		GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, false);
	}


	// Permet de relaner le niveau au début
	public void restartScene(){
		initZeroVariableLevel();
		GameObjectManager.loadScene("MainScene");
	}


	// See TitleScreen and ScreenTitle buttons in editor
	// Permet de revenir à la scéne titre
	public void returnToTitleScreen(){
		initZeroVariableLevel();
		gameData.actionsHistory = null;
		GameObjectManager.loadScene("TitleScreen");
	}


	// Permet de réinitialiser les variables du niveau dans l'objet gameData
	public void initZeroVariableLevel()
    {
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string, string)>();
	}


	// See NextLevel button in editor
	// On charge la scéne suivante
	public void nextLevel(){
		// On imcrémente le numéro du niveau
		gameData.levelToLoad.Item2++;
		// On efface l'historique
		gameData.actionsHistory = null;
		// On recharge la scéne (mais avec le nouveau numéro de niveau)
		restartScene();
	}


	// See ReloadLevel and RestartLevel buttons in editor
	// Fait recommencé la scéne mais en gardant l'historique des actions
	public void retry(){
		if (gameData.actionsHistory != null)
			UnityEngine.Object.DontDestroyOnLoad(gameData.actionsHistory);
		restartScene();
	}
	

	// ????
	public void reloadState(){
		GameObjectManager.removeComponent<NewEnd>(endPanel);
	}

	// See StopButton in editor
	// ??????
	public void stopScript(){
		restoreLastEditedScript();
		setExecutionState(true);
		CurrentAction act;
		foreach(GameObject go in currentActions){
			act = go.GetComponent<CurrentAction>();
			if(act.agent.CompareTag("Player"))
				GameObjectManager.removeComponent<CurrentAction>(go);
		}		
	}

	// See ExecuteButton in editor
	// Copie les blocks d'actions dans le container du robot associer
	public void applyScriptToPlayer(){
		//if first click on play button
		if (!buttonStop.activeInHierarchy) {
			// On note une tentative d'execution en plus
			gameData.totalExecute++;
			//hide library panels
			GameObjectManager.setGameObjectState(libraryPanel, false);
			GameObjectManager.setGameObjectState(canvas.transform.Find("Scrollbar Vertical").gameObject, false);
			//editable viewport and scrollbar
			GameObjectManager.setGameObjectState(EditableContainer.transform.Find("EditableContainers").gameObject, false);
			GameObjectManager.setGameObjectState(EditableContainer.transform.Find("Scrollbar Vertical").gameObject, false);
			//clean container for each robot and copy the new sequence
			foreach (GameObject robot in playerGO) {
				// Clean robot container
				foreach (Transform child in robot.GetComponent<ScriptRef>().scriptContainer.transform) {
					GameObjectManager.unbind(child.gameObject);
					GameObject.Destroy(child.gameObject);
				}

				//copy editable script
				GameObject containerAssocied = null;
				// On parcourt les script container pour identifer celui associé au robot 
				foreach (GameObject container in viewportContainer_f)
				{
					// Si le container comporte le même nom que le robot
					if (container.GetComponentInChildren<UITypeContainer>().associedAgentName == robot.GetComponent<AgentEdit>().agentName)
                    {
						// On recupére le container qui contiend le script à associer au robot
						containerAssocied = container.transform.Find("ScriptContainer").gameObject;

					}
				}
				// Si on a bien trouvé un container associer
				if(containerAssocied != null)
                {
					// On va copier la sequence créer par le joueur dans le container de la fenêtre du robot
					// On commence par créer une copie du container ou se trouve la sequence
					GameObject containerCopy = CopyActionsFrom(containerAssocied, false, robot);
					// On recupere le container de la fenêtre du robot qui contiendra la sequence à executer
					GameObject targetContainer = robot.GetComponent<ScriptRef>().scriptContainer;
					// On fait apparaitre le panneau du robot
					robot.GetComponent<ScriptRef>().uiContainer.transform.Find("Header").Find("Toggle").GetComponent<Toggle>().isOn = true;
					// On copie les actions dans 
					for (int i = 0; i < containerCopy.transform.childCount; i++)
					{
						// Les blocs du script container à ne pas faire apparaitre dans la fiche de l'agent
						// La name text, le container name et le block end zone du container
						if (!containerCopy.transform.GetChild(i).name.Contains("ContainerName") && !containerCopy.transform.GetChild(i).name.Contains("EndZoneActionBloc") && !containerCopy.transform.GetChild(i).name.Contains("NameText"))
						{
							Transform child = null;
							// Si c'est un block special (for, if)
							if (containerCopy.transform.GetChild(i).GetComponent<ForAction>() || containerCopy.transform.GetChild(i).GetComponent<IfAction>())
							{
								child = CopySpecialBlock(containerCopy.transform.GetChild(i));
							}
                            else
                            {
								child = UnityEngine.GameObject.Instantiate(containerCopy.transform.GetChild(i));
							}
							child.SetParent(targetContainer.transform);
							GameObjectManager.bind(child.gameObject);
							GameObjectManager.refresh(targetContainer);
						}

					}
					// Va linker les blocs ensemble
					// C'est à dire qu'il va définir pour chaque bloc, qu'elle est le suivant à exécuté
					LevelGenerator.computeNext(targetContainer);
					// On détruit la copy de la sequence d'action
					UnityEngine.Object.Destroy(containerCopy);
				}

			}
			// Lancement du son 
			buttonPlay.GetComponent<AudioSource>().Play();
			// On harmonise l'affichage de l'UI container de l'agent
			foreach(GameObject go in agents){
				LayoutRebuilder.ForceRebuildLayoutImmediate(go.GetComponent<ScriptRef>().uiContainer.GetComponent<RectTransform>());
				if(go.CompareTag("Player")){				
					GameObjectManager.setGameObjectState(go.GetComponent<ScriptRef>().uiContainer,true);				
				}
			}
		}
	}

	/**
	 * On copie le container qui contient la sequence d'actions pour modifier les parametre des block speciaux
	 * Param:
	 *	Container (GameObject) : Le container qui contient le script à copier
	 *	isInteractable (bool) : Si le script copié peut contenir des éléments interactable (sinon l'interaction sera desactivé)
	 *	agent (GameObject) : L'agent sur qui l'on va copier la sequence (pour définir la couleur)
	 * 
	 **/
	public GameObject CopyActionsFrom(GameObject container, bool isInteractable, GameObject agent){
		// On va travailler avec une copy du container
		GameObject copyGO = GameObject.Instantiate(container); 
		//Pour tous les élément interactible, on va les désactiver/activer selon le paramétrage
		foreach(TMP_Dropdown drop in copyGO.GetComponentsInChildren<TMP_Dropdown>()){
			drop.interactable = isInteractable;
		}
		foreach(TMP_InputField input in copyGO.GetComponentsInChildren<TMP_InputField>()){
			input.interactable = isInteractable;
		}

		// Pour chaque bloc for
		foreach(ForAction forAct in copyGO.GetComponentsInChildren<ForAction>()){
			// Si activer, on note combien a quel boucle on est sur combien de boucle à faire
			if(!isInteractable && !forAct.gameObject.GetComponent<WhileAction>())
			{
				forAct.nbFor = int.Parse(forAct.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text);
				forAct.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = (forAct.currentFor).ToString() + " / " + forAct.nbFor.ToString();		
			}// Sinon on met tous à 0
			else if(isInteractable && !forAct.gameObject.GetComponent<WhileAction>())
			{
				forAct.currentFor = 0;
				forAct.gameObject.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = forAct.nbFor.ToString();
			}
			else if (forAct.gameObject.GetComponent<WhileAction>())
            {
				if (!forAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject.GetComponent<EndBlockScriptComponent>())
				{
					// On traduit la condition en string
					forAct.gameObject.GetComponent<WhileAction>().condition = ConditionManagement.instance.convertionConditionSequence(forAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject, new string[] { });
				}
				else
				{
					GameObjectManager.addComponent<NewEnd>(endPanel, new { endType = NewEnd.BadCondition });
				}
			}
			// On parcourt les éléments présent dans le block action
			bool firstElement = false;
			foreach(BaseElement act in forAct.GetComponentsInChildren<BaseElement>()){
				// Si ce n'est pas un bloc action alors on le note comme premier élément puis on arrête le parcourt des éléments
				if(!act.Equals(forAct) && !firstElement){
					forAct.firstChild = act.gameObject;
					firstElement = true;
					break;
				}
			}
		}
		// Pour chaque block de boucle infini
		foreach (ForeverAction loopAct in copyGO.GetComponentsInChildren<ForeverAction>()){
			loopAct.firstChild = loopAct.gameObject.transform.Find("Container").GetChild(0).gameObject;
		}
		// Pour chaque block if
		foreach(IfAction IfAct in copyGO.GetComponentsInChildren<IfAction>()){
			//On vérifie que le bloc condition comporte un élément ou un opérator
			if (!IfAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject.GetComponent<EndBlockScriptComponent>())
			{
				// On traduit la condition en string
				IfAct.condition = ConditionManagement.instance.convertionConditionSequence(IfAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject, new string[] { });
			}
            else
            {
				GameObjectManager.addComponent<NewEnd>(endPanel, new { endType = NewEnd.BadCondition });
			}

			IfAct.firstChild = IfAct.gameObject.transform.Find("Container").GetChild(0).gameObject;
			//Si c'est un elseAction
			if (IfAct.gameObject.GetComponent<ElseAction>())
            {
				IfAct.gameObject.GetComponent<ElseAction>().elseFirstChild = IfAct.gameObject.GetComponent<ElseAction>().elsePart.transform.GetChild(0).gameObject;
			}
		}

		foreach(UITypeContainer typeContainer in copyGO.GetComponentsInChildren<UITypeContainer>()){
			typeContainer.enabled = isInteractable;
		}
		foreach(PointerSensitive pointerSensitive in copyGO.GetComponentsInChildren<PointerSensitive>()){
			pointerSensitive.enabled = isInteractable;
		}

		// On défini la couleur de l'action selon l'agent à qui appartiendra la script
		Color actionColor;
		switch(agent.tag){
			case "Player":
				actionColor = MainLoop.instance.GetComponent<AgentColor>().playerAction;
				break;
			case "Drone":
				actionColor = MainLoop.instance.GetComponent<AgentColor>().droneAction;
				break;
			default: // agent by default = robot
				actionColor = MainLoop.instance.GetComponent<AgentColor>().playerAction;
				break;
		}

		foreach(BasicAction act in copyGO.GetComponentsInChildren<BasicAction>()){
			act.gameObject.GetComponent<Image>().color = actionColor;
		}

		return copyGO;
	}

	/**
	 * Copy des contenus des block speciaux afin de les mettrent en forme correctement
	 * (On vire les end-zones, on met les conditions sous forme d'un seul bloc)
	 * Param:
	 *	specialBlock (GameObject) : Container qui contient ce qu'il faut copier
	 * Out:
	 *	newBlockForm (Transform) : Container épurer des éléments indésirables
	 * 
	 **/
	private Transform CopySpecialBlock(Transform specialBlock)
    {
		Transform newBlockForm = null;
        //Si c'est un block for on enléve juste la end zone
        if (specialBlock.GetComponent<ForAction>())
        {
			GameObject endZone = specialBlock.transform.Find("Container").GetChild(specialBlock.transform.Find("Container").childCount - 1).gameObject;
			endZone.transform.SetParent(null);
			GameObject.Destroy(endZone);

		}// Si c'est une block if on garde le container des actions (sans la end zone) mais le condition est traduite dans IfAction
		else if (specialBlock.GetComponent<IfAction>())
        {
			GameObject supChild = specialBlock.transform.Find("Container").GetChild(specialBlock.transform.Find("Container").childCount - 1).gameObject;
			supChild.transform.SetParent(null);
			GameObject.Destroy(supChild);

            if (specialBlock.GetComponent<ElseAction>())
            {
				supChild = specialBlock.GetComponent<ElseAction>().elsePart.transform.GetChild(specialBlock.GetComponent<ElseAction>().elsePart.transform.childCount - 1).gameObject;
				supChild.transform.SetParent(null);
				GameObject.Destroy(supChild);
			}
		}

		// On parcourt les block qui compose le container afin de voir si il n'est pas composé de block spécial
		foreach (Transform block in specialBlock.transform.Find("Container"))
        {
			// Si c'est le cas on fait une récursive
			if(block.GetComponent<ForAction>() || block.GetComponent<IfAction>())
            {
				Transform newFormBlock = CopySpecialBlock(block);
				newFormBlock.SetParent(block.parent);
				int index = block.GetSiblingIndex();// On note l'index du block que l'on va remplacer
				newFormBlock.SetSiblingIndex(index);
				GameObjectManager.bind(newFormBlock.gameObject);
				block.SetParent(null);
				GameObject.Destroy(block.gameObject);

				// Si c'est le premier block du container, alors il faut associer le nouveau block comme firstchild du block spécial sur lequel on travail
				if(index == 0)
                {
                    if (specialBlock.GetComponent<ForAction>())
                    {
						specialBlock.GetComponent<ForAction>().firstChild = newFormBlock.gameObject;
					}
                    else if (specialBlock.GetComponent<IfAction>())
                    {
						specialBlock.GetComponent<IfAction>().firstChild = newFormBlock.gameObject;
					}
                }
			}
        }

		newBlockForm = UnityEngine.GameObject.Instantiate(specialBlock.transform);
		return newBlockForm;
	}

	// Ajout un container à la scéne
	public void addContainer()
    {
		// On clone de viewport
		GameObject cloneContainer = Object.Instantiate(prefabViewportScriptContainer);
		// On l'ajoute à l'éditableContainer
		cloneContainer.transform.SetParent(EditableContainer.transform);
		// On regarde conbien de viewport container contient l'éditable pour mettre le nouveau viewport à la bonne position
		cloneContainer.transform.SetSiblingIndex(EditableContainer.GetComponent<EditableCanvacComponent>().nbViewportContainer);
		// On ajoute la caméra dans le bridge
		cloneContainer.GetComponent<CameraSystemBridge>().cameraAssociate = GameObject.Find("Main Camera");
		// Puis on imcrémente le nombre de viewport contenue dans l'éditable
		EditableContainer.GetComponent<EditableCanvacComponent>().nbViewportContainer += 1;
		// On ajoute le nouveau viewport container à FYFY
		GameObjectManager.bind(cloneContainer);

		// Lance le son de l'ajout d'un container
		cloneContainer.GetComponent<AudioSource>().Play();

		// Affiche le bon nom
		bool nameOk = false;
		for(int i = EditableContainer.GetComponent<EditableCanvacComponent>().nbViewportContainer; !nameOk; i++)
        {
			// Si le nom n'est pas déjà utilisé on nomme le nouveau container de cette façon
			if(!nameContainerUsed("Agent" + i))
            {
				cloneContainer.GetComponentInChildren<UITypeContainer>().associedAgentName = "Agent" + i;
				nameOk = true;
			}
		}
		MainLoop.instance.StartCoroutine(updateVerticalName(cloneContainer.GetComponentInChildren<UITypeContainer>().associedAgentName));
		MainLoop.instance.StartCoroutine(tcheckLinkName());

	}


	// Vérifie si le nom proposé existe déjà ou non pour un script container
	public bool nameContainerUsed(string nameTested) {
		bool nameUsed = false;
		// On regarde en premier lieu si le nom n'existe pas déjà
		foreach (GameObject container in scriptContainer_f)
		{
			if (container.GetComponent<UITypeContainer>().associedAgentName == nameTested)
			{
				nameUsed = true;
			}
		}

		return nameUsed;
	}
	
	// Change le nom du container
	public void newNameContainer(string name)
    {
		// Si le nom n'est pas utilisé
        if (!nameContainerUsed(name) && name.Length < 8)
        {
			// On cherche le container
			foreach (GameObject container in scriptContainer_f)
			{
				if (container.GetComponent<UITypeContainer>().editName)
				{
					// Si on trouve celui dont le nom du container selectionné correspond
					if (container.GetComponent<UITypeContainer>().associedAgentName == nameContainerSelected)
					{
						string oldName = container.GetComponent<UITypeContainer>().associedAgentName;
						// On change pour son nouveau nom
						container.GetComponent<UITypeContainer>().associedAgentName = name;
						// Puis on l'affiche verticalement
						verticalName(name);
						// On envoie au systéme sur quel agent on va modifie les données
						bool agentExist = EditAgentSystem.instance.modificationAgent(oldName);
						// Si l'agent existe, on met à jours son lien (on supprime le lien actuelle)
						if (agentExist)
						{
							// Si le changement de nom entre l'agent et le container est automatique, on change aussi le nom de l'agent
							if (container.GetComponent<UITypeContainer>().editNameAuto)
							{
								EditAgentSystem.instance.setAgentName(name);
							}
						}
						nameContainerSelected = container.GetComponent<UITypeContainer>().associedAgentName;
					}
				}
			}
		}
        else{ // Sinon on annule le changement
			cancelChangeNameContainer(name);
		}
	}

	// Udapte (au cycle update suivant) le nom du container à la veticale
	// Ceci afin d'être sur que toutes les modifications de l'update actuelle on été prise en compte
	public IEnumerator updateVerticalName(string name)
	{
		yield return null;
		verticalName(name);
	}

	// Afichage du nom du container à la verticale
	public void verticalName(string name)
    {
		// On recherhe le container qui contient le même nom
		foreach (GameObject container in scriptContainer_f)
        {
			// Si on le trouve, alors on change l'écriture du nom à la vertical
			if (container.GetComponent<UITypeContainer>().associedAgentName == name) {
				// On créer une variable pour stocker les modifications du nom
				string newViewName = "";
				for(int i = 0; i < name.Length; i++)
				{
					if(i != name.Length - 1)
                    {
						newViewName = newViewName + name[i] + "\n";
					}
                    else
                    {
						newViewName = newViewName + name[i];
					}
				}
				// On remplace le nom actuel par le nouveau format
				container.transform.Find("ContainerName").GetComponent<TMP_InputField>().text = newViewName;
			}
        }
		MainLoop.instance.StartCoroutine(tcheckLinkName());
	}


	// Affiche le nom du container de manniére horizontal
	public void horizontalName(string name)
    {
		// On recherhe le container qui contient le même nom
		foreach (GameObject container in scriptContainer_f)
		{
			// Si on le trouve, alors on change l'écriture du nom à l'horizontal
			if (container.transform.Find("ContainerName").GetComponent<TMP_InputField>().text == name && container.GetComponent<UITypeContainer>().editName)
			{
				// On remplace le nom actuel par le nouveau format
				container.transform.Find("ContainerName").GetComponent<TMP_InputField>().text = container.GetComponent<UITypeContainer>().associedAgentName;
				// On enregistre le nom du container selectionné
				nameContainerSelected = container.GetComponent<UITypeContainer>().associedAgentName;
			}
		}
	}

	// Utilisé surtout par les apelles extérieurs au systéme
	// Permet d'enregistrer le nom du container que l'on veux changé
	// Et lui changer son nom 
	public void setContainerName(string oldName, string newName)
    {
		nameContainerSelected = oldName;
		newNameContainer(newName);
	}

	public void noChangeName(string name)
    {
		foreach (GameObject container in scriptContainer_f)
		{
			if (container.transform.Find("ContainerName").GetComponent<TMP_InputField>().text == name && !container.GetComponent<UITypeContainer>().editName)
			{
				cancelChangeNameContainer(name);
			}
		}
	}

	// On annule le nouveau nom
	public void cancelChangeNameContainer(string name)
	{
		foreach (GameObject container in scriptContainer_f)
		{
			// Si le nom afficher du container et le même quand paramétre, mais pas son nom, on a bien le container non modifier
			if (container.transform.Find("ContainerName").GetComponent<TMP_InputField>().text == name)
			{
				// On réaffiche son ancien nom à la vertical
				verticalName(container.GetComponent<UITypeContainer>().associedAgentName);
			}
		}
	}

	// Permet d'activé ou désactivé le menu echap
	public void setActiveEscapeMenu()
    {
		// Si le menu est active, le désactive
        if (menuEchap.activeInHierarchy)
        {
			menuEchap.SetActive(false);
		}// Et inversement
        else
        {
			menuEchap.SetActive(true);
		}
    }


}