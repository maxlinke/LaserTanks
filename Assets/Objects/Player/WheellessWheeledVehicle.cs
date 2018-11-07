using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheellessWheeledVehicle : MonoBehaviour {

	[SerializeField] Rigidbody rb;
	[SerializeField] Transform customCenterOfMass;
	[SerializeField] PhysicMaterial defaultPhysicMaterial;
	[SerializeField] Collider coll;

	[Header("Driving")]
	[SerializeField] float maxForwardSpeed;
	[SerializeField] float maxBackwardSpeed;
	[SerializeField] float maxAcceleration;
	[SerializeField] float maxDeceleration;
	[SerializeField] float sidewaysDeceleration;

	[Header("Steering")]
	[SerializeField] float steerPerSecond;
	[SerializeField] float maxAngularAcceleration;
	[Tooltip("Whether turning on the spot should be allowed")]
	[SerializeField] bool tankControls;

	//TODO remove the headers, add custom editor. that looks at "tankControls"
	//TODO wheels

	[Header("Wheeled Steering")]
	[SerializeField] float minTurnRadius;
	[SerializeField] float maxTurnRadius;
	[Tooltip("X: speed from 0 to maxForwardSpeed, Y: 0=minTurnRadius 1=maxTurnRadius")]
	[SerializeField] AnimationCurve turnRadiusLerpCurve;

	[Header("Tracked Steering")]
	[SerializeField] float standingMaxAngularVelocity;
	[SerializeField] float movingMaxAngularVelocity;
	[Tooltip("X: speed from 0 to maxForwardSpeed, Y: 0=standingMaxAngularVelocity 1=movingMaxAngularVelocity")]
	[SerializeField] AnimationCurve angularVelocityLerpCurve;

	float defaultStaticFriction, defaultDynamicFriction;
	List<ContactPoint> contactPoints;
	float steer;

	void Start () {
		ApplyCustomCenterOfMass(customCenterOfMass, rb);
		contactPoints = new List<ContactPoint>();
		coll.material = Instantiate(defaultPhysicMaterial);
		defaultStaticFriction = coll.material.staticFriction;
		defaultDynamicFriction = coll.material.dynamicFriction;
	}

	void Update () {
		if(rb.IsSleeping()) rb.WakeUp();
//		if(Input.GetKey(KeyCode.Q)) Time.timeScale = 0.01f;
//		else Time.timeScale = 1f;
	}
	
	void FixedUpdate () {
		if(rb.IsSleeping()) rb.WakeUp();

		float currentSpeed = GetSpeed(rb);
		float groundedness = GetGroundedness(contactPoints, rb);
		float gasInput = GetGasInput();
		float desiredSpeed = gasInput * ((gasInput >= 0f) ? maxForwardSpeed : maxBackwardSpeed);
		bool braking = GetIsBraking(desiredSpeed, currentSpeed);

		float steerInput = GetSteerInput();
		UpdateSteer(steerInput, Time.fixedDeltaTime, ref steer);

		//wheel acceleration is always along forward of rigidbody
		Vector3 currentWheelVelocity = rb.transform.forward * currentSpeed;
		Vector3 desiredWheelVelocity = (braking ? Vector3.zero : rb.transform.forward * desiredSpeed);
		float maxWheelAccel = (braking ? maxDeceleration : maxAcceleration);
		Vector3 wheelAcceleration = ClampedDeltaVAcceleration(desiredWheelVelocity, currentWheelVelocity, maxWheelAccel, Time.fixedDeltaTime);

		//auto sideways (wheel friction) deceleration 
		Vector3 sidewaysVelocity = Vector3.Project(rb.velocity, rb.transform.right);
		Vector3 sidewaysFrictionAcceleration = ClampedDeltaVAcceleration(Vector3.zero, sidewaysVelocity, sidewaysDeceleration, Time.fixedDeltaTime);

		//turning. side decel does the rest..
		float evalCurveInput = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxForwardSpeed);
		Vector3 angularAccel = Vector3.zero;
		if(tankControls){
			float angularVelocityCurveEval = angularVelocityLerpCurve.Evaluate(evalCurveInput);
			float maxAngularSpeed = Mathf.Lerp(standingMaxAngularVelocity, movingMaxAngularVelocity, angularVelocityCurveEval);
			Vector3 desiredAngularVelocity = rb.transform.TransformDirection(Vector3.up * steerInput * maxAngularSpeed);
			angularAccel = ClampedDeltaVAcceleration(desiredAngularVelocity, Vector3.Project(rb.angularVelocity, rb.transform.up), maxAngularAcceleration, Time.fixedDeltaTime);
		}else{
			float turnCurveEval = turnRadiusLerpCurve.Evaluate(evalCurveInput);
			float turnRadius = Mathf.Lerp(minTurnRadius, maxTurnRadius, turnCurveEval) / Mathf.Abs(steer);
			Vector3 turnCenter = rb.worldCenterOfMass + (rb.transform.right * turnRadius * Mathf.Sign(steer));
			if(turnRadius < 100f){
				Debug.DrawLine(rb.worldCenterOfMass, turnCenter, Color.white, Time.fixedDeltaTime, false);
				Debug.DrawRay(turnCenter, rb.transform.forward * 0.1f, Color.white, Time.fixedDeltaTime, false);
				Debug.DrawRay(turnCenter, rb.transform.forward * -0.1f, Color.white, Time.fixedDeltaTime, false);
				float deltaAngle = Mathf.Atan2(currentSpeed * Time.fixedDeltaTime, turnRadius) * Mathf.Rad2Deg * Mathf.Sign(steer);
				Vector3 desiredAngularVelocity = rb.transform.TransformDirection(Vector3.up * Mathf.Deg2Rad * deltaAngle / Time.fixedDeltaTime);
				angularAccel = ClampedDeltaVAcceleration(desiredAngularVelocity, Vector3.Project(rb.angularVelocity, rb.transform.up), maxAngularAcceleration, Time.fixedDeltaTime);
				angularAccel *= Mathf.Abs(Vector3.Dot(rb.velocity.normalized, rb.transform.forward));;
			}else{
				angularAccel = ClampedDeltaVAcceleration(Vector3.zero, Vector3.Project(rb.angularVelocity, rb.transform.up), maxAngularAcceleration, Time.fixedDeltaTime);
			}
		}

		Debug.DrawRay(rb.worldCenterOfMass + rb.transform.right * 0.0f, rb.velocity, Color.cyan, Time.fixedDeltaTime, false);
		Debug.DrawRay(rb.worldCenterOfMass + rb.transform.right * 0.1f, rb.transform.forward * desiredSpeed, Color.yellow, Time.fixedDeltaTime, false);
		Debug.DrawRay(rb.worldCenterOfMass + rb.transform.right * -0.1f, wheelAcceleration, Color.green, Time.fixedDeltaTime, false);
		Debug.DrawRay(rb.worldCenterOfMass + rb.transform.forward * 0.1f, sidewaysFrictionAcceleration, Color.red, Time.fixedDeltaTime, false);

		float frictionMultiplier = groundedness * (1f - Mathf.Abs(gasInput)) * (1f - Mathf.Abs(steerInput));
		coll.material.staticFriction = defaultStaticFriction * frictionMultiplier;
		coll.material.dynamicFriction = defaultDynamicFriction * frictionMultiplier;

		rb.velocity += (sidewaysFrictionAcceleration + wheelAcceleration) * groundedness * Time.fixedDeltaTime;
		rb.angularVelocity += angularAccel * groundedness * Time.fixedDeltaTime;

		contactPoints.Clear();
	}

	void OnCollisionEnter (Collision collision) {
		contactPoints.AddRange(collision.contacts);
	}

	void OnCollisionStay (Collision collision) {
		contactPoints.AddRange(collision.contacts);
	}

	void ApplyCustomCenterOfMass (Transform newCenterOfMass, Rigidbody rb) {
		if(newCenterOfMass != null){
			rb.centerOfMass = rb.transform.InverseTransformPoint(newCenterOfMass.position);
		}
	}

	float GetSteerInput () {
		float steerInput = 0f;
		if(Input.GetKey(KeyCode.A)) steerInput -= 1f;
		if(Input.GetKey(KeyCode.D)) steerInput += 1f;
		return steerInput;
	}

	float GetGasInput () {
		float gasInput = 0f;
		if(Input.GetKey(KeyCode.W)) gasInput += 1f;
		if(Input.GetKey(KeyCode.S)) gasInput -= 1f;
		return gasInput;
	}

	bool GetIsBraking (float speedTarget, float currentSpeed) {
		if(Mathf.Abs(currentSpeed) < 0.01f) return false;	//HACK but hey it works :P
		else{
			float deltaSpeed = speedTarget - currentSpeed;
			return (Mathf.Sign(currentSpeed) != Mathf.Sign(deltaSpeed));
		}
	}

	void UpdateSteer (float steerTarget, float deltaTime, ref float steer) {
		float deltaSteer = steerTarget - steer;
		if(Mathf.Abs(deltaSteer / deltaTime) > steerPerSecond){
			deltaSteer = Mathf.Sign(deltaSteer) * steerPerSecond;
		}else{
			deltaSteer /= deltaTime;
		}
		steer += deltaSteer * deltaTime;
	}

	float GetSpeed (Rigidbody rb) {
		Vector3 projectedVelocity = Vector3.Project(rb.velocity, rb.transform.forward);
		return projectedVelocity.magnitude * Mathf.Sign(Vector3.Dot(projectedVelocity, rb.transform.forward));
	}

	float GetGroundedness (List<ContactPoint> contactPoints, Rigidbody rb) {
		float maxGroundedNess = 0f;
		for(int i=0; i<contactPoints.Count; i++){
			ContactPoint point = contactPoints[i];
			float dot = Vector3.Dot(point.normal, rb.transform.up);
			maxGroundedNess = Mathf.Max(maxGroundedNess, dot);
		}
		return maxGroundedNess;
	}

	Vector3 ClampedDeltaVAcceleration (Vector3 targetVelocity, Vector3 currentVelocity, float maxAccel, float deltaTime) {
		Vector3 deltaV = targetVelocity - currentVelocity;
		Vector3 deltaVAccel = deltaV / deltaTime;
		if(deltaVAccel.magnitude > maxAccel){
			return deltaV.normalized * maxAccel;
		}else{
			return deltaVAccel;
		}
	}

}
