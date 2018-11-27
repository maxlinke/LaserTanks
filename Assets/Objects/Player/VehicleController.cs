using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour {

	[SerializeField] VehicleBody body;
	[SerializeField] VehicleTurret turret;

	void Start () {
		body.Initialize(turret);
		turret.Initialize(body);
	}

	void Reset () {
		body = GetComponentInChildren<VehicleBody>();
		turret = GetComponentInChildren<VehicleTurret>();
	}

	void OnEnable () {
		body.gameObject.SetActive(true);
		turret.gameObject.SetActive(false);
		StartCoroutine(EnableNextFrame(turret.gameObject));
	}

	void OnDisable () {
		body.gameObject.SetActive(false);
		turret.gameObject.SetActive(false);
	}
	
	void Update () {
		
	}
	
	void FixedUpdate () {
		
	}

	IEnumerator EnableNextFrame (GameObject obj) {
		yield return null;
		obj.SetActive(true);
	}
	
}
