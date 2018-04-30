using System.Collections;
using UnityEngine;

public class Grid : MonoBehaviour {
	public int width = 256;
	public int height = 256;
	private Texture2D texture;
	//TODO: Refactor all into ints to test for possibly faster performance on the CPU, or else possibly try to force Unity to offload flops onto GPU
	public float dA = 1.0f;
	public float dB = 0.5f;
	public float f = 0.055f;
	public float k = 0.062f;
	//TODO: Add new global array for nextCells, initialize and modify values instead
	private Cell[,] cells;

	// Use this for initialization
	void Start () {
		//TODO: Update at run time to apply texture to whatever item is selected, transform item between solid primitives
		initializeTexture ();

		initializeCells ();
	}
	
	// Update is called once per frame
	void Update () {
		reactDiffuse ();

		refreshTexture ();
	}
	//TODO: click to create new 4x4 'seeds'
	//TODO: add textures to Unity, use Color.lerp on loaded Texture colours instead of simply black/yellow.
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

		for (int x = 1; x < width - 1; ++x) {
			for (int y = 1; y < height - 1; ++y) {
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
	
}
