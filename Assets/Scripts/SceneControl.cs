﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class SceneControl : MonoBehaviour
{

	public GameObject hostClientPanel;
	public Text notification;
	public GameObject playerCanvas;
	public RectTransform myHealth;

	public MapSession mapSession;
	public String DEV_KEY;

	public GameObject thisOrigin;
	public GameObject otherOriginPrefab;
	public GameObject SUPlayerPrefab;
	public GameObject detectedObjectPrefab;

	private float detectThresh = 0.7f;

	public GameObject localPlayer;
	public Queue<string> lookFor = new Queue<string>();
	public Text lookForText;
    public GameObject lookForTextObj;


	//TODO: remove
	public void TestTap(){
		localPlayer.GetComponent<TransformControl>().TestTap ();
	}

	void Start(){
		Invoke ("InitMappingSession", 2.0f);
	}

	public void AddLocalPlayer(GameObject localPlayer){
		this.localPlayer = localPlayer;
	}

	public void AddNonLocalPlayer(GameObject playerID){
		lookFor.Enqueue (playerID.name);
		UpdateLookForDisplay ();
	}

	public void UpdateLookForDisplay ()
	{
		if (lookFor.Count < 1) {
			lookForText.text = "";
            lookForTextObj.SetActive(false);
		} else {
			lookForText.text = "Tap Player " + lookFor.Peek () + " When They Light Up!";
            lookForTextObj.SetActive(true);
		}
	}

	public void StartGame(){
		playerCanvas.SetActive (true);
	}

	private void InitMappingSession ()
	{

		//Mapsession initialization
		bool isMappingMode = true;
		string mapID = "Jido";
		string userID = "Multiplayer";

		mapSession.Init (isMappingMode ? MapMode.MapModeMapping : MapMode.MapModeLocalization, userID, mapID);

		mapSession.ObjectDetectedEvent += ObjectDetectedCallback;
		//Set callback to handly MapStatus updates
		mapSession.StatusChangedEvent += StatusChangedCallback;

		//Set callback that confirms when assets are stored
		mapSession.AssetStoredEvent += AssetStoredCallback;

		//Set Callback for when assets are reloaded
		mapSession.AssetLoadedEvent += AssetLoadedCallback;

	}

	public void ObjectDetectedCallback(DetectedObject detectedObject){
		if (detectedObject.Confidence > detectThresh) {

			if (detectedObject.Name == "person") {
				if (lookFor.Count > 0) {
					print ("look for: " + lookFor.Count);
					Vector3 pos = new Vector3 (detectedObject.X, detectedObject.Y, -detectedObject.Z);
					GameObject SUPlayer = Instantiate (SUPlayerPrefab, pos, Quaternion.identity);
					SUPlayer.transform.localScale = new Vector3 (detectedObject.Height / 3, detectedObject.Height, detectedObject.Height / 3);
					SUPlayer.GetComponent<DetectedObjectControl> ().isVisible = true;
				} else {
					Vector3 pos = new Vector3 (detectedObject.X, detectedObject.Y, -detectedObject.Z);
					GameObject SUPlayer = Instantiate (SUPlayerPrefab, pos, Quaternion.identity);
					SUPlayer.transform.localScale = new Vector3 (detectedObject.Height / 3, detectedObject.Height, detectedObject.Height / 3);
					SUPlayer.GetComponent<DetectedObjectControl> ().isVisible = false;
				}
			} else if (lookFor.Count < 1 && detectedObject.Name == "chair"){
								Vector3 pos = new Vector3 (detectedObject.X, detectedObject.Y, -detectedObject.Z);
								GameObject DO = Instantiate (detectedObjectPrefab, pos, Quaternion.identity);
								DO.transform.localScale = new Vector3 (detectedObject.Height / 2, detectedObject.Height, detectedObject.Height / 2);
			}
		}
	}

	public void StatusChangedCallback (MapStatus mapStatus)
	{
		Debug.Log ("status updated: " + mapStatus);
	}

	public void AssetStoredCallback (bool stored){}

	public void AssetLoadedCallback (MapAsset mapAsset){}

	public void Toast (String message, float time)
	{
		notification.text = message;
		notification.gameObject.SetActive (true);
		CancelInvoke ();
		Invoke ("ToastOff", time);
	}

	public void ToastOff ()
	{
		notification.gameObject.SetActive (false);
	}
}