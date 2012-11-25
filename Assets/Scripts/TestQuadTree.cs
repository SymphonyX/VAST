//#define HORIZONTAL 0
//#define VERTICAL 1

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

class QuadTree2
{
	// ab
	// dc
	// a is the NW sub quad-tree
	// b is the NE sub quad-tree
	// c is the SE sub quad-tree
	// d is the SW sub quad-tree
	public QuadTree2 a = null;
	public QuadTree2 b = null;
	public QuadTree2 c = null;
	public QuadTree2 d = null;
	public QuadTree2 parent = null;
	public float minx = 0.0f;
	public float maxx = 0.0f;
	public float minz = 0.0f;
	public float maxz = 0.0f;
	public float cost = -1.0f;
	public bool containsObstacle = false;
	public QuadStruct st;
	
	
	public int level = 0;
	public bool tag = false;
	
	public bool dirty = false;
	
	static public int NUM_MAX_LEVEL = 7;
		
	override public string ToString() {
		return "[" + minx + "," + minz + "," + maxx + "," + maxz + "]";
	}
	public QuadTree2(){}
	
	public QuadTree2(float _minx, float _maxx, float _minz, float _maxz, int _level, QuadTree2 _parent) 
	{
		minx = _minx;
		maxx = _maxx;
		minz = _minz;
		maxz = _maxz;
		level = _level;
		parent = _parent;
	}
	
	// To find the quad-tree grid which encloses the point(xx, zz)
	public QuadTree2 Find(float xx, float zz, ref float tminx, ref float tmaxx, ref float tminz, ref float tmaxz)
	{
		if (xx >= minx && xx < maxx && zz >= minz && zz < maxz && a == null)
		{
			tminx = minx;
			tmaxx = maxx;
			tminz = minz;
			tmaxz = maxz;
			return this;
		}
		else if (a != null)
		{
			QuadTree2 retval = null;
			
			retval = a.Find(xx, zz, ref tminx, ref tmaxx, ref tminz, ref tmaxz);
			if (retval != null) return retval;
			
			retval = b.Find(xx, zz, ref tminx, ref tmaxx, ref tminz, ref tmaxz);
			if (retval != null) return retval;
			
			retval = c.Find(xx, zz, ref tminx, ref tmaxx, ref tminz, ref tmaxz);
			if (retval != null) return retval;
			
			retval = d.Find(xx, zz, ref tminx, ref tmaxx, ref tminz, ref tmaxz);
			if (retval != null) return retval;
		}
		
		return null;
	}
	
	public void BuildTree(Vector3 pt)
	{
		QuadTree2 tree = this;
		float half_x = (tree.maxx - tree.minx) / 2.0f;
		float half_z = (tree.maxz - tree.minz) / 2.0f;
		
		if (tree == null || tree.level >= QuadTree2.NUM_MAX_LEVEL)
		{
			return;
		}
		
		Bounds treeBound = new Bounds(new Vector3((tree.minx + tree.maxx) / 2.0f, horizontal_y, (tree.minz + tree.maxz) / 2.0f), 
										  new Vector3(tree.maxx - tree.minx, 5.0f, tree.maxz - tree.minz));
		pt.y = horizontal_y;
		Bounds bs = new Bounds(pt, Vector3.zero);
		int count = 0;
		if (Contain(treeBound, bs))
		{
			count = 1;
		}
		
		int grid_size_x = (int)(tree.maxx - tree.minx);
		
		
		if (count >= 1 && grid_size_x >= 2) {
			// branch node
						
			bool is_child_leave_node = false;
			if (grid_size_x <= 2)
				is_child_leave_node = true;
			
			tree.a = new QuadTree2(tree.minx, tree.minx + half_x, tree.minz, tree.minz + half_z, tree.level+1, tree);
			if (!is_child_leave_node) {
				tree.a.BuildTree(pt);
			}
			
			tree.b = new QuadTree2(tree.minx, tree.minx + half_x, tree.minz + half_z, tree.maxz, tree.level+1, tree);
			if (!is_child_leave_node) {
				tree.b.BuildTree(pt);
			}
			
			tree.c = new QuadTree2(tree.minx + half_x, tree.maxx, tree.minz + half_z, tree.maxz, tree.level+1, tree);
			if (!is_child_leave_node) {
				tree.c.BuildTree(pt);
			}
			
			tree.d = new QuadTree2(tree.minx + half_x, tree.maxx, tree.minz, tree.minz + half_z, tree.level+1, tree);
			if (!is_child_leave_node) {
				tree.d.BuildTree(pt);
			}
		}
	}
	
	public QuadTree2 RecoverTree()
	{
		if (parent != null)
		{
			if (!parent.a.tag) parent.a = null;
			if (!parent.b.tag) parent.b = null;
			if (!parent.c.tag) parent.c = null;
			if (!parent.d.tag) parent.d = null;
			
			if (parent.tag)
			{
				return parent;
			}
			else
			{
				return parent.RecoverTree();
			}
		}
		
		return null;
	}
	
	// static members and methods
	static public int num_of_cubes;
	static public QuadTree2 refTree = null;
	static public float rminx = -64.0f;
	static public float rminz = -64.0f;
	static public float rmaxx =  64.0f;
	static public float rmaxz =  64.0f;
	static public Bounds[] B = null;
	static public float horizontal_y = 0.0f;
	
	static public QuadTree2 Init(Bounds[] b, float y)
	{
		B = b;
		horizontal_y = y;
		num_of_cubes = b.Length;
		
		refTree = new QuadTree2(rminx, rmaxx, rminz, rmaxz, 0, null);		
		Construct(refTree);
		
		QuadTree2 root = null;
		CopyTree(ref refTree, ref root);
		return root;
	}
	
	static public void Construct(QuadTree2 tree)
	{
		if (tree == null)
			return;
		
		//print (tree.level);
		
		int count = 0;
		bool flag = false;
		
		for (int i = 0; i < num_of_cubes; i++)
		{
			Bounds treeBound = new Bounds(new Vector3((tree.minx + tree.maxx) / 2.0f, horizontal_y, (tree.minz + tree.maxz) / 2.0f), 
										  new Vector3(tree.maxx - tree.minx, 5.0f, tree.maxz - tree.minz));
			
			if (i == 0 && Contain(treeBound, B[i]))
			{
				tree.containsObstacle = true;
				count++;
				//break;
			}
			else if (!Contain(B[i], treeBound) && B[i].Intersects(treeBound))
			{
				flag = true;
				tree.containsObstacle = true;
				count++;
				//break;
			}
		}
		float half_x = (tree.maxx - tree.minx) / 2.0f;
		float half_z = (tree.maxz - tree.minz) / 2.0f;
		
		if (tree.level >= QuadTree2.NUM_MAX_LEVEL)
		{
			tree.tag = true;
			return;
		}
		
		int grid_size_x = (int)(tree.maxx - tree.minx);
		
		
		
		if (count >= 1)
		{
			// branch node
			
			bool is_child_leave_node = false;
			if (grid_size_x <= 2)
				is_child_leave_node = true;
			
			tree.a = new QuadTree2(tree.minx, tree.minx + half_x, tree.minz, tree.minz + half_z, tree.level+1, tree);
			if (is_child_leave_node) {
				// leave node
				tree.a.tag = flag;
			}
			else {
				Construct(tree.a);
			}
			
			tree.b = new QuadTree2(tree.minx, tree.minx + half_x, tree.minz + half_z, tree.maxz, tree.level+1, tree);
			if (is_child_leave_node) {
				// leave node
				tree.b.tag = flag;
			}
			else {
				Construct(tree.b);
			}
			
			tree.c = new QuadTree2(tree.minx + half_x, tree.maxx, tree.minz + half_z, tree.maxz, tree.level+1, tree);
			if (is_child_leave_node) {
				// leave node
				tree.c.tag = flag;
			}
			else {
				Construct(tree.c);
			}
			
			tree.d = new QuadTree2(tree.minx + half_x, tree.maxx, tree.minz, tree.minz + half_z, tree.level+1, tree);
			 
			if (is_child_leave_node) {
				// leave node
				tree.d.tag = flag;
			}
			else {
				Construct(tree.d);
			}

		}
		else if (count == 0)
		{
			// leave node
			tree.tag = true;
		}
	}
	
	static public void CopyTree(ref QuadTree2 fromTree, ref QuadTree2 toTree)
	{
		if (fromTree == null) {
			toTree = null;
			return;
		}
		
		toTree = new QuadTree2();
		toTree.dirty = fromTree.dirty;
		toTree.level = fromTree.level;
		toTree.maxx = fromTree.maxx;
		toTree.maxz = fromTree.maxz;
		toTree.minx = fromTree.minx;
		toTree.minz = fromTree.minz;
		toTree.cost = fromTree.cost;
		
		if (fromTree.a != null) CopyTree(ref fromTree.a, ref toTree.a);
		if (fromTree.b != null) CopyTree(ref fromTree.b, ref toTree.b);
		if (fromTree.c != null) CopyTree(ref fromTree.c, ref toTree.c);
		if (fromTree.d != null) CopyTree(ref fromTree.d, ref toTree.d);
	}	
	
	static public bool Contain(Bounds big, Bounds small) {
		return (small.min.x >= big.min.x && small.min.z >= big.min.z 
			 && small.max.x <= big.max.x && small.max.z <= big.max.z);
	}
	
	static public void DrawTree() {
		DrawTree(refTree);
	}
	
	// Draw tree using Gizmos utility
	static private void DrawTree(QuadTree2 tree)
	{
		if (tree == null)
			return;
		
		float y = horizontal_y + 2.0f;
		
		Gizmos.color = Color.grey;
			
		Gizmos.DrawLine(new Vector3(tree.minx, y, tree.minz), new Vector3(tree.minx, y, tree.maxz));
		Gizmos.DrawLine(new Vector3(tree.minx, y, tree.minz), new Vector3(tree.maxx, y, tree.minz));
		Gizmos.DrawLine(new Vector3(tree.maxx, y, tree.maxz), new Vector3(tree.maxx, y, tree.minz));
		Gizmos.DrawLine(new Vector3(tree.maxx, y, tree.maxz), new Vector3(tree.minx, y, tree.maxz));
			
		if (tree.a != null)
			DrawTree(tree.a);
		
		if (tree.b != null)
			DrawTree(tree.b);
		
		if (tree.c != null)
			DrawTree(tree.c);
		
		if (tree.d != null)
			DrawTree(tree.d);	
	}
	
	static public List<QuadTree2> findNeighborsToQuad(QuadTree2 quad)
	{
		List<QuadTree2> neighbors = new List<QuadTree2>();
		if (quad.level == 0)
			return neighbors;
		float a = 0.0f, b = 0.0f, c = 0.0f, d = 0.0f;
		QuadTree2 north = QuadTree2.refTree.Find(quad.minx + 0.5f, quad.maxz + 0.5f, ref a, ref b, ref c, ref d);
		if (north != null && north.level > quad.level) {
			foreach(QuadTree2 subtree in returnAllQuadsForNeighbor(north, quad, 0)) 
			{
				neighbors.Add(subtree);	
			}
		} else {
			neighbors.Add(north);	
		}
		
		QuadTree2 south = QuadTree2.refTree.Find(quad.minx + 0.5f, quad.minz - 0.5f, ref a, ref b, ref c, ref d);
		if (south != null && south.level > quad.level) {
			foreach (QuadTree2 subtree in returnAllQuadsForNeighbor(south, quad, 0))
			{
				neighbors.Add(subtree);	
			}
		} else {
			neighbors.Add(south);	
		}
		
		QuadTree2 east = QuadTree2.refTree.Find(quad.maxx + 0.5f, quad.minz + 0.5f, ref a, ref b, ref c, ref d);
		if (east != null && east.level > quad.level) {
			foreach(QuadTree2 subtree in returnAllQuadsForNeighbor(east, quad, 1))
			{
				neighbors.Add(subtree);	
			}
		} else {
			neighbors.Add(east);	
		}
		
		QuadTree2 west = QuadTree2.refTree.Find(quad.minx - 0.5f, quad.minz + 0.5f, ref a, ref b, ref c, ref d);
		if (west!= null && west.level > quad.level) {
			foreach(QuadTree2 subtree in returnAllQuadsForNeighbor(west, quad, 1))
			{
				neighbors.Add(subtree);	
			} 
		} else {
			neighbors.Add(west);	
		}
		QuadTree2 northWest = QuadTree2.refTree.Find(quad.minx - 0.5f, quad.maxz + 0.5f, ref a, ref b, ref c, ref d);
		neighbors.Add(northWest);
		QuadTree2 southWest = QuadTree2.refTree.Find(quad.minx - 0.5f, quad.minz - 0.5f, ref a, ref b, ref c, ref d);
		neighbors.Add(southWest);
		QuadTree2 northEast = QuadTree2.refTree.Find(quad.maxx + 0.5f, quad.maxz + 0.5f, ref a, ref b, ref c, ref d);
		neighbors.Add(northEast);
		QuadTree2 southEast = QuadTree2.refTree.Find(quad.maxx + 0.5f, quad.minz - 0.5f, ref a, ref b, ref c, ref d);
		neighbors.Add(southEast);
		return neighbors;
	}
	
	static private List<QuadTree2> returnAllQuadsForNeighbor(QuadTree2 neighbor, QuadTree2 currentQuad, int axis)
	{
		List<QuadTree2> neighborQuads = new List<QuadTree2>();
		int levelDifference = neighbor.level - currentQuad.level;
		int numberOfQuads = (int)Mathf.Pow(2.0f, (float)levelDifference);
		float min, max, offset;
		float a = 0.0f, b = 0.0f, c = 0.0f, d = 0.0f;
		QuadTree2 nextNeighbor;
		if (axis == 1) {
			min = currentQuad.minz;
			max = currentQuad.maxz;
			offset = (max - min)/numberOfQuads;
		} else {
			min = currentQuad.minx;
			max = currentQuad.maxx;
			offset = (max - min)/numberOfQuads;
		}
		
		for(int index = 0; index < numberOfQuads; ++index){
			float testPoint = min+(index*offset+0.5f);
			if (axis == 0) {
				nextNeighbor = refTree.Find(testPoint, neighbor.minz+0.5f, ref a, ref b, ref c, ref d);	
			} else {
				nextNeighbor = refTree.Find(neighbor.minx+0.5f, testPoint, ref a, ref b, ref c, ref d);	
			}
			neighborQuads.Add(nextNeighbor);
		}
		return neighborQuads; 
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct QuadStruct {
	public int parent;
	public int predecessor;
	public int index;
	public int NE, NW, SE, SW;
	public float minx, maxx, minz, maxz, distance;
	public int level;
};

// 1) Attach this to a 'Ground' object; 2) The 'Cube' objects are the obstacles.
public class TestQuadTree : MonoBehaviour {
	
	bool bInit = false;
	Bounds[] B = new Bounds[1000];
	Vector3 P = new Vector3(0, 0, 0);
	public GameObject pointer = null;
	public GameObject goal;
	List<QuadTree2> path;
	List<QuadStruct> structList;
	static int currentIndex = 0;
	public bool GPU = false;
	
	[DllImport("CUDA-DLL")]
	private static extern void calculateDistances(QuadStruct[] quads, int numberOfQuads, float startx, float starty);
	
	[DllImport("CUDA-DLL")]
	private static extern void createPath(QuadStruct[] quadsIn, QuadStruct[] quadsOut);
	
	QuadTree2 Init()
	{
		bInit = true;
		int num_of_cubes = 0;
		
		// P - a point in the scene that also needs to be treated like a 'Cube' obstacle
		B[num_of_cubes++] = new Bounds (pointer.transform.position, Vector3.zero);
		
		foreach(GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
		{			
		    if(go.name == "Cube" )
		    {
				if (Mathf.Abs (go.transform.rotation.z) >= 0.001f)
				{
					// the 'ramp' cube is the only special one to...
					B [num_of_cubes] = new Bounds (go.collider.bounds.center, go.collider.bounds.size);
					B [num_of_cubes].SetMinMax (new Vector3 ((B [num_of_cubes].min.x + B [num_of_cubes].max.x) / 2.0f, B [num_of_cubes].min.y, B [num_of_cubes].min.z),
						new Vector3 (B [num_of_cubes].max.x, B [num_of_cubes].max.y, B [num_of_cubes].max.z));
					num_of_cubes++;
				}
				else
				{
					B [num_of_cubes++] = new Bounds (go.collider.bounds.center, go.collider.bounds.size);
				}
			}
		}
		
		return QuadTree2.Init(B, transform.position.y);
	}
	
	void GlobalUpdate()
	{
		bInit = false;
	}
	
	void LocalUpdate(Vector3 oldPos, Vector3 newPos)
	{
		if (QuadTree2.refTree == null)
		{
			bInit = false;
		}
		else
		{
			float tminx1 = 0.0f;
			float tmaxx1 = 0.0f;
			float tminz1 = 0.0f;
			float tmaxz1 = 0.0f;
			QuadTree2 t1 = QuadTree2.refTree.Find(oldPos.x, oldPos.z, ref tminx1, ref tmaxx1, ref tminz1, ref tmaxz1);
			
			float tminx2 = 0.0f;
			float tmaxx2 = 0.0f;
			float tminz2 = 0.0f;
			float tmaxz2 = 0.0f;
			QuadTree2 t2 = QuadTree2.refTree.Find(newPos.x, newPos.z, ref tminx2, ref tmaxx2, ref tminz2, ref tmaxz2);
			
			if (!t1.Equals(t2) && t1.parent != t2.parent)
			{
				QuadTree2 parentTree = t1.RecoverTree();
				if (parentTree != null)
				{
					bool flag2 = true;
					
					if (parentTree.minx <= t2.minx &&
						parentTree.maxx >= t2.maxx &&
						parentTree.minz <= t2.minz && 
						parentTree.maxz >= t2.maxz)
					{
						flag2 = false;
					}
					
					if (flag2)
					{
						if (tmaxx2 == tminx1)
						{
							// t2 is a neighbor to the left of t1
							//print ("t2 is a neighbor to the left of t1");
							t2.BuildTree(newPos);
						}
						else if (tminx2 == tmaxx1)
						{
							// t2 is a neighbor to the right of t1
							//print ("t2 is a neighbor to the right of t1");
							t2.BuildTree(newPos);
						}
						else if (tminz2 == tmaxz1)
						{
							//t2 is a neighbor on top of t1
							//print ("t2 is a neighbor on top of t1");
							t2.BuildTree(newPos);
						}
						else if (tmaxz2 == tminz1)
						{
							//t2 is a neighbor below t1
							//print ("t2 is a neighbor below t1");
							t2.BuildTree(newPos);
						}
						else
						{
							// not a neighbor
							bInit = false;
						}
					}
					else
					{
						parentTree.BuildTree(newPos);
					}
				}
				else
				{
					bInit = false;
				}
			}
			else
			{
				// they are the same quad-tree node
			}
			
			P = new Vector3(newPos.x, newPos.y, newPos.z);
		}
	}
	
	void calculateCosts(QuadTree2 tree)
	{ 
		
		if (tree.a == null && tree.b == null && tree.c == null && tree.d == null) {
			Vector2 start = new Vector2(pointer.transform.position.x, pointer.transform.position.z);
			Vector2 treeCenter = new Vector2(tree.minx+(tree.maxx - tree.minx)/2, tree.minz+(tree.maxz - tree.minz)/2);
			tree.cost = Vector2.Distance(treeCenter, start);
		} else {
			if (tree.a != null) calculateCosts(tree.a);
			if (tree.b != null) calculateCosts(tree.b);
			if (tree.c != null) calculateCosts(tree.c);
			if (tree.d != null) calculateCosts(tree.d);
		}
	}
	
	List<QuadTree2> getPath()
	{
		Vector2 start = new Vector2(pointer.transform.position.x, pointer.transform.position.z);
		Vector2 goalPosition = new Vector2(goal.transform.position.x, goal.transform.position.z);
		float a = 0.0f, b = 0.0f, c = 0.0f, d = 0.0f;
		QuadTree2 startQuad = QuadTree2.refTree.Find(start.x, start.y, ref a, ref b, ref c, ref d);
		QuadTree2 currentQuad = QuadTree2.refTree.Find(goalPosition.x, goalPosition.y, ref a, ref b, ref c, ref d);
		List<QuadTree2> pathToReturn = new List<QuadTree2>();
		
		while(currentQuad != startQuad) {
			List<QuadTree2> neighbors = QuadTree2.findNeighborsToQuad(currentQuad);
			QuadTree2 predecessor = null;
			foreach(QuadTree2 neighbor in neighbors)
			{
				if (predecessor == null) predecessor = neighbor;
				else {
					if (neighbor.cost < predecessor.cost && !containsObstacle(neighbor)){
						predecessor = neighbor; 	
					}
				}
			}
			pathToReturn.Add(currentQuad);
			currentQuad = predecessor;
		}
		pathToReturn.Add(currentQuad);
		return pathToReturn;
	}
	
	bool containsObstacle(QuadTree2 tree)
	{
		foreach(GameObject obstacle in GameObject.FindGameObjectsWithTag("movable obstacles"))
		{
			Vector3 obstaclePosition = obstacle.transform.position;
			if(obstaclePosition.x >= tree.minx && obstaclePosition.x <= tree.maxx
				&& obstaclePosition.z >= tree.minz && obstaclePosition.z <= tree.maxz)
				return true;
		}
		return false;
	}
	
	QuadStruct structFromQuadTree(QuadTree2 quad)
	{
		QuadStruct st = new QuadStruct();
		st.level = quad.level;
		st.maxx = quad.maxx; st.minx = quad.minx;
		st.maxz = quad.maxz; st.minz = quad.minz;
		st.NE = -1; st.NW = -1; st.SE = -1; st.SW = -1;
		st.distance = -1;
		st.index = -1;
		st.parent = -1;
		return st;
	}
	
	void populateStructList(QuadTree2 root)
	{
		Queue<QuadTree2> structQueue = new Queue<QuadTree2>();
		structQueue.Enqueue(root);
		int count = 0;
		while (structQueue.Count > 0) {
			QuadTree2 quad = structQueue.Dequeue();
			count++;
			if (quad == root) count = 0;
			quad.st = structFromQuadTree(quad);
			quad.st.index = currentIndex++;
			if (quad.parent != null) {
				quad.st.parent = quad.parent.st.index;
				if (count == 4) {
				int parentIndex = quad.parent.st.index;
				QuadStruct parentStruct = structList[parentIndex];
				parentStruct.NE = quad.parent.a.st.index;
				parentStruct.NW = quad.parent.b.st.index;
				parentStruct.SE = quad.parent.c.st.index;
				parentStruct.SW = quad.parent.d.st.index;
				structList[parentIndex] = parentStruct;
				count = 0;
				}
			}
			structList.Add(quad.st);
			if (quad.a != null) {
				List<QuadTree2> children = new List<QuadTree2>();
				children.Add(quad.a); children.Add(quad.b);
				children.Add(quad.c); children.Add(quad.d);
				foreach(QuadTree2 child in children) {
					structQueue.Enqueue(child);
				}
			}
		}
	}
	
	void Start() 
	{
		structList = new List<QuadStruct>();
		Init();
		populateStructList(QuadTree2.refTree);
		QuadStruct[] structs = structList.ToArray();
		QuadStruct[] structPath = new QuadStruct[20];
		if (GPU) {
			calculateDistances(structs, structList.Count, pointer.transform.position.x, pointer.transform.position.z);
			createPath(structs, structPath);
			
		} else {
			calculateCosts(QuadTree2.refTree);
			path = getPath();
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (pointer != null && P != pointer.transform.position)
		{
			GlobalUpdate();
			//LocalUpdate(P, new Vector3(pointer.transform.position.x, pointer.transform.position.y, pointer.transform.position.z));
		}
	}
	
	void OnDrawGizmos ()
	{
		if (!bInit)
		{
			QuadTree2 tree = Init();
		}
		
		QuadTree2.DrawTree();
		
		Gizmos.color = Color.black;
		if (path != null && path.Count > 0) {
			foreach(QuadTree2 tree in path)
			{
				Vector3 center =  new Vector3(tree.minx+(tree.maxx-tree.minx)/2, 1.5f, tree.minz+(tree.maxz-tree.minz)/2);
				Gizmos.DrawSphere(center, 0.25f);
			}
		}

	}
}
