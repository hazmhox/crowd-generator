using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using System.IO;
using MeshVR;

// CrowdGenerator v1.0
// Crowd Generator for Virt-A-Mate

namespace CrowdGeneratorPlugin
{
    public class CrowdGenerator : MVRScript
    {
		// JSONStorables
		public JSONStorableBool ToggleEditTilesParameters;
		public JSONStorableBool ToggleAnimateTilesParameters;
		public JSONStorableBool ToggleMaterialParameters;
	
		public JSONStorableStringChooser CharacterToSpawn;
		protected List<string> CharactersChoices;
		public JSONStorableFloat CharactersSeed;
		public JSONStorableBool CharactersFindTheInterloper;
		public JSONStorableStringChooser CharactersMaterial;
		protected List<string> CharactersMaterialChoices;
		public JSONStorableColor CharactersMaterialColor;
		
		public JSONStorableFloat CrowdSize;		
		public JSONStorableFloat CrowdLines;
		public JSONStorableFloat CrowdMinDistance;
		public JSONStorableFloat CrowdRandOffset;
		public JSONStorableFloat CrowdRandRotation;
		public JSONStorableBool CrowdIsCircular;
		public JSONStorableFloat CrowdRadius;
		public JSONStorableFloat CrowdCircumference;
		public JSONStorableBool CrowdFlipRotation;
		public JSONStorableBool CrowdIsUniform;
		public JSONStorableFloat CrowdSeed;
		public JSONStorableFloat CrowdIdleChance;
		
		public JSONStorableStringChooser AnimationToUse;
		protected List<string> AnimationsList;
		
		public JSONStorableStringChooser AnimationMirroring;
		protected List<string> AnimationMirroringList;
		
		public JSONStorableBool AnimationIsUniform;
		public JSONStorableFloat AnimSpeed;
		public JSONStorableFloat AnimSeed;
		
		private JSONStorableString _loadingInfos;
		private JSONStorableString _helpText;
				
		// Tiles for the animation
		protected List<Transform> CurrentTilesList;
		protected Transform[] CurrentTilesArray;

		// Game Objects and components
		private Transform CrowdCharasRoot;
		private Transform CrowdCharasUniqueRoot;
		private Transform CrowdCharasNakedRoot;
		private Transform CrowdMaterialsRoot;
		private Transform CrowdRoot;
		
		protected List<Transform> CharactersPool;
		protected List<Transform> CharactersUniquePool;
		protected List<Transform> CharactersNakedPool;
		protected List<Transform> CharactersSelectionPool;
		protected List<Material> MaterialPool;
		
		// Security and initialization
		private bool editorInitialized = false;
		private int initLoops = 0;
		private int maxInitLoops = 500;
		private bool cuaIsValid = false;
		private bool animRoutineRunning = false;
		private bool interloperSpawned = false;
		
		// UI Elements		
		private UIDynamic HelpUIElement;
		private UIDynamic[] colorUIElements;
		
		private UIDynamic[] charactersParamsUIElements;
		private UIDynamic[] crowdParamsUIElements;
		private UIDynamic[] animParamsUIElements;
		private UIDynamic[] seedParamsUIElements;
		
		private List<UIDynamicColorPicker> allUIColorPickers;
		private List<UIDynamicSlider> allUISliders;
		private List<UIDynamicToggle> allUIToggles;
		private List<UIDynamicTextField> allUITextFields;
		private List<UIDynamicButton> allUIButtons;
		private List<UIDynamic> allUISpacers;
			
		public override void Init()
        {			 
            try
            {
                if (containingAtom.type != "CustomUnityAsset")
                {
                    SuperController.LogError("Please add CrowdGenerator on a Custom Unity Asset");
                    return;
                }

				/* *************** LISTS INIT ***************** */
				CharactersChoices = new List<string>();
				CharactersChoices.Add("All");
				
				CharactersMaterialChoices = new List<string>();
				CharactersMaterialChoices.Add("Default");
				CharactersMaterialChoices.Add("White Lit");
				CharactersMaterialChoices.Add("White Unlit");
				
				AnimationsList = new List<string>();
				AnimationsList.Add("All");
				AnimationsList.Add("Idle Breathing");
				AnimationsList.Add("Idle Happy");
				AnimationsList.Add("Idle Standing");
				AnimationsList.Add("Idle Standing Annoyed");
				AnimationsList.Add("Cheering Low Energy");
				AnimationsList.Add("Cheering Mid Energy");
				AnimationsList.Add("Cheering High Energy");
				AnimationsList.Add("Clapping Low Energy");
				AnimationsList.Add("Clapping Mid Energy");
				AnimationsList.Add("Clapping High Energy");			
				AnimationsList.Add("Headbang 01 Low Energy");
				AnimationsList.Add("Headbang 01 Mid Energy");
				AnimationsList.Add("Headbang 01 High Energy");
				AnimationsList.Add("Headbang 02 Low Energy");
				AnimationsList.Add("Headbang 02 Mid Energy");
				AnimationsList.Add("Headbang 02 High Energy");
				
				AnimationMirroringList = new List<string>();
				AnimationMirroringList.Add("Random");
				AnimationMirroringList.Add("Original");
				AnimationMirroringList.Add("Mirrored");
				
				/* ************* COLORS ************** */
				HSVColor hsvcDefault = HSVColorPicker.RGBToHSV(1f, 1f, 1f);
				
				
				/* ************* JSONSTORABLES ************** */
				CharacterToSpawn = new JSONStorableStringChooser("Character", CharactersChoices, "All", "Character");
				CharacterToSpawn.setCallbackFunction += (val) => { CharacterToSpawn.valNoCallback = val; CreateCrowd(); };

				CharactersMaterial = new JSONStorableStringChooser("Characters Material", CharactersMaterialChoices, "Default", "Characters Material");
				CharactersMaterial.setCallbackFunction += (val) => { CharactersMaterial.valNoCallback = val; CreateCrowd(); };

				CharactersMaterialColor = new JSONStorableColor("Material Color", hsvcDefault);
				CharactersMaterialColor.setCallbackFunction += (h,s,v) => {
					var newHSV = default(HSVColor);
					newHSV.H = h;
					newHSV.S = s;
					newHSV.V = v;
					CharactersMaterialColor.valNoCallback = newHSV;
					CreateCrowd();
				};
				
				CharactersSeed = new JSONStorableFloat("Characters Seed", 0, 0f, 150000f);
				CharactersSeed.setCallbackFunction += (val) => { CharactersSeed.valNoCallback = Mathf.Round(val); CreateCrowd(); };
				
				CharactersFindTheInterloper = new JSONStorableBool("Find the Interloper!", false);
				CharactersFindTheInterloper.setCallbackFunction += (val) => { CharactersFindTheInterloper.valNoCallback = val; CreateCrowd(); };
				
				CrowdSize = new JSONStorableFloat("Crowd Size", 30f, 1f, 100f);
				CrowdSize.setCallbackFunction += (val) => { CrowdSize.valNoCallback = Mathf.Round(val); CreateCrowd(); }; // This is meant to avoid creating a specific callback for all the storables and create the tiles immediately
				
				CrowdLines = new JSONStorableFloat("Crowd Lines", 5f, 1f, 10f);
				CrowdLines.setCallbackFunction += (val) => { CrowdLines.valNoCallback = Mathf.Round(val); CreateCrowd(); };

				CrowdMinDistance = new JSONStorableFloat("Crowd Min Distance", 1f, 0.1f, 5f);
				CrowdMinDistance.setCallbackFunction += (val) => { CrowdMinDistance.valNoCallback = val; CreateCrowd(); };
				
				CrowdRandOffset = new JSONStorableFloat("Crowd Random Offset", 0.2f, 0f, 5f);
				CrowdRandOffset.setCallbackFunction += (val) => { CrowdRandOffset.valNoCallback = val; CreateCrowd(); };
				
				CrowdRandRotation = new JSONStorableFloat("Crowd Random Rotation", 15f, 0f, 90f);
				CrowdRandRotation.setCallbackFunction += (val) => { CrowdRandRotation.valNoCallback = val; CreateCrowd(); };

				CrowdIsCircular = new JSONStorableBool("Generate Circular Crowd", false);
				CrowdIsCircular.setCallbackFunction += (val) => { CrowdIsCircular.valNoCallback = val; CreateCrowd(); };

				CrowdRadius = new JSONStorableFloat("Crowd Radius", 0.5f, 0f, 5f);
				CrowdRadius.setCallbackFunction += (val) => { CrowdRadius.valNoCallback = val; CreateCrowd(); };
				
				CrowdCircumference = new JSONStorableFloat("Crowd Circumference", 30f, 0f, 360f);
				CrowdCircumference.setCallbackFunction += (val) => { CrowdCircumference.valNoCallback = val; CreateCrowd(); };
				
				CrowdFlipRotation = new JSONStorableBool("Flip Crowd Rotation", false);
				CrowdFlipRotation.setCallbackFunction += (val) => { CrowdFlipRotation.valNoCallback = val; CreateCrowd(); };
				
				CrowdIsUniform = new JSONStorableBool("Generate Uniform Crowd", false);
				CrowdIsUniform.setCallbackFunction += (val) => { CrowdIsUniform.valNoCallback = val; CreateCrowd(); };
				
				CrowdSeed = new JSONStorableFloat("Crowd Seed", 0, 0f, 150000f);
				CrowdSeed.setCallbackFunction += (val) => { CrowdSeed.valNoCallback = Mathf.Round(val); CreateCrowd(); };

				CrowdIdleChance = new JSONStorableFloat("Crowd Idle Chance", 0.2f, 0f, 1f);
				CrowdIdleChance.setCallbackFunction += (val) => { CrowdIdleChance.valNoCallback = val; CreateCrowd(); };

				AnimationToUse = new JSONStorableStringChooser("Animation", AnimationsList, "All", "Animation");
				AnimationToUse.setCallbackFunction += (val) => { AnimationToUse.valNoCallback = val; CreateCrowd(); };
				
				AnimationMirroring = new JSONStorableStringChooser("Mirroring", AnimationMirroringList, "Random", "Mirroring");
				AnimationMirroring.setCallbackFunction += (val) => { AnimationMirroring.valNoCallback = val; CreateCrowd(); };

				AnimationIsUniform = new JSONStorableBool("Uniform animation", false);
				AnimationIsUniform.setCallbackFunction += (val) => { AnimationIsUniform.valNoCallback = val; CreateCrowd(); };
				
				AnimSpeed = new JSONStorableFloat("Animation Speed", 1f, 0f, 2f);
				AnimSpeed.setCallbackFunction += (val) => { AnimSpeed.valNoCallback = val; CreateCrowd(); };
				
				AnimSeed = new JSONStorableFloat("Animation Seed", 0, 0f, 150000f);
				AnimSeed.setCallbackFunction += (val) => { AnimSeed.valNoCallback = Mathf.Round(val); CreateCrowd(); };
				
				// Help!
				_helpText = new JSONStorableString("Help",
					"<color=#000><size=35><b>Crowd Generator</b></size></color>\n\n" + 
					"<color=#333><size=32>" +
					"This editor allows you to customize how your crowd is generated.\n\n" +
					"<b>Character settings</b>\nAll parameters to configure how the characters are selected.\n\n<i>Find The Interloper</i> makes a unique, strange, peculiar character appear in the crowd. (Only one is available at the moment)\n\n" +
					"<b>Crowd settings</b>\nAll parameters to configure how the crowd is generated.\n\n" +
					"<b>Animation parameters</b>\nAll parameters to configure how the characters in the crowd are animated.\n\n" +
					"<b>Seed parameters</b>\nControls how the characters, crowd and animation looks and keep it when reloading the scene.\n\nA seed is used to produce random numbers, if the seed is always the same, the random numbers will always be the same. The number doesn't matter, just select a value and check how your crowd looks.\n\nIt will allow you to keep a consistent crowd everytime the scene is reloaded. You can have a seed for each part of the crowd generator. If a seed is at zero it means it will be random everytime the scene is loaded.\n\n" +
					"</size></color>"
				);
							
				RegisterStringChooser(CharacterToSpawn);
				RegisterFloat(CharactersSeed);
				RegisterBool(CharactersFindTheInterloper);
				RegisterStringChooser(CharactersMaterial);
				RegisterColor(CharactersMaterialColor);
				
				RegisterFloat(CrowdSize);
				RegisterFloat(CrowdLines);
				RegisterFloat(CrowdMinDistance);
				RegisterFloat(CrowdRandOffset);
				RegisterFloat(CrowdRandRotation);
				RegisterBool(CrowdIsCircular);
				RegisterFloat(CrowdRadius);
				RegisterFloat(CrowdCircumference);
				RegisterBool(CrowdFlipRotation);
				RegisterBool(CrowdIsUniform);
				RegisterFloat(CrowdSeed);
				RegisterFloat(CrowdIdleChance);
				
				RegisterStringChooser(AnimationToUse);
				RegisterStringChooser(AnimationMirroring);
				RegisterBool(AnimationIsUniform);
				RegisterFloat(AnimSpeed);
				RegisterFloat(AnimSeed);
				
				// Creating the default UI that will inform you that the thing is loading
				CreateLoadingUI();
				
			}
            catch(Exception e)
            {
                SuperController.LogError("CrowdGenerator - Exception caught: " + e);
            }
		}
		
		void Start()
        {

        }
		
		void Update() {
			// If the editor is not initialized, we're gonna try to do it
			// I'm putting 3600 tries ( at 60fps it is 1min )
			if( initLoops < maxInitLoops && editorInitialized == false ) {
				initLoops++;
				// If our CUA is loading and everything is ok, we can remove the loading UI and Init the final UI
				if (InitEditor() == true)
				{
					RemoveLoadingUI();
					cuaIsValid = true; // Our CUA is initialized, we consider it as valid
					StartCoroutine("CheckCUAStatus"); // We're gonna check if the CUA is valid every 5 secs
					
					CreateCrowd();
					
				}
			}
			
			// Triggering an error if we have reached our limit of tries
			if( initLoops == maxInitLoops && editorInitialized == false ) {
				initLoops++; // To avoid spamming the log with the error
			}
		}
		
		private bool InitEditor() {
			// Updating our loading UI
			UpdateLoadingUI();

			if( editorInitialized == true ) return true;
			
			// Source characters root
			CrowdCharasRoot = getChildRoot( containingAtom.reParentObject, "crowd_charas" );
			if( CrowdCharasRoot == null )
			{
				return false;
			}
			CrowdCharasRoot.gameObject.SetActive(false);
			
			// Source characters root (unique)
			CrowdCharasUniqueRoot = getChildRoot( containingAtom.reParentObject, "crowd_charas_unique" );
			if( CrowdCharasUniqueRoot == null )
			{
				return false;
			}
			CrowdCharasUniqueRoot.gameObject.SetActive(false);
			
			// Source characters root (naked)
			CrowdCharasNakedRoot = getChildRoot( containingAtom.reParentObject, "crowd_charas_naked" );
			if( CrowdCharasNakedRoot == null )
			{
				return false;
			}
			CrowdCharasNakedRoot.gameObject.SetActive(false);
			
			// Source materials root
			CrowdMaterialsRoot = getChildRoot( containingAtom.reParentObject, "crowd_materials" );
			if( CrowdMaterialsRoot == null )
			{
				return false;
			}
			CrowdMaterialsRoot.gameObject.SetActive(false);

			// Caching all characters
			CharactersPool = new List<Transform>();
			for( var i = 0; i < CrowdCharasRoot.childCount; i++ ) {
				var tmpTr = CrowdCharasRoot.GetChild( i );
				CharactersPool.Add( tmpTr );
				CharactersChoices.Add( tmpTr.gameObject.name );
			}
			
			CharactersUniquePool = new List<Transform>();
			for( var i = 0; i < CrowdCharasUniqueRoot.childCount; i++ ) {
				var tmpTr = CrowdCharasUniqueRoot.GetChild( i );
				CharactersUniquePool.Add( tmpTr );
				CharactersChoices.Add( tmpTr.gameObject.name );
			}
			
			CharactersNakedPool = new List<Transform>();
			for( var i = 0; i < CrowdCharasNakedRoot.childCount; i++ ) {
				var tmpTr = CrowdCharasNakedRoot.GetChild( i );
				CharactersNakedPool.Add( tmpTr );
				CharactersChoices.Add( tmpTr.gameObject.name );
			}
			
			// Caching all materials
			MaterialPool = new List<Material>();
			for( var i = 0; i < CrowdMaterialsRoot.childCount; i++ ) {
				var tmpTr = CrowdMaterialsRoot.GetChild( i );
				Renderer tmpTRRend = tmpTr.GetComponent<Renderer>();
				MaterialPool.Add(tmpTRRend.material);				
			}
			
			// The root where the characters are gonna be created
			CrowdRoot = getChildRoot( containingAtom.reParentObject, "crowd_root" );
			if( CrowdRoot == null )
			{
				return false;
			}

			editorInitialized = true;
			CreateCUAUI();
			
			return true;

		}
		
		// The function creating the loading UI before everything is ready
		private void CreateLoadingUI()
		{
			_loadingInfos = new JSONStorableString("Loading Infos", "");
			UIDynamic stateinfosTextfield = CreateTextField(_loadingInfos, false);
			stateinfosTextfield.height = 450.0f;
		}

		// The function clearing the loading UI
		private void RemoveLoadingUI()
		{
			// Removing the loading text field on the left of the UI
			RemoveTextField(_loadingInfos);
		}

		// The function updating the loading UI while we're either waiting for the CUA to load, or waiting for the root object and everything to be found
		private void UpdateLoadingUI()
		{
			var loadingText = "<color=#000><b>Custom Unity Asset loading...</b></color>\n" + (initLoops/60) + "/" + (maxInitLoops/60) + "secs";
			if (initLoops == maxInitLoops)
			{
				loadingText += "\n\n<color=#333><b>UNABLE TO LOAD YOUR CUA :</b>\n";
				loadingText += "- Your CUA may not be compatible with this script\n";
				loadingText += "- Your CUA may take too long to load</color>";
				loadingText += "\n\nSelect another CUA and reload the script, or just reload the script to try again.";
			}
			_loadingInfos.val = loadingText;
		}
		
		// The function creating the UI when the CUA has been invalidated
		private void CreateCUAInvalidatedUI()
		{
			JSONStorableString _invalidatedCUA = new JSONStorableString("Invalidated CUA", "");
			UIDynamic stateinfosTextfield = CreateTextField(_invalidatedCUA, false);
			stateinfosTextfield.height = 150.0f;
			_invalidatedCUA.val = "<color=#000><b>Custom Unity Asset invalidated.</b></color>\n" +
			                      "You have changed the CUA in the <b>Asset</b> tab, the script is no longer valid. Please reload it or change it.";
		}

		// Function creating the whole CUA UI
		private void CreateCUAUI()
		{
			// Creating all the lists 
			allUIColorPickers = new List<UIDynamicColorPicker>();
			allUISliders = new List<UIDynamicSlider>();
			allUIToggles = new List<UIDynamicToggle>();
			allUITextFields = new List<UIDynamicTextField>();
			allUIButtons = new List<UIDynamicButton>();
			allUISpacers = new List<UIDynamic>();
			
			UIDynamicSlider tmpSlider;
			UIDynamicToggle tmpToggle;
			UIDynamicColorPicker tmpColor;
			UIDynamicTextField tmpTextfield;
			
			// Characters stuff
			charactersParamsUIElements = new UIDynamic[12];

			tmpTextfield = createStaticDescriptionText("Characters settings","<color=#000><size=35><b>Characters settings</b></size>\n<size=28>How are the characters generated</size></color>",false,75);
			allUITextFields.Add( tmpTextfield );
			charactersParamsUIElements[0] = (UIDynamic)tmpTextfield;
			
			UIDynamicPopup CTSdp = CreateScrollablePopup(CharacterToSpawn);
			CTSdp.labelWidth = 180f;
			charactersParamsUIElements[1] = (UIDynamic)CTSdp;
			
			UIDynamicPopup CMdp = CreateScrollablePopup(CharactersMaterial);
			CMdp.labelWidth = 180f;
			charactersParamsUIElements[2] = (UIDynamic)CMdp;
			
			tmpColor = CreateColorPicker(CharactersMaterialColor, true);
			allUIColorPickers.Add(tmpColor);
			charactersParamsUIElements[3] = (UIDynamic)tmpColor;
			
			tmpToggle = CreateToggle(CharactersFindTheInterloper);
			allUIToggles.Add(tmpToggle);
			charactersParamsUIElements[4] = (UIDynamic)tmpToggle;


			// Crowd settings
			crowdParamsUIElements = new UIDynamic[15];
			
			tmpTextfield = createStaticDescriptionText("Crowd settings","<color=#000><size=35><b>Crowd settings</b></size>\n<size=28>How the crowd is generated</size></color>",false,75);
			allUITextFields.Add( tmpTextfield );
			crowdParamsUIElements[0] = (UIDynamic)tmpTextfield;
			
			tmpSlider = CreateSlider(CrowdSize);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[1] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdLines);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[2] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdMinDistance);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[3] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdRandOffset);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[4] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdRandRotation);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[5] = (UIDynamic)tmpSlider;

			tmpToggle = CreateToggle(CrowdIsCircular,false);
			allUIToggles.Add(tmpToggle);
			crowdParamsUIElements[6] = (UIDynamic)tmpToggle;
			
			tmpSlider = CreateSlider(CrowdRadius);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[7] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdCircumference);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[8] = (UIDynamic)tmpSlider;
			
			tmpToggle = CreateToggle(CrowdFlipRotation,false);
			allUIToggles.Add(tmpToggle);
			crowdParamsUIElements[9] = (UIDynamic)tmpToggle;
			
			tmpToggle = CreateToggle(CrowdIsUniform,false);
			allUIToggles.Add(tmpToggle);
			crowdParamsUIElements[10] = (UIDynamic)tmpToggle;

			tmpSlider = CreateSlider(CrowdIdleChance);
			allUISliders.Add(tmpSlider);
			crowdParamsUIElements[11] = (UIDynamic)tmpSlider;
			
			// Anim settings
			animParamsUIElements = new UIDynamic[10];

			tmpTextfield = createStaticDescriptionText("Animation settings","<color=#000><size=35><b>Animation settings</b></size>\n<size=28>All animation related options</size></color>",false,75);
			allUITextFields.Add( tmpTextfield );
			animParamsUIElements[0] = (UIDynamic)tmpTextfield;
			
			UIDynamicPopup ATUdp = CreateScrollablePopup(AnimationToUse);
			ATUdp.labelWidth = 180f;
			animParamsUIElements[1] = (UIDynamic)ATUdp;
			
			UIDynamicPopup AMdp = CreateScrollablePopup(AnimationMirroring);
			AMdp.labelWidth = 180f;
			animParamsUIElements[2] = (UIDynamic)AMdp;
			
			tmpToggle = CreateToggle(AnimationIsUniform);
			allUIToggles.Add(tmpToggle);
			animParamsUIElements[3] = (UIDynamic)tmpToggle;
			
			tmpSlider = CreateSlider(AnimSpeed);
			allUISliders.Add(tmpSlider);
			animParamsUIElements[4] = (UIDynamic)tmpSlider;

			
			// Seeds settings
			seedParamsUIElements = new UIDynamic[4];
			
			tmpTextfield = createStaticDescriptionText("Seed settings","<color=#000><size=35><b>Seed settings</b></size>\n<size=28>Control how the crowd looks and keep it when reloading the scene</size></color>",false,115);
			allUITextFields.Add( tmpTextfield );
			seedParamsUIElements[0] = (UIDynamic)tmpTextfield;
			
			tmpSlider = CreateSlider(CharactersSeed);
			allUISliders.Add(tmpSlider);
			seedParamsUIElements[1] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(CrowdSeed);
			allUISliders.Add(tmpSlider);
			seedParamsUIElements[2] = (UIDynamic)tmpSlider;
			
			tmpSlider = CreateSlider(AnimSeed);
			allUISliders.Add(tmpSlider);
			seedParamsUIElements[3] = (UIDynamic)tmpSlider;
			
			
			
			HelpUIElement = CreateTextField(_helpText, true);
			HelpUIElement.height = 900.0f;
			
			//ToggleEditTilesParametersCallback(true);
			//ToggleAnimateTilesParametersCallback(false);
			//ToggleMaterialParametersCallback(false);
		}
		
		// Function clearing the whole CUA UI
		private void ClearCUAUI()
		{		
			foreach( UIDynamicColorPicker uiEl in allUIColorPickers ) {
				RemoveColorPicker(uiEl);
			}
			
			foreach( UIDynamicSlider uiEl in allUISliders ) {
				RemoveSlider(uiEl);
			}
			
			foreach( UIDynamicToggle uiEl in allUIToggles ) {
				RemoveToggle(uiEl);
			}
			
			foreach( UIDynamicTextField uiEl in allUITextFields ) {
				RemoveTextField(uiEl);
			}
			
			foreach( UIDynamic uiEl in allUISpacers ) {
				RemoveSpacer(uiEl);
			}
			
			foreach( UIDynamicButton uiEl in allUIButtons ) {
				RemoveButton(uiEl);
			}
			
			RemovePopup(CharacterToSpawn);
			RemovePopup(CharactersMaterial);
			RemovePopup(AnimationToUse);
			RemovePopup(AnimationMirroring);
			RemoveTextField( _helpText );
		}
			
		// **************************
		// Functions
		// **************************
		private void CreateCrowd() 
		{
			if( CrowdRoot == null ) return;
			
			// Resetting some values
			interloperSpawned = false;
					
			// Cleaning the parent which contains all characters
			foreach (Transform child in CrowdRoot)
			{
				Destroy(child.gameObject);
			}
			
			// Creating the global pool for character selection
			CharactersSelectionPool = new List<Transform>();
			CharactersSelectionPool.AddRange(CharactersPool); // We're always grabbing from the main pool
			
			// If we're grabbing a specific character, then we need to have them all available
			if( CharacterToSpawn.val != "All" ) {
				CharactersSelectionPool.AddRange(CharactersUniquePool);
				CharactersSelectionPool.AddRange(CharactersNakedPool);
			}
			
			float hOffset = 0f;
			float fOffset = 0f;
			float lineHorOffset = 0f;
			float lineRotOffset = 0f;
			float lineRadius = CrowdRadius.val;
			float xPos = 0f;
			float yPos = 0f;

			CurrentTilesList = new List<Transform>();
			Vector3 charaPos = new Vector3(0,0,0);
			Quaternion charaRot;
			int spawnedCharacters = 0;
			for( var i = 0; i < CrowdLines.val; i++ ) {
				var CharPerLine = Mathf.Ceil(CrowdSize.val / CrowdLines.val);
				for( var j = 0; j < CharPerLine; j++ ) {
					// Checking if we got a seed
					if( CrowdSeed.val > 0 ) UnityEngine.Random.seed = (int)CrowdSeed.val + spawnedCharacters;
					
					// Position and rotation depending on the type of spawn
					if( CrowdIsCircular.val == false ) {
						charaPos = GetNewCharacterPosition(hOffset,fOffset);
						charaRot = GetNewCharacterRotation();						
					} else {
						var rCount = CrowdCircumference.val < 360 ? CharPerLine - 1 : CharPerLine;
						float angle = j * ( CrowdCircumference.val * Mathf.PI / 180 ) / rCount;
						float cYRot = - ((CrowdCircumference.val / rCount) * j - 90f);
						if( CrowdFlipRotation.val == true ) cYRot += 180f;
						
						float addOffsetX = 0f;
						float addOffsetZ = 0f;
						if( CrowdIsUniform.val == false ) {
							addOffsetX = UnityEngine.Random.Range(-CrowdRandOffset.val, CrowdRandOffset.val);
							addOffsetZ = UnityEngine.Random.Range(-CrowdRandOffset.val, CrowdRandOffset.val);
						}
						xPos = Mathf.Cos(angle) * lineRadius + addOffsetX;
						yPos = Mathf.Sin(angle)*lineRadius + addOffsetZ;
						
						charaPos = new Vector3( xPos, 0f, yPos);
						charaRot = Quaternion.Euler( 0f, cYRot , 0f);
					}
					
					// Creating the character
					Transform clone = GetNewCharacterModel(spawnedCharacters);
					clone.gameObject.SetActive(true);
					clone = Instantiate(clone, CrowdRoot);
					clone.name = "character_"+i+"_"+j;
					clone.transform.localPosition = charaPos;
					clone.transform.localRotation = charaRot;
					
					SetCharacterMaterial(clone);
					
					// Animator
					Animator cloneAnimator = clone.GetComponent<Animator>();
					SetAnimatorParams(cloneAnimator, spawnedCharacters);
					
					hOffset += CrowdMinDistance.val;
					
					spawnedCharacters++;
					if( spawnedCharacters >= CrowdSize.val ) return;
				}
				
				if( CrowdIsCircular.val == true ) {
					lineRadius += CrowdMinDistance.val;
				}
				
				hOffset = 0;
				fOffset += CrowdMinDistance.val;
			}
			
			// When everything is create, converting the TR list to an array (faster iteration)
			CurrentTilesArray = CurrentTilesList.ToArray();
		}
		
		private Transform GetNewCharacterModel( int spawnedCharacters ) {
			int originalSeed =  UnityEngine.Random.seed;
			Transform newChara;
			if( CharacterToSpawn.val == "All" ) {
				if( CharactersSeed.val > 0 ) {
					UnityEngine.Random.seed = (int)CharactersSeed.val + spawnedCharacters;
				} else {
					// I need to "randomize it" again because the main loop enforce the seed
					UnityEngine.Random.seed = Time.frameCount + spawnedCharacters;
				}
				
				if( CharactersFindTheInterloper.val == false || interloperSpawned == true ) {
					newChara = CharactersSelectionPool[ UnityEngine.Random.Range(0, CharactersSelectionPool.Count) ];
				} else {
					// We still haven't spawned the interloper and we're on the last character, force it.
					if( spawnedCharacters == CrowdSize.val - 1 ) {
						newChara = CharactersUniquePool[ UnityEngine.Random.Range(0, CharactersUniquePool.Count) ];
					// Or else, let's try to spawn it randomly in the crowd
					} else {					
						if( UnityEngine.Random.Range(0, 1000) > 985 ) {
							newChara = CharactersUniquePool[ UnityEngine.Random.Range(0, CharactersUniquePool.Count) ];
							interloperSpawned = true;
						} else {
							newChara = CharactersSelectionPool[ UnityEngine.Random.Range(0, CharactersSelectionPool.Count) ];
						}
					}
				}
				
				// Restoring the main seed				
				UnityEngine.Random.seed = originalSeed;
			} else {
				newChara = CharactersSelectionPool[ CharactersChoices.IndexOf( CharacterToSpawn.val ) - 1 ];
			}
			return newChara;
		}
		
		private Vector3 GetNewCharacterPosition( float hOffset, float fOffset ) {
			float rndHOffset = 0f;
			float rndFOffset = 0f;
			if( CrowdIsUniform.val == false ) {
				rndHOffset = UnityEngine.Random.Range(-CrowdRandOffset.val, CrowdRandOffset.val);
				rndFOffset = UnityEngine.Random.Range(-CrowdRandOffset.val, CrowdRandOffset.val);
			}
			
			return new Vector3(hOffset + rndHOffset,0,fOffset + rndFOffset);
		}
		
		private Quaternion GetNewCharacterRotation() {
			float rndRotY = 0f;
			if( CrowdIsUniform.val == false ) {
				rndRotY = UnityEngine.Random.Range(-CrowdRandRotation.val, CrowdRandRotation.val);
			}
			
			if( CrowdFlipRotation.val == true ) {
				rndRotY += 180f;
			}
			
			return Quaternion.Euler(0f, rndRotY, 0f);
		}
		
		private int GetCharacterAnim( int spawnedCharacters ) {
			// Fixing the anim seed
			int animId = 0;
			
			// If the anim is forced
			if( AnimationToUse.val != "All" ) return animId = AnimationsList.IndexOf( AnimationToUse.val) - 1;
			
			int originalSeed =  UnityEngine.Random.seed;
			if( AnimSeed.val > 0 ) {
				UnityEngine.Random.seed = (int)AnimSeed.val + spawnedCharacters;
			} else {
				// I need to "randomize it" again because the main loop enforce the seed
				UnityEngine.Random.seed = Time.frameCount + spawnedCharacters;
			}
			
			int randStart = 4;
			if(  UnityEngine.Random.Range(0f, 1f) < CrowdIdleChance.val ) randStart = 0;
			animId = UnityEngine.Random.Range(randStart, 16);
			
			// Restoring the main seed
			UnityEngine.Random.seed = originalSeed;
			
			return animId;
		}
		
		private void SetAnimatorParams( Animator currentAnimator, int spawnedCharacters ) {
			currentAnimator.SetInteger("AnimType", GetCharacterAnim(spawnedCharacters) );
			currentAnimator.SetFloat("SpeedMultiplier", AnimSpeed.val );
			
			if( AnimationIsUniform.val == false ) {
				currentAnimator.SetFloat("OffsetState", GetCharacterAnimOffsetVal() );
			}
			
			if( AnimationMirroring.val == "Random" ) {
				if( UnityEngine.Random.Range(0f, 1f) > 0.75f ) { currentAnimator.SetBool("MirrorAnim", true); }
			} else if ( AnimationMirroring.val == "Mirrored" ) {
				currentAnimator.SetBool("MirrorAnim", true);
			}
		}
		
		private void SetCharacterMaterial( Transform character ) {
			if( CharactersMaterial.val == "Default" ) return;
			Material newMaterial = MaterialPool[ CharactersMaterialChoices.IndexOf( CharactersMaterial.val ) - 1 ];
			foreach ( SkinnedMeshRenderer smr in character.gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer)) ) {
				Material[] currentMats = smr.materials;
				for ( var i = 0; i < currentMats.Length; i++ ) {
					currentMats[i] = newMaterial;
				}
				smr.materials = currentMats;
			}
		}
		
		private float GetCharacterAnimOffsetVal() {
			return UnityEngine.Random.Range(0f, 1f);
		}
		
		protected void ToggleEditTilesParametersCallback(bool enabled) {
			foreach( UIDynamic uidElem in charactersParamsUIElements ) {
				if( enabled == true ) {
					uidElem.GetComponent<LayoutElement>().transform.localScale = new Vector3(1,1,1);
					uidElem.GetComponent<LayoutElement>().ignoreLayout = false;
				} else {
					uidElem.GetComponent<LayoutElement>().transform.localScale = new Vector3(0,0,0);
					uidElem.GetComponent<LayoutElement>().ignoreLayout = true;
				}
			}
		}
		
		protected void ToggleAnimateTilesParametersCallback(bool enabled) {
			foreach( UIDynamic uidElem in crowdParamsUIElements ) {
				if( enabled == true ) {
					uidElem.GetComponent<LayoutElement>().transform.localScale = new Vector3(1,1,1);
					uidElem.GetComponent<LayoutElement>().ignoreLayout = false;
				} else {
					uidElem.GetComponent<LayoutElement>().transform.localScale = new Vector3(0,0,0);
					uidElem.GetComponent<LayoutElement>().ignoreLayout = true;
				}
			}
		}


		
		// **************************
		// COROUTINES
		// **************************
		
		// Routine checking every 5secs if our CUA is still valid, this will invalidate the UI if the user change the CUA in the asset tab
		private IEnumerator CheckCUAStatus()
		{
			while (cuaIsValid)
			{
				// Our object root is null, it means our CUA is invalid
				if (CrowdCharasRoot == null)
				{
					// We invalidate it so that the coroutine stops by itself, remove the UI and show the UI to alert the user
					cuaIsValid = false;
					ClearCUAUI();
					CreateCUAInvalidatedUI();
				}
				yield return new WaitForSeconds(5.0f);
			}
		}
		
		// **************************
		// Local Tools
		// **************************
		private void logDebug( string debugText ) {
			SuperController.LogMessage( debugText );
		}
		
		private void logError( string debugText ) {
			SuperController.LogError( debugText );
		}
		
		private static void disableScrollOnText(UIDynamicTextField target) {
			ScrollRect targetSR = target.UItext.transform.parent.transform.parent.transform.parent.GetComponent<ScrollRect>();
			if( targetSR != null ) {
				targetSR.horizontal = false;
				targetSR.vertical = false;
			}
		}
		
		private UIDynamicTextField createStaticDescriptionText( string DescTitle, string DescText, bool rightSide, int fieldHeight ) {
			JSONStorableString staticDescString = new JSONStorableString(DescTitle,DescText);
			UIDynamicTextField staticDescStringField = CreateTextField(staticDescString, rightSide);
			staticDescStringField.backgroundColor = new Color(1f, 1f, 1f, 0f);
			LayoutElement sdsfLayout = staticDescStringField.GetComponent<LayoutElement>();
			sdsfLayout.preferredHeight = sdsfLayout.minHeight = fieldHeight;
			staticDescStringField.height = fieldHeight;
			disableScrollOnText(staticDescStringField);

			return staticDescStringField;
		}
				
		private Transform getChildRoot( Transform parent, string searchName ) {
			foreach( Transform child in parent )
			{
				if( child.name == searchName ) {
					return child;
				} else {
					Transform childSearch = getChildRoot(child,searchName);
					if (childSearch != null) return childSearch;
				}
			}
			
			return null;
		}
		
		// **************************
		// Time to cleanup !
		// **************************
		void OnDestroy() {
			// Cleaning modified native VAM assets

			// Cleaning the parent which contains all characters
			if(CrowdRoot != null && CrowdRoot.childCount > 0) {
				foreach (Transform child in CrowdRoot)
				{
					Destroy(child.gameObject);
				}
			}
		}

	}
}