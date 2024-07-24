using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotate : MonoBehaviour {
	void Update () {
		//transform.Rotate(230.43f * Time.deltaTime * Random.value, 150.52f * Time.deltaTime * Random.value, 40.34f * Time.deltaTime * Random.value);
		transform.Rotate(0f, 0f, 40.34f * Time.deltaTime * Random.value);
	}
}
