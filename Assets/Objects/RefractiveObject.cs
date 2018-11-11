using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefractiveObject : MonoBehaviour {

	[SerializeField] float refractiveIndex;

	public float RefractiveIndex { get { return refractiveIndex; } }

	void Start () {
		if(!Optics.RefractiveObjectIsValid(this)) throw new UnityException("RefractiveObject  \"" + gameObject.name + "\" is invalid!");
	}

}
