using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Renderer))]
public class Goal : MonoBehaviour{
	static public bool 	goalMet = false;
	[Header("Goal Colors")]
	public Color idleColor = new Color(0f, 1f, 0f, 0.5f);
	public Color hitColor = new Color(0f, 1f, 0f, 0.8f);

	void Start() {
		Material mat = GetComponent<Renderer>().material;
		mat.color = idleColor;
	}

	void OnTriggerEnter(Collider other) {
		if (goalMet) return;

		// Detect projectile even when the collider belongs to a child object.
		Projectile proj = other.GetComponent<Projectile>();
		if (proj == null) {
			proj = other.GetComponentInParent<Projectile>();
		}
		if (proj == null && other.attachedRigidbody != null) {
			proj = other.attachedRigidbody.GetComponent<Projectile>();
		}

		if (proj != null) {
			// if so, set goalMet = true
			Goal.goalMet = true;

			// also set the alpha of the color of higher opacity
			Material mat = GetComponent<Renderer>().material;
			mat.color = hitColor;
		}
	}
}
