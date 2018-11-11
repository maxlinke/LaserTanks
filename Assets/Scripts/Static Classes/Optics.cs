using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Optics {

	private const float maxRefractiveObjectSize = 100f;

	public struct LightcastHit {
		
		public readonly RaycastHit raycastHit;
		public readonly float totalDistance;

		public LightcastHit (RaycastHit hit, float distance) {
			this.raycastHit = hit;
			this.totalDistance = distance;
		}

	}

	public static bool RefractiveObjectIsValid (RefractiveObject obj) {
		Collider[] colliders = obj.gameObject.GetComponentsInChildren<Collider>(true);
		if(colliders.Length != 1){
			return false;
		}else{
			Collider coll = colliders[0];
			if(coll is BoxCollider) return true;
			else if(coll is SphereCollider) return true;
			else if(coll is CapsuleCollider) return true;
			else if(coll is MeshCollider) return ((MeshCollider)coll).convex;
			else return false;
		}
	}

	/// <summary>
	/// Raycast that behaves like light, refracting and reflecting off surfaces. It is assumed that the Lightcast starts in air instead of another medium.
	/// </summary>
	/// <param name="start">Start of cast in world coordinates.</param>
	/// <param name="direction">Direction of cast in world space.</param>
	/// <param name="hit">Info about what was hit.</param>
	/// <param name="points">All the points the cast passed through.</param>
	/// <param name="maxDistance">Maximum distance, it is recommended to NOT use infinity.</param>
	/// <param name="layerMask">Raycast layer mask.</param>
	public static bool Lightcast (Vector3 start, Vector3 direction, out LightcastHit hit, out Vector3[] points, float maxDistance = Mathf.Infinity, int layerMask = ~0) {
		direction = direction.normalized;
		hit = new LightcastHit(new RaycastHit(), maxDistance);
		bool hitSomething = false;
		List<Vector3> pointList = new List<Vector3>();
		float remainingDistance = maxDistance;
		while(remainingDistance > 0f){
			pointList.Add(start);
			RaycastHit rayHit;
			if(Physics.Raycast(start, direction, out rayHit, remainingDistance, layerMask)){
				remainingDistance -= (start - rayHit.point).magnitude;
				RefractiveObject refractiveOther = rayHit.collider.GetComponent<RefractiveObject>();
				ReflectiveObject reflectiveOther = rayHit.collider.GetComponent<ReflectiveObject>();
				if(refractiveOther != null){
					bool totalReflection;
					Vector3 tempStart = rayHit.point;
					Vector3 tempDir = Refract(direction, rayHit.normal, 1f, refractiveOther.RefractiveIndex, out totalReflection);
					if(!totalReflection){
						//ray is now inside refractive object
						Collider refractiveColl = rayHit.collider;
						while(remainingDistance > 0f){
							pointList.Add(tempStart);
							//check where the ray hits and wheter it exits the collider or gets reflected again...
							if(refractiveColl.Raycast(new Ray(tempStart + (tempDir.normalized * maxRefractiveObjectSize), tempDir * -1f), out rayHit, maxRefractiveObjectSize)){
								remainingDistance -= (tempStart - rayHit.point).magnitude;
								if(remainingDistance > 0f){
									pointList.Add(rayHit.point);
									tempStart = rayHit.point;
									tempDir = Refract(tempDir, -rayHit.normal, refractiveOther.RefractiveIndex, 1f, out totalReflection);
									if(!totalReflection){
										//ray didn't get reflected and left refractive object
										start = tempStart;
										direction = tempDir;
										break;
									}
								}else{
									//ray just reflected and refracted around inside without ever getting anywhere...
									Vector3 finalPoint = tempStart + (tempDir.normalized * remainingDistance);
									pointList.Add(finalPoint);
									break;
								}
							}else{
								//something went wrong
								Debug.LogWarning("collidercast didn't hit anything!");
								remainingDistance = 0f;
								break;
							}
						}
					}else{
						//ray was totally reflected and never entered the refractive object
						start = tempStart;
						direction = tempDir;
					}
				}else if(reflectiveOther != null){
					start = rayHit.point;
					direction = Vector3.Reflect(direction, rayHit.normal);
				}else{
					pointList.Add(rayHit.point);
					hit = new LightcastHit(rayHit, maxDistance - remainingDistance);
					break;
				}
			}else{
				pointList.Add(start + (direction.normalized * remainingDistance));
				break;
			}
		}
		points = pointList.ToArray();
		return hitSomething;
	}
		
	public static Vector3 Refract (Vector3 rayDirection, Vector3 hitNormal, float incomingRefractiveIndex, float outgoingRefractiveIndex, out bool totalReflection) {
		float threshold = GetTotalReflectionThresholdAngle(incomingRefractiveIndex, outgoingRefractiveIndex);
		float hitAngle = Vector3.Angle(rayDirection, hitNormal * -1f);
		if(hitAngle >= threshold){
			totalReflection = true;
			return Vector3.Reflect(rayDirection, hitNormal);
		}else{
			totalReflection = false;
			float outputAngleInRadians = Mathf.Asin((outgoingRefractiveIndex * Mathf.Sin(Mathf.Deg2Rad * hitAngle)) / incomingRefractiveIndex);
			Vector3 projectedRay = Vector3.ProjectOnPlane(rayDirection, hitNormal);
			return ((Mathf.Cos(outputAngleInRadians) * hitNormal.normalized * -1f) + (Mathf.Sin(outputAngleInRadians) * projectedRay.normalized));
		}
	}

	static void RefractiveLoop (RaycastHit rayHit, float refractiveIndex, ref Vector3 start, ref Vector3 direction, ref float remainingDistance) {
		bool totalReflection;
		direction = Refract(direction, rayHit.normal, 1f, refractiveIndex, out totalReflection);
		start = rayHit.point;
		if(!totalReflection){
			//inside the refractive body now...
			Collider coll = rayHit.collider;
			while(remainingDistance > 0f){
				//new raycast from the outside to figure out where the internal ray would hit. assuming convex bodies here...
				Vector3 tempStart = start + (direction.normalized * maxRefractiveObjectSize);
				Vector3 tempDir = direction * -1f;
				if(coll.Raycast(new Ray(tempStart, tempDir), out rayHit, maxRefractiveObjectSize)){

				}else{
					//something went wrong..
				}
			}
		}else{
			//ray was immediately reflected. no changes to made anymore
		}
	}

	static float GetTotalReflectionThresholdAngle (float incomingRefractiveIndex, float outgoingRefractiveIndex) {
		return Mathf.Rad2Deg * Mathf.Asin(incomingRefractiveIndex / outgoingRefractiveIndex);
	}
	
}
