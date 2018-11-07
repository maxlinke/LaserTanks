using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestJointedRigidBody : MonoBehaviour {

	[SerializeField] float speed;
	[SerializeField] float acceleration;
	[SerializeField] Rigidbody rb1;
	[SerializeField] Rigidbody rb2;
	[SerializeField] HingeJoint joint;

	void Start () {
		
	}
	
	void Update () {
		
	}
	
	void FixedUpdate () {
		Vector3 ownVelocity = Vector3.ProjectOnPlane(rb1.velocity, rb1.transform.up);
		Vector3 desiredVelocity = rb1.transform.TransformDirection(WASDToVector()) * speed;
		Vector3 deltaV = desiredVelocity - ownVelocity;
		Vector3 deltaVAccel = deltaV / Time.fixedDeltaTime;
		Vector3 accel;
		if(deltaVAccel.magnitude > acceleration){
			accel = deltaV.normalized * acceleration;
		}else{
			accel = deltaVAccel;
		}
		rb1.velocity += accel * Time.fixedDeltaTime;

		JointMotor jointMotor = new JointMotor();
		jointMotor.targetVelocity = Input.GetAxisRaw("Mouse X") * 100f;
		jointMotor.force = 1000f;
		jointMotor.freeSpin = false;
		joint.motor = jointMotor;	//this is a weeeeeeiiiiiiird way of doing it but okay...
	}

	Vector3 WASDToVector () {
		Vector3 output = Vector3.zero;
		if(Input.GetKey(KeyCode.W)) output += Vector3.forward;
		if(Input.GetKey(KeyCode.S)) output += Vector3.back;
		if(Input.GetKey(KeyCode.A)) output += Vector3.left;
		if(Input.GetKey(KeyCode.D)) output += Vector3.right;
		if(output.magnitude > 1f) output = output.normalized;
		return output;
	}

}
