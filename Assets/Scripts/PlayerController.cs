﻿//Main control script for players. Handles scene starting and shooting
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerController : NetworkBehaviour {

	[HideInInspector]
    public string playerID;
    private GameManager _gameManager;
	private Jido_Manager _jidoManager;

    //[SyncVar(hook = "OnChangeHealth")]
    public int Health;
    private Image _localHealthBar;
    public const int MaxHealth = 100;
    public int CurrentHealth = MaxHealth;

	[HideInInspector]
    public Image WorldSpaceHealthBar;

	//Model used for this Player
	public GameObject PlayerModel;
	public ModelController ModelController;

    [Header("Projectile Info")]
    public GameObject LocalProjectilePrefab;
    public GameObject ClientProjectilePrefab;

    private float _maxSpeed = 6;

	[HideInInspector]
	public bool gameStarted;

	//Variables to track how long user has been touching for a shoot
	private float maxCount = 10;
    private float count = 1;
    private float ShootChargeSpeed = 10.5f;

    private Transform _cameraTransform;

    private void Start()
    {
        _cameraTransform = Camera.main.transform;
        _gameManager = FindObjectOfType<GameManager>();
		_jidoManager = FindObjectOfType<Jido_Manager> ();

		Health = MaxHealth;

		//Set the name of this player game object using netId
		playerID = GetComponent<NetworkIdentity>().netId.ToString();
		name = playerID;

        if (!isLocalPlayer)
        {
			_jidoManager.AddNonLocalPlayer(gameObject);
            ModelController.ShieldVisuals.tag = "Untagged";//This is only on the non local player so it doesn't allow lasers to pass through it
        }
        else
        {
            _localHealthBar = _gameManager.LocalPlayerHealthBar;

			_gameManager.ShieldButton.onClick.AddListener(ShieldButtonPressed);
			_jidoManager.AddLocalPlayer(gameObject);
        }
    }

    void Update () {

		//Only detect shoot if local player
		if (!isLocalPlayer) {
			return;
		}

		//Charges shot on touch holding, shoots on touch up
        if (Input.touchCount > 0 && gameStarted)  
		{
            if (EventSystem.current.currentSelectedGameObject == _gameManager.ShieldButton.gameObject)
                return;

			if ((Input.GetTouch (0).phase == TouchPhase.Stationary) || (Input.GetTouch (0).phase == TouchPhase.Moved)) {
                if (count < maxCount)
                {
                    count = count + (Time.deltaTime * ShootChargeSpeed);
                    if (count > 1.5f)
                    {
                        _gameManager.ShootChargeRing.fillAmount = (count)/10;
                    }
                }
			} else if (Input.GetTouch (0).phase == TouchPhase.Ended) {
                _gameManager.ShootChargeRing.fillAmount = 0;
				float speedFraction = (float)count / maxCount;
                Fire (speedFraction);
				CmdFire (speedFraction);
				count = 1;
			}
        }

		//TODO: Remove this. Just for Testing in editor.
		if(Input.GetKeyDown(KeyCode.Space)){
			float speedFraction = 0.5f;
			Fire (speedFraction);
			CmdFire (speedFraction);
			count = 1;
		}
	}
		
	public void SetGameStarted(){
		if (!gameStarted) {
			gameStarted = true;
            FindObjectOfType<GameManager>().StartGame();
		}
	}

	public void SetGameStarted(GameObject playerModel){
		if (!gameStarted) 
        {
			PlayerModel = playerModel;
			ModelController = PlayerModel.GetComponent<ModelController> ();
            WorldSpaceHealthBar = ModelController.WorldSpaceHealthBar;
			gameStarted = true;
			FindObjectOfType<GameManager>().StartGame();
		}
	}

	//This fire works on local and remote players
    private void Fire(float speedFraction)
    {
        GameObject laser;
        if(isLocalPlayer){
            // Create the Bullet from the Bullet Prefab
            laser = (GameObject)Instantiate(
                LocalProjectilePrefab,
                ModelController.ProjectileSpawnPoint.position,
                ModelController.ProjectileSpawnPoint.rotation);
        }else{
            // Create the Bullet from the Bullet Prefab
            laser = (GameObject)Instantiate(
                ClientProjectilePrefab,
                ModelController.ProjectileSpawnPoint.position,
                ModelController.ProjectileSpawnPoint.rotation); 
        }
        // Add velocity to the bullet scaled by how long user touched down
        laser.GetComponent<Rigidbody>().velocity = laser.transform.forward * _maxSpeed * speedFraction;

        // Destroy the bullet after 4 seconds
        Destroy(laser, 4.0f);
    }

	//Send Fire to Host
	[Command]
	void CmdFire(float speedFraction){
		RpcRemoteFire (speedFraction);
	}

	//Send Fire to Client players
	[ClientRpc]
	void RpcRemoteFire(float speedFraction){
		//For some reason this sometimes get called on the local player
		if (isLocalPlayer) {
			//print ("Local RPC");
			return;
		}
			
		if (!gameStarted) {
			return;
		}
         
        //Pass Fire to the remote player's local avatar
        Fire(speedFraction);
	}

	//Works on local player only. Checks if was hit by projectile.
	void OnTriggerEnter(Collider collider)
	{
		if (!isLocalPlayer)
		{
			return;
		}

		CmdHit();
	}

	//Register a hit with Host
    [Command]
    void CmdHit()
    {
        RpcChangeHealth();
    }

	//Send hit to clients
    [ClientRpc]
    void RpcChangeHealth()
    {
        CurrentHealth -= 10;

        if(_localHealthBar)
            _localHealthBar.fillAmount = CurrentHealth * 0.01f;

        if(WorldSpaceHealthBar)
            WorldSpaceHealthBar.fillAmount = CurrentHealth * 0.01f;

        if (CurrentHealth <= 0 && isLocalPlayer)
        {
            //CurrentHealth = MaxHealth;
            FindObjectOfType<GameManager>().ShowToast("You Died!", 1.0f); 
            _localHealthBar.fillAmount = 0;
        }
    }

	private void ShieldButtonPressed(){
		ToggleShield ();
		CmdActivateShield();
	}

	//This functions on Local and Remote Players
	private void ToggleShield (){
        ModelController.ShieldVisuals.SetActive(!ModelController.ShieldVisuals.activeSelf);
	}

	private void DeactivateShield (){
		ModelController.ShieldVisuals.SetActive(false);
	}

	//Register shield with host
    [Command]
    void CmdActivateShield()
    {
        RpcActivateShield();
    }

    //Send shield to Client players
    [ClientRpc]
    void RpcActivateShield()
    {
        //For some reason this sometimes get called on the local player
        if (isLocalPlayer)
            return;
        
		ToggleShield();
    }

    [Command]
    public void CmdPlaceDetectedObject(Vector3 position)
    {
        RpcPlaceDetectedObject(position);
    }

    [ClientRpc]
    public void RpcPlaceDetectedObject(Vector3 position)
    {
        if (isLocalPlayer)
        {
            return;
        }
			
        //Instantiate(defensePrefab, transformControl.GetLocalPosition(position), Quaternion.identity);
    }
}
