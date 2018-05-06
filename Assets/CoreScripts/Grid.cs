using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]

public class Grid : MonoBehaviour {
	public static int textureResolution = 2048;
	public int width = textureResolution;
	public int height = textureResolution;
	private Texture2D texture;
	//TODO: Refactor all into ints to test for possibly faster performance on the CPU, or else possibly try to force Unity to offload flops onto GPU
	public float dA = 1.0f;
	public float dB = 0.5f;
	public float f = 0.055f;
	public float k = 0.062f;
	//TODO: Add new global array for nextCells, initialize and modify values instead
	private Cell[,] cells;

	//Shader code
	public ComputeShader shader;
	Renderer rend;
	RenderTexture[] rt; //we need multiple render textures to avoid multiple copies per compute step
	int currTex = 0;
	const int numTex = 2;
	//string consts prevent Unity recomputing strings
	private const string CSMain = "CSMain";
	private const string CurrTex = "currTex";
	private const string CurrTexA = "currTexA";
	private const string _MainTex = "_MainTex";
	private const string PrevTexA = "prevTexA";
	private const string PrevTex = "prevTex";
	private const string TexRes = "texRes";
	private const string Initialize = "Initialize";
	//at this point I should really have made it an enum

	//Needed for Raycasting/updating .b values on Ctrl+Click
	public Camera cam;

	//rotation variables begin
	#region ROTATE
	private float _sensitivity = 0.5f;
	private Vector3 _mouseReference = Vector3.zero;
	private Vector3 _mouseOffset;
	private Vector3 _rotation = Vector3.zero;
	private bool _isRotating;
	#endregion
	//rotation variables end

	//raycast variables begin
	private bool leftControl = false;
	private int tempX = 0;
	private int tempY = 0;
	//raycast variables end

	// Use this for initialization
	void Start () {
		//TODO: Update at run time to apply texture to whatever item is selected, transform item between solid primitives
		rt = new RenderTexture[numTex];
		for (int i = 0; i < numTex; ++i) {
			rt[i] = new RenderTexture(width,height,24,RenderTextureFormat.ARGBFloat);
			rt[i].enableRandomWrite = true;
			//rt[i].wrapMode = TextureWrapMode.Clamp;
			rt[i].Create ();
		}


		rend = GetComponent<Renderer> ();
		rend.enabled = true;

		initializeTextureInCompute ();

		//initializeTexture ();

		initializeCells ();

		cam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
		//reactDiffuse ();

		//refreshTexture ();

		updateTextureFromCompute ();

		if (Input.GetKeyDown (KeyCode.LeftControl)) {
			leftControl = true;
		}

		if (Input.GetKeyUp (KeyCode.LeftControl)) {
			leftControl = false;
		}

		if (leftControl && _isRotating) {
			RaycastHit hit;
			if (!Physics.Raycast (cam.ScreenPointToRay (Input.mousePosition), out hit))
				return;

			Renderer rend = hit.transform.GetComponent<Renderer> ();
			MeshCollider meshCollider = hit.collider as MeshCollider;

			if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
				return;

			Vector2 pixelUV = hit.textureCoord * width;
			tempX = Mathf.FloorToInt (pixelUV.x);
			tempY = Mathf.FloorToInt (pixelUV.y);

			cells [tempX, tempY].b = 1f;
			cells [tempX + 1, tempY].b = 1f;
			cells [tempX, tempY + 1].b = 1f;
			cells [tempX + 1, tempY + 1].b = 1f;

			return;
		}

		if (_isRotating)
		{
			_mouseOffset = (Input.mousePosition - _mouseReference);

			//_rotation.z = _mouseOffset.x * _sensitivity;
			_rotation.y = -_mouseOffset.x * _sensitivity;
			_rotation.x = _mouseOffset.y * _sensitivity;
			//_rotation.z = -_mouseOffset.y * _sensitivity;

			gameObject.transform.Rotate(_rotation);
			_mouseReference = Input.mousePosition;

			transform.eulerAngles += _rotation;
		}
	}
	//TODO: click to create new 4x4 'seeds' in shader
	void initializeTexture ()
	{
		texture = new Texture2D (width, height);
		Renderer renderer = GetComponent<Renderer> ();
		renderer.material.mainTexture = texture;
		for (int x = 0; x < width; ++x) {
			for (int y = 0; y < height; ++y) {
				texture.SetPixel (x, y, Color.white);
			}
		}
		texture.Apply ();
	}

	private void updateTextureFromCompute ()
	{
		int prevTex = currTex; //0-1
		currTex = (currTex + 1) % numTex; //1-0

		int kernelHandle = shader.FindKernel (CSMain);
		shader.SetTexture (kernelHandle, PrevTex, rt [prevTex]);
		shader.SetTexture (kernelHandle, CurrTex, rt [currTex]);
		shader.Dispatch (kernelHandle, width >> 3, height >> 3, 1);

		rend.material.SetTexture (_MainTex, rt[currTex]);
	}

	private void initializeTextureInCompute ()
	{
		int prevTex = currTex;
		currTex = currTex + 1;

		int kernelHandle = shader.FindKernel (Initialize);

		shader.SetTexture (kernelHandle, CurrTex, rt[currTex]);
		shader.Dispatch (kernelHandle, width >> 3, height >> 3, 1);

		rend.material.SetTexture (_MainTex, rt[currTex]);
	}
	//CPU bound vvvv
	void initializeCells ()
	{
		cells = new Cell[width, height];
		for (int x = 0; x < width; ++x) {
			for (int y = 0; y < height; ++y) {
				cells [x, y] = new Cell(1.0f,0f);
			}
		}

		cells [100, 100].b = 1f;
		cells [101, 100].b = 1f;
		cells [100, 101].b = 1f;
		cells [101, 101].b = 1f;

		cells [60, 60].b = 1f;
		cells [60, 61].b = 1f;
		cells [61, 60].b = 1f;
		cells [61, 61].b = 1f;
		cells [30, 60].b = 1f;
		cells [30, 61].b = 1f;
		cells [31, 60].b = 1f;
		cells [31, 61].b = 1f;

		cells [80, 60].b = 1f;
		cells [80, 61].b = 1f;
		cells [81, 60].b = 1f;
		cells [81, 61].b = 1f;
	}

	void refreshTexture ()
	{
		for (int x = 0; x < width; ++x) {
			for (int y = 0; y < height; ++y) {
				texture.SetPixel( x, y, Color.LerpUnclamped( Color.black, Color.yellow, cells[x,y].b ) );
			}
		}
		texture.Apply ();
	}

	void reactDiffuse ()
	{
		Cell[,] nextCells = new Cell[width, height];

		for (int x = 0; x < width; ++x) {
			for (int y = 0; y < height; ++y) {
				float a = cells [x, y].a;
				float b = cells [x, y].b;

				float laPlaceA = LaPlaceA (x, y);
				float laPlaceB = LaPlaceB (x, y);

				float nextA = a + (dA * laPlaceA - (a * b * b) + (f * (1 - a)));
				float nextB = b + (dB * laPlaceB + (a * b * b) - ((k + f) * b));

				nextCells [x, y] = new Cell(nextA, nextB);
			}
		}

		cells = nextCells;
	}

	float LaPlaceA(int x, int y) {
		float laPlaceA = cells [x, y].a * -1f;

		int wrappedLeftX = (x - 1 + width) % width;
		int wrappedRightX = (x + 1 + width) % width;
		int wrappedTopY = (y - 1 + height) % height;
		int wrappedBottomY = (y + 1 + height) % height;
		//conflate operations into one to reduce number of assignments & flops
		laPlaceA += (cells [wrappedRightX, y].a + cells [wrappedLeftX, y].a + cells [x, wrappedBottomY].a + cells [x, wrappedTopY].a) * 0.2f;

		laPlaceA += (cells [wrappedLeftX, wrappedTopY].a + cells [wrappedLeftX, wrappedBottomY].a + cells [wrappedRightX, wrappedTopY].a + cells [wrappedRightX, wrappedBottomY].a) * 0.05f;

		return laPlaceA;
	}

	float LaPlaceB(int x, int y) {
		float laPlaceB = cells [x, y].b * -1f;

		int wrappedLeftX = (x - 1 + width) % width;
		int wrappedRightX = (x + 1 + width) % width;
		int wrappedTopY = (y - 1 + height) % height;
		int wrappedBottomY = (y + 1 + height) % height;

		laPlaceB += (cells [wrappedRightX, y].b + cells [wrappedLeftX, y].b + cells [x, wrappedBottomY].b + cells [x, wrappedTopY].b) * 0.2f;

		laPlaceB += (cells [wrappedLeftX, wrappedTopY].b + cells [wrappedLeftX, wrappedBottomY].b + cells [wrappedRightX, wrappedTopY].b + cells [wrappedRightX, wrappedBottomY].b) * 0.05f;

		return laPlaceB;
	}

	void OnMouseDown()
	{
		_mouseReference = Input.mousePosition;

		_isRotating = true;
	}

	void OnMouseUp()
	{
		_isRotating = false;
	}
}
