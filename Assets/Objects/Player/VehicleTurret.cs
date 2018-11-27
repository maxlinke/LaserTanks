﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleTurret : MonoBehaviour {

	[SerializeField] Rigidbody rb;
	[SerializeField] HingeJoint joint;
	[SerializeField] Transform customCenterOfMass;
	[SerializeField] Transform muzzle;

	[SerializeField] float maxTurnSpeed;
	[SerializeField] float maxTurnAcceleration;

	VehicleBody body;
	float maxTurnForce;

	void Start () {
		ApplyCustomCenterOfMass(customCenterOfMass, rb);
		maxTurnForce = rb.mass * maxTurnAcceleration;
	}

	void Reset () {
		rb = GetComponent<Rigidbody>();
		joint = GetComponent<HingeJoint>();
		for(int i=0; i<rb.transform.childCount; i++){
			Transform child = rb.transform.GetChild(i);
			string lowercase = child.name.ToLower();
			if(lowercase.Contains("centerofmass") || lowercase.Contains("center of mass")){
				customCenterOfMass = child;
			}
			if(lowercase.Contains("muzzle")){
				muzzle = child;
			}
		}
	}

	public void Initialize (VehicleBody body) {
		this.body = body;
	}
	
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space)){
			Debug.Log("bang");	//TODO shoot (ABSTRACTION!!!!)
		}
		float rayLength = 5f;
		Optics.LightcastHit lightcastHit;
		Vector3[] points;
		Optics.Lightcast(muzzle.position, muzzle.forward, out lightcastHit, out points, rayLength);
		for(int i=1; i<points.Length; i++){
			Debug.DrawLine(points[i-1], points[i], Color.magenta, 0f, true);
		}
	}

	void FixedUpdate () {
//		DirectInputTurretTurning();
		MousePointerTurretTurning();
	}

	void ApplyCustomCenterOfMass (Transform newCenterOfMass, Rigidbody rb) {
		if(newCenterOfMass != null){
			rb.centerOfMass = rb.transform.InverseTransformPoint(newCenterOfMass.position);
		}
	}

	void DirectInputTurretTurning () {
		float turnInput = GetDirectTurnInput();
		JointMotor motor = new JointMotor();
		motor.targetVelocity = turnInput * maxTurnSpeed;
		motor.force = maxTurnForce;
		motor.freeSpin = false;
		joint.motor = motor;
	}

	float GetDirectTurnInput () {
		float output = 0f;
		if(Input.GetKey(KeyCode.LeftArrow)) output -= 1f;
		if(Input.GetKey(KeyCode.RightArrow)) output += 1f;
		return output;
	}

	void MousePointerTurretTurning () {
		Vector3 toMouse = GetVectorToMouse();
		float deltaAngle = Vector3.Angle(toMouse, rb.transform.forward) * Mathf.Sign(Vector3.Dot(toMouse, rb.transform.right));
		JointMotor motor = new JointMotor();
		motor.targetVelocity = deltaAngle / Time.fixedDeltaTime;
		motor.force = maxTurnForce;
		motor.freeSpin = false;
		joint.motor = motor;
	}

	//TODO parameters and stuff
	Vector3 GetVectorToMouse () {
		Camera cam = Camera.main;		//TODO actually reference the proper camera...
		//so when networked move the main camera here OR instantiate / enable a new camera
		Vector3 screenMousePos = Input.mousePosition;
		Vector3 worldMousePos = cam.ScreenToWorldPoint(new Vector3(screenMousePos.x, screenMousePos.y, 1f));		//apparently any value for z will do...
		Vector3 mouseRay = worldMousePos - cam.transform.position;
		Vector3 planeOrigin = rb.worldCenterOfMass;
		Vector3 planeNormal = rb.transform.up;
		float lambda = Vector3.Dot(planeNormal, (planeOrigin - cam.transform.position)) / Vector3.Dot(planeNormal, mouseRay);
		Vector3 mouseOnPlane = cam.transform.position + (lambda * mouseRay);
		Vector3 toMouse = mouseOnPlane - rb.worldCenterOfMass;
		return toMouse;
	}

}
