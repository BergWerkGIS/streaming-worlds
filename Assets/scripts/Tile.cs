using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{

	void Start() { }
	void Update() { }



	public int Zoom { get { return _zoom; } }


	private TextMesh _text;
	//private GameObject _plane;
	//private MeshRenderer _meshRenderer;
	private int _zoom;


	public void Initialize(int z, int x, int y, float offset)
	{
		_zoom = z;
		string name = string.Format("{0}/{1}/{2}", z, x, y);

		_text = transform.Find("text").GetComponent<TextMesh>();
		//Debug.Log(_text.transform.rotation);
		_text.transform.SetPositionAndRotation(
			_text.transform.position
			, new Quaternion(0.7f, 0, 0, 0.7f)
		);
		_text.anchor = TextAnchor.MiddleCenter;
		_text.color = Color.red;
		_text.text = string.Format(
			"Initialize{0}{1}{0}{2}"
			, Environment.NewLine
			, name
			, DateTime.Now.ToString("HHmmss.fff")
		);

		//_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
		//_meshRenderer = _plane.AddComponent<MeshRenderer>();

		gameObject.name = name;
		gameObject.SetActive(true);

		float off = 1 * z + offset;

		Vector2dBounds bb = Conversions.TileIdToBounds(x, y, z);
		Conversions.TileIdToCenterLatitudeLongitude(x, y, z);

		Debug.Log("bbox:" + bb);
		Debug.Log("bbox.center:" + bb.Center);
		Vector2d centerWM = Conversions.LatLonToMeters(bb.Center);
		Debug.Log("bbox.center[WebMerc]:" + centerWM);

		Vector3 position = new Vector3(
			x * off
			, 0
			, y * off
		);
		transform.localPosition = position;
	}


	public void SetActive(bool active) { gameObject.SetActive(active); }



}
