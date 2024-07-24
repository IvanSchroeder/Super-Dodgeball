﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPan : MonoBehaviour {
	IEnumerator Pan () {
		while (true) {
			yield return new WaitForSeconds(2.0f);
			while(transform.position.x < 1.0f) {
				transform.Translate(0.2f * Time.deltaTime, 0, 0);
				yield return null;
			}
			yield return new WaitForSeconds(2.0f);
			while(transform.position.x > 0.0f) {
				transform.Translate(-0.2f * Time.deltaTime, 0, 0);
				yield return null;
			}
		}
	}
}
