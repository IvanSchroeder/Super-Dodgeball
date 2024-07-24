using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;

[ExecuteInEditMode]
public class PixelCamera : MonoBehaviour {
	public enum RenderMode {
		Width,
		Height
	}

	[Serializable]
	public struct Resolution {
		[Range(1, 1920)]
		public int width;
		[Range(1, 1080)]
		public int height;

		public Resolution(int _width, int _height) {
			width = _width;
			height = _height;
		}
	}

	[SerializeField] private Camera mainCamera;

	[Header("Resize parameters")]
    public IntSO pixelsPerUnit;
	public RenderMode renderMode = RenderMode.Height;
    [SerializeField] private bool isResizable;
	[Range(1, 8)]
    [SerializeField] private int resolutionScale = 1;
    [Min(0.5f)]
    [SerializeField] private float zoomMultiplier = 1f;
    [SerializeField] private Resolution referenceResolution = new Resolution(1920, 1080);
    [SerializeField] private Resolution renderResolution = new Resolution(640, 360);
    [SerializeField] private float screenRatio;

	private void Awake () {
		GetCamera();
	}

    private void OnValidate() {
        GetCamera();
        ResizeCamera();
    }

	private void GetCamera() {
		if (mainCamera == null) mainCamera = GetComponent<Camera>();
	}

	private void Update() {
		switch (renderMode) {
			case RenderMode.Width:
				if (Screen.width != renderResolution.width) ResizeCamera();
			break;
			case RenderMode.Height:
				if (Screen.height != renderResolution.height) ResizeCamera();
			break;
		}
	}

	private void ResizeCamera() {
		float size = 11.25f;
        screenRatio = referenceResolution.width.ToFloat() / referenceResolution.height.ToFloat();

        if (isResizable) {
			switch (renderMode) {
				case RenderMode.Width:
					renderResolution = new Resolution();
					renderResolution.width = referenceResolution.width / resolutionScale;
					renderResolution.height = (renderResolution.width / screenRatio).ToInt();

					size = (renderResolution.width.ToFloat() / pixelsPerUnit.Value / 2f) * zoomMultiplier;
				break;
				case RenderMode.Height:
					renderResolution.height = referenceResolution.height / resolutionScale;
					renderResolution.width = (renderResolution.height * screenRatio).ToInt();

					size = (renderResolution.height.ToFloat() / pixelsPerUnit.Value / 2f) * zoomMultiplier;
				break;
			}
        }
		
		mainCamera.orthographicSize = size;
    }
	
	// void OnRenderImage(RenderTexture source, RenderTexture destination) {
	// 	RenderTexture buffer = RenderTexture.GetTemporary(renderResolution.width, renderResolution.height, -1);
	
	// 	buffer.filterMode = FilterMode.Point;
	// 	source.filterMode = FilterMode.Point;
	
	// 	Graphics.Blit(source, buffer);
	// 	Graphics.Blit(buffer, destination);
	
	// 	RenderTexture.ReleaseTemporary(buffer);
	// }
}