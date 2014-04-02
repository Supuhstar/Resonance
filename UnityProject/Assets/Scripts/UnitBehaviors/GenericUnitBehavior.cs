﻿using UnityEngine;
using System.Collections;

public class GenericUnitBehavior : MonoBehaviour
{

    #region //Enums used for state machines and easy access in inspector.
    public enum State
    {
        Dummy,
        ChaseTarget,
        FollowPlayer,
        GatherNearestResourcePoint,
        ReturnToBase,
        Stay,
        DropoffAtFreighter,
        Attack,
        
        //Other states go here.
    }

    public enum ShipType
    {
        Interceptor,
        Freighter,
        Alpha,
        Resonator,
        Node,
    }
    #endregion

    #region //Property Instantiation Section
    private const float DAMPING = 7.0f;
    public State state = State.FollowPlayer;
    public State prevState = State.FollowPlayer;
    public ShipType shipType;

    public string AllyTag { get; set; }

    protected float gatheringRate { get; set; }

    protected float attackRate { get; set; }

    protected float rateModifier { get; set; }

    protected float movementSpeed { get; set; }

    protected float resourceCapacity { get; set; }

    protected float resourceLoad { get; set; }

    protected float integrity { get; set; } //This is kinda like a healthbar. This shouldn't be very important in our prototype's mode of play.;

    protected float interestRate { get; set; }// This one is taken from the GameController and updated with each update. - Moore

    protected Vector3 startPosition;
    protected const float distanceTreshold = 20.0f;
    private bool isPaused;
	public bool isSelected;
    public GameObject target;
    public GameObject altTarget;
    public GameObject playingArea;
    public GameObject theGameController;
    public GameObject myStatusIndicator;
    protected GameController theGameControllerScript;
    #endregion


    #region //Event Methods - Start
    void OnEnable()
    {
        GameController.OnPause += HandleOnPause;
        UpdateStatusIndicator();
    }

    void OnDisable()
    {
        GameController.OnPause -= HandleOnPause;
		isSelected = false;
        CommandHandler.OnCommand -= HandleCommandEvent;
		UnitSelection.OnDeselect -= HandleDeselectEvent;
    }

    // Use this for initialization
    void Start()
    {
        //BroadcastMessage("getMessage");
        //if (shipType == null)
        //{
        //    shipType = ShipType.Node;
        //} //Defaults to Node only if a type is not selected.
        if (shipType == ShipType.Alpha)
        {
            theGameControllerScript = theGameController.GetComponent<GameController>();
        }

        // NOTE FROM JARED:
        // I commented out the line above and took this out of the 'if' statement.
        // I need access to this script so I can detect if the game is paused.
        // (Refer to the OnGUI method.)
        //theGameController = GameObject.Find("Game Controller Prop");
        //theGameControllerScript = theGameController.GetComponent<GameController>();

        playingArea = GameObject.FindGameObjectWithTag("PlayingArea");

        startPosition = transform.position;

        movementSpeed = 10.0f;
        resourceCapacity = 600.0f;
        ResourceLoad = 500.0f;
        gatheringRate = 2.0f;
        rateModifier = 1.0f;

        interestRate = 0.005f; //Reminder: This will be overwritten each update by the value in the Game Controller. - Moore

        isPaused = false;

        //TODO: Use a switch case here to vary up the starting stats based on whatever shipType this unit is.
        switch (shipType)
        {
            case ShipType.Alpha:
                {
                    gatheringRate = 100.0f;
                    attackRate = 0.0f;
                    movementSpeed = 15.0f;
                    resourceCapacity = -1.0f;
                    ResourceLoad = 10.0f;
                    integrity = 100.0f;
                    break;
                }
            case ShipType.Freighter:
                {
                    gatheringRate = 10.0f;
                    attackRate = 0.0f;
                    movementSpeed = 5.0f;
                    resourceCapacity = 1000.0f;
                    ResourceLoad = 500.0f; 
                    integrity = 200.0f;
                    break;
                }
            case ShipType.Interceptor:
                {
                    gatheringRate = 1.0f;
                    attackRate = 0.1f;
                    movementSpeed = 25.0f;
                    resourceCapacity = 250.0f;
                    ResourceLoad = 50.0f;
                    integrity = 100.0f;
                    break;
                }

            default:
                break;
        }
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        switch (state)
        {
            case State.Dummy:
                break;
            
            case State.Stay:
                //Distinct from dummy. If this unit has distinct behaviors like attacking, it should still perform those duties... just not in this script. - Moore
                ApplyBrakes();
                break;

            case State.ChaseTarget:
            //Pick the cloest target with the appropriate tag and move towards it. - Moore
                target = FindClosestGameObjectWithTag("Target");
                FlyTowardsGameObject(target);


            //If too far from the start position, teleport back to start.
                if (Vector3.Distance(startPosition, transform.position) > 100)
                { 
                    transform.position = startPosition;
                }

                break;

            case State.FollowPlayer:
                target = FindClosestGameObjectWithTag("Player");
                FlyTowardsGameObjectWithSmartBraking(target);
                break;

            case State.GatherNearestResourcePoint:
            //Search for the nearest resource point. - Moore
                target = FindClosestGameObjectWithTag("ResourcePoint");

            //If close enough, draw resources from it. If not, then fly closer. - Moore
                if (IsWithinDistanceThreshold(target))
                {
                    ApplyBrakes();
                    GatherResourcesFromSource(target);
                } else
                {
                    FlyTowardsGameObjectWithSmartBraking(target);
                }

            //If my resources are maxed out, return to the alpha ship (player) - Moore.
                if (ResourceLoad >= resourceCapacity)
                {
                    if (shipType == ShipType.Freighter)
                    {
                        state = State.ReturnToBase;
                        //UpdateStatusIndicator(); DELETEME
                    } else
                    {
                        state = State.DropoffAtFreighter;
                        //UpdateStatusIndicator(); DELETEME
                    }
                }

                break;

            case State.ReturnToBase:
                target = FindClosestGameObjectWithTag("Player"); //This had been planned to be a separate ship, but now it refers to the player. - Moore
                if (IsWithinDistanceThreshold(target))
                {
                    ApplyBrakes();
                    TransferResourcesToSource(target); //If you're close enough to the player, drop off your resources until you're empty. - Moore
                } else
                {
                    FlyTowardsGameObjectWithSmartBraking(target); //If you're not close enough, get closer. - Moore.
                }
            
            //If no more resources to deposit, go back to the default behavior of following the player.
                if (ResourceLoad <= 0.0f)
                {
                    state = State.GatherNearestResourcePoint;
                    //UpdateStatusIndicator(); DELETEME
                }
            
                break;

            case State.DropoffAtFreighter:
            
            if (shipType == ShipType.Freighter)
            {state = State.ReturnToBase;}
            else
            {
            
                target = FindClosestGameObjectWithTag("Freighter");
                altTarget = FindClosestGameObjectWithTag("Player");

                if (target != null && altTarget != null)
                {
                    //Change the target to point at the same thing as the altTarget if the altTarget is closer.
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.transform.position.x, target.transform.position.z)) > Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(altTarget.transform.position.x, altTarget.transform.position.z)))
                    {
                        target = altTarget;
                    }

                    if (IsWithinDistanceThreshold(target))
                    {
                        ApplyBrakes();
                        TransferResourcesToSource(target); //If you're close enough to the player, drop off your resources until you're empty. - Moore
                    } else
                    {
                        FlyTowardsGameObjectWithSmartBraking(target); //If you're not close enough, get closer. - Moore.
                    }
            
                    //If no more resources to deposit, go back to the default behavior of following the player.
                    if (ResourceLoad <= 0.0f)
                    {
                        state = State.GatherNearestResourcePoint;
                        //UpdateStatusIndicator(); DELETEME
                                
                    }
                }
            }
                break;



            default:
                {
                    break;
                }
        }

        //Any time the state changes, update the status indicator.
        if (prevState != state)
        {
            UpdateStatusIndicator();
            prevState = state;
        }

    }

    void Update()
    {
        //Done cycling through options based on the state. This is now based on the ship's type.
        switch (shipType)
        {
            case ShipType.Alpha:
                {
                    //Only the player gains interest and communicates with the GameController.
                    if (theGameControllerScript != null)
                    {
                        //Update the interest from the GameController.
                        interestRate = theGameControllerScript.GetInterestRate();
                        //Bank the interest from existing resources.
                        ResourceLoad += ResourceLoad * interestRate * Time.deltaTime;
                    }
                    break;
                }
            case ShipType.Node:
                {
                    //Only the player gains interest and communicates with the GameController.
                    if (ResourceLoad <= 0)
                    {
                        //Destroy(gameObject);
                        ResourceLoad += Random.Range(200, 2000);
                        transform.position = RandomVector3InRange(playingArea.renderer.bounds.min.x, playingArea.renderer.bounds.max.x, 0, 0, playingArea.renderer.bounds.min.z, playingArea.renderer.bounds.max.z);

                    }
                    break;
                }
        //Interceptors should occasionally attack other units.
        //Freighters don't have much in the way of special behavior, but may cause resource nodes to appear when attacked?
        //To make the AI smarter, maybe consider having them drop to the nearest freighter instead of the player if the freighter is closer. - Moore
        //Resonators should spend some of their Resource load to boost other allied units.
            default:
                break;
        }
    }

    //Credit to Jared Cerbin for coming up with this GUI stuff.
    void OnGUI()
    {
        // Display the GUI only if the game is not paused. Take this out of the 'if' to see why. - Jared
        if (!isPaused)
        {
            Vector2 labelPos = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(labelPos.x, Screen.height - labelPos.y - 30, 50, 20), ResourceLoad.ToString("F0")); //I did a slight tweak to not draw the decimial portion of the resource thinger. - Moore
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //If the object we're colliding with is a unitSelection bubble... - Moore
        UnitSelection usScript = other.transform.parent.GetComponent<UnitSelection>();
        if (usScript != null)
        {
            //Then we add ourselves to the selection and start following the player. But only if we're not a player.
            if (shipType != ShipType.Alpha /* && usScript.iCanHasSelected*/)
            {
                CommandHandler.OnCommand += HandleCommandEvent;
				UnitSelection.OnDeselect += HandleDeselectEvent;
				isSelected = true;
                state = State.FollowPlayer;
                UpdateStatusIndicator(); //DELETEME
            }
        }
    }

    #endregion //Event Methods - End

    #region // Accessor/Mutator Methods - Start

    public float ResourceLoad
    {
        get { return resourceLoad;} //Accessor
        set { resourceLoad = value;} //Mutator
    }

    public float MovementSpeed
    {
        get { return movementSpeed;} //Accessor
        set { movementSpeed = value;} //Mutator
    }

    public float ResourceCapacity
    {
        get { return resourceCapacity;} //Accessor
        set { resourceCapacity = value;} //Mutator
    }

    public float GatheringRate
    {
        get { return gatheringRate;} //Accessor
        set { gatheringRate = value;} //Mutator
    }

    public float RateModifier
    {
        get { return rateModifier;} //Accessor
        set { rateModifier = value;} //Mutator
    }

    #endregion //Accessor/Mutator Methods - End

    #region //Mutator/Logic Methods - Start



    public float GatherResourcesFromSource(GameObject targetObject)
    {
        //Precondition: You should really only call this method once per calling object per update. - Moore
        float result = 0.0f;
        GenericUnitBehavior gubScript = targetObject.GetComponent<GenericUnitBehavior>();
        if (gubScript != null)
        {
            result = gubScript.TransferResourcesToSource(gameObject);
        }
        return result;
    }

    public float TransferResourcesToSource(GameObject targetObject)
    {

        //Precondition: You should only call this once per calling object per update.

        /*
        result = 0.0f;
        GenericUnitBehavior gubScript = targetObject.GetComponent<GenericUnitBehavior>();
        if (gubScript != null)
        {
        result = gubScript.GatherResourcesFromSource(this);
        }
        return result;
        // This version is a redirection helper mathod to swap arguments from GatherResourcesFromSource. DELETEME - Moore.
         */


        //Check how many resources the target wants to take.
        float result = 0.0f;
        float amountRequested = 0.0f;
        GenericUnitBehavior gubScript = targetObject.GetComponent<GenericUnitBehavior>();

        amountRequested = gubScript.gatheringRate * gubScript.rateModifier * Time.fixedDeltaTime;

        //Can't draw more than the capacity. - Moore
        if (gubScript.resourceCapacity < 0.0f)
        {
            ;// This is here because a negative capacity effectively means infinite capacity. It prevents the following branch from bouncing back.
        } else if (amountRequested + gubScript.ResourceLoad > gubScript.resourceCapacity)
        {
            amountRequested = gubScript.resourceCapacity - gubScript.ResourceLoad;
        }

        //If this request can be fulfilled in full, do so. 
        if (amountRequested <= ResourceLoad)
        {
            ResourceLoad -= amountRequested;
            gubScript.ResourceLoad += amountRequested;
        } else
        { //If not, give all that remains. Leave cleanup for another method to deal with. - Moore.
            amountRequested = ResourceLoad;
            ResourceLoad -= amountRequested;
            gubScript.ResourceLoad += amountRequested;
        }

        result = amountRequested;
        return result;


    }

    protected void UpdateStatusIndicator()
    {
        //TODO: Consider having events for the different state changes and then letting other objects listen out for those events intsead - Moore
        {
            switch (state)
            {
                case State.Attack:
                    BroadcastMessage("SetMaterialRed", true, SendMessageOptions.DontRequireReceiver);
                    break;
                
                case State.GatherNearestResourcePoint:
                    BroadcastMessage("SetMaterialPurple", true, SendMessageOptions.DontRequireReceiver);
                    break;
                
                case State.FollowPlayer:
					if(isSelected)
					{
						BroadcastMessage("SetMaterialBlue", true, SendMessageOptions.DontRequireReceiver);
					}
					else
					{
                    	BroadcastMessage("SetMaterialWhite", true, SendMessageOptions.DontRequireReceiver);
					}
                    break;
                
                case State.ReturnToBase:
                    BroadcastMessage("SetMaterialYellow", true, SendMessageOptions.DontRequireReceiver); //The notes suggest this should be purple, but yellow is a bit easier to see the change.
                    break;
                
                case State.DropoffAtFreighter:
                    BroadcastMessage("SetMaterialYellow", true, SendMessageOptions.DontRequireReceiver); //The notes suggest this should be purple, but yellow is a bit easier to see the change.
                    break;

            /*
                case State.ReturnToBase:
                    BroadcastMessage("SetMaterialYellow", true, SendMessageOptions.DontRequireReceiver); //The notes suggest this should be purple, but yellow is a bit easier to see the change.
                    break;
                    */
                
                default: 
                    BroadcastMessage("SetMaterialNone", true, SendMessageOptions.DontRequireReceiver);
                    break;
            }
        }
    }

    protected void HandleCommandEvent(Command theCommand) //Technically, units shouldn't really 'know' these commands, but this solution is faster (but bad in the long run) than using the OOD Adapter Pattern. Maybe worth changing later for decoupling.  - Moore
    {
        switch (theCommand)
        {
            case Command.NULL:
                CommandHandler.OnCommand -= HandleCommandEvent;
				isSelected = false;
                break;
            
            case Command.ATTACK:
                state = State.Attack;
                break;
            
            case Command.COLLECT:
                state = State.GatherNearestResourcePoint;
                break;
            
            case Command.STAY:
                state = State.Stay;
                break;
            
            case Command.UNLOAD:
            if (shipType == ShipType.Freighter) {state = State.ReturnToBase;} //WORKAROUND: We shouldn't be checking for shiptypes here at all. Right now, the state is set over and over until the 'selection' is ended.
            else {state = State.DropoffAtFreighter;} // - This is different from Dropoff at Freighter (Which is done while out collecting) as it goes ONLY to the player. The player will collect much faster and is already in selection range. - Moore
                break;
            
            default:
                break;
        }

        //Regardless of what your command was, this unit isn't listening anymore.
        //CommandHandler.OnCommand -= HandleCommandEvent;
		isSelected = false;
    }

	// This message is broadcast whenever the player starts the selection process by pressing the Select Units button
	protected void HandleDeselectEvent()
	{
		isSelected = false;
		UpdateStatusIndicator();
		UnitSelection.OnDeselect -= HandleDeselectEvent;
	}
        

    #endregion //Mutator/Logic Methods - End

    #region //Utility Methods - Start

    protected void FlyTowardsGameObject(GameObject destination)
    {
        //Precondition: You should only be calling this once per caller per update. - Moore
        if (destination != null)
        {
            //Make sure to check that the target is within a desirable distance. So there should be a maximum distance variable. - Moore
            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
            //Quaternion rotation = Quaternion.LookRotation((new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position));
            //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * DAMPING);

            if (IsNotWithinDistanceThreshold(destination))
            { //4:23 Defect - Had the < intstead of >.
                if (rigidbody == null)
                {
                    transform.position += (transform.forward * movementSpeed * rateModifier * Time.fixedDeltaTime);
                } else
                {               //Changing this ^^ to use rigidbodies and forces instead to allow for less weird overlapping stuff. - Moore.            
                    rigidbody.freezeRotation = true;
                    rigidbody.AddForce(-rigidbody.velocity);
                    rigidbody.AddForce(transform.forward * movementSpeed * rateModifier * Time.fixedDeltaTime); //NOTE: We should only be using *this* version in a fixed update. - Moore.
                    //rigidbody.velocity = transform.forward * movementSpeed * rateModifier * Time.deltaTime;         
                }       
            }
        }
    }

    protected void FlyTowardsGameObjectWithSmartBraking(GameObject destination)
    {
        //Description: This is very similar to the original FlyTowardsGameObject script, but is intended only for use with GameObjects that have rigid bodies. If it detects that its destination is within breaking distance it will start to slow down. - Moore.
        //Precondition: You should only be calling this once per caller per update. - Moore
        if (destination != null)
        {
            //Make sure to check that the target is within a desirable distance. So there should be a maximum distance variable. - Moore
            //transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
            Quaternion rotation = Quaternion.LookRotation((new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z) - transform.position));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * DAMPING);


            if (IsNotWithinBrakingDistance(destination))
            { //4:23 Defect - Had the < intstead of >.
                if (rigidbody != null)
                {               

                    rigidbody.freezeRotation = true;
                    rigidbody.AddForce(-rigidbody.velocity);
                    rigidbody.AddForce(transform.forward * movementSpeed * rateModifier /** Time.fixedDeltaTime*/); //NOTE: We should only be using *this* version in a fixed update. - Moore.
                } else
                {
                    print("The method \"FlyTowardsGameObjectWithSmartBraking\" was called on an object with no rigidbody. Did you mean to use \"FlyTowardsGameObject\"?");
                }
            } else
            {
                ApplyBrakes();
            }
        }
    }

    protected bool IsWithinDistanceThreshold(GameObject destination) //TODO: Change this to factor in rigidbody.velocity so that it slows down before reaching destination. Alternatively, make a new method to do exactly that. - Moore
    {
        bool result = false;

        if (Vector3.Distance(transform.position, destination.transform.position) > distanceTreshold)
        { //If it is outside of the threshold...
            result = false; //Return false. - Moore
        } else
        { // Otherwise, return true. - Moore
            return true;
        }

        return result;
    }

    //Convenience Negation Wrapper Method for the above:
    protected bool IsNotWithinDistanceThreshold(GameObject destination)
    {
        return !IsWithinDistanceThreshold(destination);
    }

    protected bool IsWithinBrakingDistance(GameObject destination) //TODO: Change this to factor in rigidbody.velocity so that it slows down before reaching destination. Alternatively, make a new method to do exactly that. - Moore
    {
        bool result = false;
        
        if (Vector3.Distance(transform.position, destination.transform.position) > (distanceTreshold + (rigidbody.velocity.magnitude /* * 2*/)) && rigidbody.velocity.magnitude > 0)
        { //If it is outside of the threshold...
            result = false; //Return false. - Moore
        } else
        { // Otherwise, return true. - Moore
            return true;
        }
        
        return result;
    }
    
    //Convenience Negation Wrapper Method for the above:
    protected bool IsNotWithinBrakingDistance(GameObject destination)
    {
        return !IsWithinDistanceThreshold(destination);
    }

    protected void ApplyBrakes()
    {
        if (rigidbody != null)
        {
            rigidbody.AddForce(-0.6f * rigidbody.velocity);
            if (rigidbody.velocity.magnitude < 0.1f)
            {
                rigidbody.velocity = Vector3.zero;
            }
        }
    }

    protected GameObject FindClosestGameObjectWithTag(string tagToFind)
    {
        GameObject result = null;
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag(tagToFind);
        
        foreach (GameObject current in allObjects)
        {
            if (current != this.gameObject)
            {
                if (result == null)
                {
                    result = current;
                } else
                {
                    if (Vector3.Distance(transform.position, result.transform.position) > Vector3.Distance(transform.position, current.transform.position))
                    {
                        result = current;
                    }
                }
            }
        }
        return result;
    }

    protected Vector3 RandomVector3InRange(float xLow, float xHigh, float yLow, float yHigh, float zLow, float zHigh)
    {
        float tempX = Random.Range(xLow, xHigh);
        float tempY = Random.Range(yLow, yHigh);
        float tempZ = Random.Range(zLow, zHigh);

        return new Vector3(tempX, tempY, tempZ);
    }

    // This is the class that handles the OnPause event, which is broadcast by GameController whenever the RunState changes.
    void HandleOnPause(bool flag)
    {
        isPaused = flag;
    }
    #endregion //Utility Methods - end
}