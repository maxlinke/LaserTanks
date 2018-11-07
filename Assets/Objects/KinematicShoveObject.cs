using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicShoveObject : MonoBehaviour {

	[SerializeField] Rigidbody rb;
	[SerializeField] Transform start;
	[SerializeField] Transform end;
	[SerializeField] float speed;
	[SerializeField] float waitTimeAtEnd;
	[SerializeField] float waitTimeAtStart;

	Vector3 velocity;

	void Start () {
		rb.isKinematic = true;
		rb.transform.position = start.position;
		velocity = Vector3.zero;
		StartCoroutine(WaitAndRestart());
	}

	IEnumerator WaitAndRestart () {
		yield return new WaitForSeconds(waitTimeAtEnd);
		rb.MovePosition(start.position);
		yield return new WaitForSeconds(waitTimeAtStart);
		velocity = (end.position - start.position).normalized * speed;
	}
	
	void FixedUpdate () {
		if((rb.transform.position - start.position).magnitude > (end.position - start.position).magnitude){
			velocity = Vector3.zero;
			StartCoroutine(WaitAndRestart());
		}
		if(velocity != Vector3.zero){
			rb.MovePosition(rb.transform.position + (velocity * Time.fixedDeltaTime));
		}
	}
	
}
