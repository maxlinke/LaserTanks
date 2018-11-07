using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleTurret : MonoBehaviour {

	[SerializeField] Rigidbody rb;
	[SerializeField] HingeJoint joint;
	[SerializeField] Transform customCenterOfMass;
	[SerializeField] protected Transform muzzle;

	[SerializeField] float maxTurnSpeed;
	[SerializeField] float maxTurnAcceleration;

	float maxTurnForce;

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

	void Start () {
		ApplyCustomCenterOfMass(customCenterOfMass, rb);
		maxTurnForce = rb.mass * maxTurnAcceleration;
	}
	
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space)){
			Debug.Log("bang");	//TODO shoot (ABSTRACTION!!!!)
		}
		Debug.DrawRay(muzzle.transform.position, muzzle.transform.forward * 5f, Color.magenta, 0f, true);
	}

	void FixedUpdate () {
		float turnInput = GetTurnInput();
		JointMotor motor = new JointMotor();
		motor.targetVelocity = turnInput * maxTurnSpeed;
		motor.force = maxTurnForce;
		motor.freeSpin = false;
		joint.motor = motor;
	}

	void ApplyCustomCenterOfMass (Transform newCenterOfMass, Rigidbody rb) {
		if(newCenterOfMass != null){
			rb.centerOfMass = rb.transform.InverseTransformPoint(newCenterOfMass.position);
		}
	}

	float GetTurnInput () {
		float output = 0f;
		if(Input.GetKey(KeyCode.LeftArrow)) output -= 1f;
		if(Input.GetKey(KeyCode.RightArrow)) output += 1f;
		return output;
	}
}
