using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleTurret : MonoBehaviour {

	[SerializeField] Rigidbody rb;
	[SerializeField] HingeJoint joint;
	[SerializeField] Transform customCenterOfMass;
	[SerializeField] Transform muzzle;

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
		float rayLength = 5f;
//		RaycastHit hit;
//		if(Physics.Raycast(muzzle.position, muzzle.forward, out hit, rayLength)){
//			Debug.DrawLine(muzzle.position, hit.point, Color.magenta, 0f, true);
//			RefractiveObject other = hit.collider.GetComponent<RefractiveObject>();
//			if(other != null){
//				float hitDist = (hit.point - muzzle.position).magnitude;
//				bool totalReflection;
//				Vector3 outputDir = Optics.Refract(muzzle.forward, hit.normal, 1f, other.RefractiveIndex, out totalReflection);
//				Color drawColor = (totalReflection ? Color.green : Color.magenta);
//				Debug.DrawRay(hit.point, outputDir.normalized * (rayLength - hitDist), drawColor, 0f, true);
//			}
//		}else{
//			Debug.DrawRay(muzzle.transform.position, muzzle.transform.forward * rayLength, Color.magenta, 0f, true);
//		}
		Optics.LightcastHit lightcastHit;
		Vector3[] points;
		Optics.Lightcast(muzzle.position, muzzle.forward, out lightcastHit, out points, rayLength);
		for(int i=1; i<points.Length; i++){
			Debug.DrawLine(points[i-1], points[i], Color.magenta, 0f, true);
		}
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
