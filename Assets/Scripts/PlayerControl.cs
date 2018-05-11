﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerControl : NetworkBehaviour {

	public GameObject localShip;

	public GameObject thisOrigin;
	public bool gameStarted;

	void Update () {

		if (!isLocalPlayer) {
			return;
		}

		if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetKeyDown(KeyCode.Space))
		{
			LocalFire();
			CmdFire();
		}
	}

	public void SetGameStarted(){
		if (!gameStarted) {
			gameStarted = true;
			GameObject.Find ("GUI").GetComponent<SceneControl>().StartGame ();
		}
	}

	public void SetGameStarted(GameObject origin){
		if (!gameStarted) {
			thisOrigin = origin;
			GetComponent<Health> ().healthBar = thisOrigin.GetComponent<AvatarControl> ().thisAvatar.GetComponent<ShipControl> ().healthBar;
			gameStarted = true;
			GameObject.Find ("GUI").GetComponent<SceneControl>().StartGame ();
		}
	}

	private void LocalFire(){
		if (gameStarted) {
			localShip.GetComponent<ShipControl> ().Fire ();
		}
	}

	[Command]
	void CmdFire(){
		RpcRemoteFire ();
	}

	[ClientRpc]
	void RpcRemoteFire(){
		if (isLocalPlayer) {
			print ("Local RPC");
			return;
		}

		print ("RPC " + GetComponent<NetworkIdentity>().netId.ToString());
		if (!gameStarted) {
			print ("Not Started");
			return;
		}
		thisOrigin.GetComponent<AvatarControl>().Fire ();
	}
		
}