using UnityEngine;
using System.Text;
using System.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices;



[StructLayout(LayoutKind.Sequential)]
public struct StateStruct {
	public int x;
	public int y;
	public float g;
	public float f;
	public float costToReach;
	public int predx;
	public int predy;
};


public class DLLTest : MonoBehaviour {
	
	const float NEEDSUPDATE = -1.0f;
	const float GOALSTATE = -3.0f;
	const float STARTSTATE = 0.0f;
	const float OBSTACLESTATE = 50000.0f;
	
	enum Direction {Left, Right, Up, Down};
	
	public GameObject start, goal;
	public bool optimal;
	public bool suboptimal;
	public bool minIndexOptimal;
	public int rows, columns;
	private GameObject[] obstacles;
	private GameObject selectedGameObject;
	private float[] computedCosts;
	private float[] gValuesForCPUMinIndex;
	private float[] stateColorValues;
	private float[] stateGrayScaleValues;
	private float[] hMap;
	private StateStruct[] minIndexStates;
	private float minh, maxh, minf, maxf;
	public bool showColorMap, showGrayScaleMap;
	List<Vector3> path;
	
	[DllImport("CUDA-DLL")]
	private static extern void generateTexture(int rows, int columns);
	
	[DllImport("CUDA-DLL")]
	private static extern void insertStart(int x, int y, float costToReach);
	
	[DllImport("CUDA-DLL")]
	private static extern void insertGoal(int x, int y, float costToReach);
	
	[DllImport("CUDA-DLL")]
	private static extern void insertValuesInMap(int x, int y, float cost, float costToReach);
	
	[DllImport("CUDA-DLL")]
	private static extern void computeCostsSubOptimal();
	
	[DllImport("CUDA-DLL")]
	private static extern void computeCostsOptimal();
	
	[DllImport("CUDA-DLL")]
	private static extern void computeCostsMinIndex();
	
	[DllImport("CUDA-DLL")]
	private static extern float fValueForPosition(int x, int y);
	
	[DllImport("CUDA-DLL")]
	private static extern float gValueForPosition(int x, int y);
	
	[DllImport("CUDA-DLL")]
	private static extern void textureCosts(float[] costs);
	
	[DllImport("CUDA-DLL")]
	private static extern StateStruct stateAtPosition(int x, int y);
	
	[DllImport("CUDA-DLL")]
	private static extern void returnHMap(float[] hMap);
	
	private void getCostsMarshal()
	{
		computedCosts = new float[rows*columns];
		textureCosts(computedCosts);
	}
	
	private void generateFMap()
	{
		minf = Mathf.Infinity;
		maxh = Mathf.NegativeInfinity;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				float cost = computedCosts[i*columns+j];
				if (minf > cost) minf = cost;
				if (maxf < cost && cost != OBSTACLESTATE) maxf = cost;
			}
		}
		generateGrayScaleValues();
	}
	
	private void generateGrayScaleValues()
	{
		stateGrayScaleValues = new float[rows*columns];
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				if (computedCosts[index] == OBSTACLESTATE) {
					stateGrayScaleValues[index] = OBSTACLESTATE;	
				} else {
					stateGrayScaleValues[index] = computedCosts[index] / maxf;
				}
			}
		}
	}
	
	private void generateHMap()
	{
		hMap = new float[rows*columns];
		returnHMap(hMap);
		minh = Mathf.Infinity;
		maxh = Mathf.NegativeInfinity;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				float cost = hMap[i*columns+j];
				
				if (minh > cost) minh = cost;
				if (maxh < cost) maxh = cost;
				
			}
		}
		generateColorValues();
	}
	
	private void generateColorValues()
	{
		stateColorValues = new float[rows*columns];
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				stateColorValues[index] = hMap[index] / maxh;	
			}
		}
		
	}
	
	private List<Vector2> getNeighbors(int x, int y)
	{
		List<Vector2> neighbors = new List<Vector2>();
		neighbors.Add(new Vector2(x+1, y));
		neighbors.Add(new Vector2(x-1, y));
		neighbors.Add(new Vector2(x, y+1));
		neighbors.Add(new Vector2(x, y-1));
		neighbors.Add(new Vector2(x+1, y+1));
		neighbors.Add(new Vector2(x+1, y-1));
		neighbors.Add(new Vector2(x-1, y+1));
		neighbors.Add(new Vector2(x-1, y-1));
		return neighbors;
	}
	
	private List<Vector3> constructPath()
	{
		List<Vector3> path = new List<Vector3>();
		List<Vector2> neighbors = new List<Vector2>();
		float x = goal.transform.position.x;
		float y = goal.transform.position.z;
		do {
			path.Add(new Vector3(x, 0.5f, y));
			neighbors = getNeighbors(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
			float f = Mathf.Infinity;
			float g = Mathf.Infinity;
			Vector2 predecessorPosition = new Vector2();
			StateStruct state = minIndexStates[Mathf.RoundToInt(y)*columns+Mathf.RoundToInt(x)];
			foreach(Vector2 neighbor in neighbors) {
				if (!isValidNeighbor(neighbor)) 
					continue;
				int neighbor_x = Mathf.RoundToInt(neighbor.x);
				int neighbor_z = Mathf.RoundToInt(neighbor.y);
				float newf = computedCosts[neighbor_x+rows*neighbor_z];
				float newg = 0;
				if (minIndexOptimal) {
					newg = gValuesForCPUMinIndex[neighbor_z*columns+neighbor_x];	
				} else {
					newg = gValueForPosition(neighbor_x, neighbor_z);	
				}
				if (newg < g && newf >= 0) {
					//f = newf;	
					g = newg;
					predecessorPosition = neighbor;
				}
			}
			state.predx = Mathf.RoundToInt(predecessorPosition.x);
			state.predy = Mathf.RoundToInt(predecessorPosition.y);
			minIndexStates[Mathf.RoundToInt(y)*columns+Mathf.RoundToInt(x)] = state;
			x = predecessorPosition.x;
			y = predecessorPosition.y;
		} while(x != start.transform.position.x || y != start.transform.position.z);
		
		return path;
	}
	
	bool isValidNeighbor(Vector2 neighbor)
	{
		int neighbor_x = Mathf.RoundToInt(neighbor.x);
		int neighbor_z = Mathf.RoundToInt(neighbor.y);
		if (neighbor_x < 0 || neighbor_x >= columns || neighbor_z < 0 || neighbor_z >= rows) {
			return false;	
		}
		return true;
	}
	

	// Use this for initialization
	void Start () {
		generateTexture(rows, columns);
		int goalx = Mathf.RoundToInt(goal.transform.position.x);
		int goaly = Mathf.RoundToInt(goal.transform.position.z);
		insertGoal(goalx, goaly, 1.0f);
		
		int startx = Mathf.RoundToInt(start.transform.position.x); 
		int starty = Mathf.RoundToInt(start.transform.position.z);
		insertStart(startx, starty, 1.0f);
		
		minIndexStates = new StateStruct[rows*columns];
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				StateStruct state = new StateStruct();
				state.x = j; state.y = i;
				if (i == startx && j == starty) {
					state.f = 0.0f; state.g = 0.0f;
				} else {
					state.f = Mathf.Infinity; state.g = Mathf.Infinity;
				}
				minIndexStates[i*columns+j] = state;
			}
		}
		
		

		
		obstacles = GameObject.FindGameObjectsWithTag("movable obstacles");
		for(int i = 0; i < obstacles.Length; ++i) {
			int obstaclex = Mathf.RoundToInt(obstacles[i].transform.position.x);
			int obstacley = Mathf.RoundToInt(obstacles[i].transform.position.z);
			insertValuesInMap(obstaclex, obstacley, OBSTACLESTATE, 20.0f);	
		}
				
		getCostsMarshal();
		gValuesForCPUMinIndex = new float[rows*columns];
		for (int i = 0; i < rows*columns; i++) {
			gValuesForCPUMinIndex[i] = Mathf.Infinity;	
		}

		runKernel();
		
		path = constructPath();
		generateHMap();
		generateFMap();	
	}
	
		// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)){
			selectGameObject();
		}
		
		if (selectedGameObject != null) {
			Vector3 previousState = selectedGameObject.transform.position;
			if (Input.GetKeyDown(KeyCode.RightArrow)) {
				handleUpdateForGameObjectWithDirection(selectedGameObject, Direction.Right);
			}
			if (Input.GetKeyDown(KeyCode.LeftArrow)) {
				handleUpdateForGameObjectWithDirection(selectedGameObject, Direction.Left);
			}
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				handleUpdateForGameObjectWithDirection(selectedGameObject, Direction.Up);
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				handleUpdateForGameObjectWithDirection(selectedGameObject, Direction.Down);
			}
		}
	}
	
	void selectGameObject()
	{
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if(Physics.Raycast(ray, out hit, 100.0f)){
			Debug.Log("Selected");
			selectedGameObject = hit.transform.gameObject;
		}
		else{
			Debug.Log("Nothing selected");	
		}
	}
	
	void handleUpdateForGameObjectWithDirection(GameObject selectedGameObject, Direction direction)
	{
		Vector3 previousState = selectedGameObject.transform.position;
		Vector3 translation = new Vector3(0.0f, 0.0f, 0.0f);
		switch (direction) {
		case Direction.Left:
			translation.x = -1.0f;
			break;
		case Direction.Right:
			translation.x = 1.0f;
			break;
		case Direction.Up:
			translation.z = 1.0f;
			break;
		case Direction.Down:
			translation.z = -1.0f;
			break;
		}
		
		selectedGameObject.transform.Translate(translation);
		if (selectedGameObject == goal) {
			insertGoal(Mathf.RoundToInt(goal.transform.position.x), Mathf.RoundToInt(goal.transform.position.z), 1.0f);
			generateHMap();	
			handleGoalMovement(previousState, goal.transform.position);
		}  else if (selectedGameObject == start) {
			handleStartMovement(previousState, start.transform.position);	
		} else {
			handleObstacleMovement(previousState, selectedGameObject.transform.position);
		}
		generateFMap();
	}
	
	int indexOfState(Vector3 vec)
	{
		return Mathf.RoundToInt(vec.z)*columns+Mathf.RoundToInt(vec.x);	
	}
	
	void runKernel()
	{
		if (optimal) {
			computeCostsOptimal();	
			getCostsMarshal();
		} else if (suboptimal) {
			computeCostsSubOptimal();	
			getCostsMarshal();
		} else if (minIndexOptimal) {
			int startIndex = Mathf.RoundToInt(start.transform.position.z*columns+start.transform.position.x);
			gValuesForCPUMinIndex[startIndex] = STARTSTATE;
			int minIndex = 0;
			int goalIndex = Mathf.RoundToInt(goal.transform.position.z*columns+goal.transform.position.x);
			do {
				minIndex = computeCostsMinIndexCPU();	
			} while (minIndex != goalIndex);
			//computeCostsMinIndex();
		}
	}
	
	private List<int> neighborsForPosition(int x, int y)
	{
		int index = y*columns+x;
		List<int> neighbors = new List<int>();
		if (x > 0) {
			neighbors.Add(index-1);
			if (y > 0) {
				neighbors.Add(index-1-columns);
			} 
			if (y < rows-1) {
				neighbors.Add(index-1+columns);
			}
		} 
		if (y > 0) {
			neighbors.Add(index-columns);				
		}
		if (y < rows-1) {
			neighbors.Add(index+columns);			
		}

		if (x < columns-1) {
			neighbors.Add(index+1);
			if (y > 0) {
				neighbors.Add(index-columns+1);	
			}
			if (y < rows-1) {
				neighbors.Add(index+columns+1);	
			}
		}
		return neighbors;
	}
	
	private int computeCostsMinIndexCPU()
	{
		int minIndex = 0;
		List<int> indexesToUpdate = new List<int>();
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				List<int> neighbors = neighborsForPosition(j, i);
				bool hasNieghborToUpdate = false;
				for(int s = 0; s < neighbors.Count; s++) {
					if (computedCosts[neighbors[s]] > -1 && computedCosts[neighbors[s]] != OBSTACLESTATE) {
						hasNieghborToUpdate = true;	
					}
				}
				
				if (computedCosts[index] < 0 && hasNieghborToUpdate) {
					indexesToUpdate.Add(index);	
				}
			}
		}
		
		float minCost = Mathf.Infinity;
		for (int a = 0; a < indexesToUpdate.Count; a++) {
			int updateIndex = indexesToUpdate[a];
			int x = updateIndex%columns;
			int y = updateIndex/columns;
			
			List<int> neighborsToUpdateIndex = neighborsForPosition(x, y);
			for (int s = 0; s < neighborsToUpdateIndex.Count; s++) {
				
				float neighborCost = computedCosts[neighborsToUpdateIndex[s]];
				float neighborG = gValuesForCPUMinIndex[neighborsToUpdateIndex[s]];
				int neighbor_x = neighborsToUpdateIndex[s]%columns;
				int neighbor_y = neighborsToUpdateIndex[s]/columns;
				Vector2 vec1 = new Vector2((float)x, (float)y);
				Vector2 vec2 = new Vector2((float)neighbor_x, (float)neighbor_y);
				Vector2 goalVec = new Vector2(goal.transform.position.x, goal.transform.position.z);
				float costToUpdate = computedCosts[updateIndex];
				float newCost = neighborG+Vector2.Distance(vec1, vec2)+Vector2.Distance(vec1,goalVec);
				
				if (neighborCost != NEEDSUPDATE && (newCost < costToUpdate || costToUpdate < 0)) {
					computedCosts[updateIndex] = newCost;
					gValuesForCPUMinIndex[updateIndex] = neighborG+Vector2.Distance(vec1, vec2);
					StateStruct state = minIndexStates[updateIndex];
					state.f = newCost; state.g = neighborG+Vector2.Distance(vec1, vec2);
					state.predx = neighbor_x; state.predy = neighbor_y;
					updateSuccessorMinIndexCPU(updateIndex);
					minIndexStates[updateIndex] = state;
					if (newCost <= minCost) {
						minCost = newCost;
						minIndex = updateIndex;	
					}
				}
			}
		}
		return minIndex;
	}
	
	void updateSuccessorMinIndexCPU(int stateIndex)
	{
		int statex = stateIndex % columns; 
		int statey = stateIndex / columns;
		List<int> neighbors = neighborsForPosition(statex, statey);
		for (int i = 0; i < neighbors.Count; i++) {
			int neighborIndex = neighbors[i];
			if (minIndexStates[neighborIndex].predx == statex && minIndexStates[neighborIndex].predy == statey) {
				//state is this neighbor's predecessor
				computedCosts[neighborIndex] = NEEDSUPDATE;
			}
		}
	}
	
	/********************************
	****** MOVEMENT HANDLING ********
	*********************************/
	
	void handleStartMovement (Vector3 previousState, Vector3 newState)
	{
		int newStateX = Mathf.RoundToInt(newState.x);
		int newStateY = Mathf.RoundToInt(newState.z);
		insertStart(newStateX, newStateY, 1.0f);
		List<Vector2> neighbors = getNeighbors(newStateX, newStateY);
		foreach (Vector2 neighbor in neighbors) {
			if (isValidNeighbor(neighbor)) {
				StateStruct neighborState = stateAtPosition(Mathf.RoundToInt(neighbor.x), Mathf.RoundToInt(neighbor.y));
				if (stateIsNotAnObstacle(neighborState)) {
					insertValuesInMap(Mathf.RoundToInt(neighbor.x), Mathf.RoundToInt(neighbor.y), NEEDSUPDATE, 1.0f);	
				}
			}
		}
		runKernel();
		path = constructPath();
	}
	
	void handleGoalMovement (Vector3 previousState, Vector3 newState)
	{	
		int previousStateX = Mathf.RoundToInt(previousState.x);
		int previousStateY = Mathf.RoundToInt(previousState.z);
		int newStateX = Mathf.RoundToInt(newState.x);
		int newStateY = Mathf.RoundToInt(newState.z);
		if (minIndexOptimal) {
			for (int i = 0; i < rows; ++i) {
				for (int j = 0; j < columns; ++j) {
					int index = i*columns+j;
					if (computedCosts[index] > -1 && computedCosts[index] != OBSTACLESTATE) {
						computedCosts[index] = gValuesForCPUMinIndex[index] + hMap[index];
					}
				}
			}
			computedCosts[previousStateY*columns+previousStateX] = NEEDSUPDATE;
			computedCosts[newStateY*columns+newStateX] = NEEDSUPDATE;
		} else {
			insertValuesInMap(previousStateX, previousStateY, NEEDSUPDATE, 1.0f);	
		}
		runKernel();
		path = constructPath();
	}
	
	void handleObstacleMovement(Vector3 previousState, Vector3 newState) 
	{	
		int newx = Mathf.RoundToInt(newState.x); 
		int newy = Mathf.RoundToInt(newState.z);
		int prevx = Mathf.RoundToInt(previousState.x); 
		int prevy = Mathf.RoundToInt(previousState.z);
		if (minIndexOptimal) {
			computedCosts[prevy*columns+prevx] = NEEDSUPDATE;
			computedCosts[newy*columns+newx] = OBSTACLESTATE;
			gValuesForCPUMinIndex[newy*columns+newx] = OBSTACLESTATE;
		} else {
			insertValuesInMap(newx, newy, OBSTACLESTATE, 20.0f);
			insertValuesInMap(prevx, prevy, NEEDSUPDATE, 1.0f);
		}
		foreach (Vector3 pathState in path) {
			if (pathState == newState) {
				if (minIndexOptimal) {
					int index = newy*columns+newx;
					updateSuccessorMinIndexCPU(index);
				} else {
					checkPredecessor(newState);
				}
				break;
			}
		}

 		runKernel();
		path = constructPath();
	}
	
	bool stateIsNotAnObstacle(StateStruct state)
	{
		foreach(GameObject obstacle in obstacles) {
			if (Mathf.RoundToInt(obstacle.transform.position.x) == state.x && Mathf.RoundToInt(obstacle.transform.position.z) == state.y) {
				return false;
			}
		}
		return true;
	}
	
	void checkPredecessor(Vector3 state)
	{
		List<Vector2> neighbors = new List<Vector2>();
		neighbors = getNeighbors((int)state.x, (int)state.z);
		foreach(Vector2 neighbor in neighbors) {
			if (isValidNeighbor(neighbor)) {
				StateStruct neighborState = stateAtPosition((int)neighbor.x, (int)neighbor.y);
				if (stateIsNotAnObstacle(neighborState)) {
					if (neighborState.predx == (int)state.x && neighborState.predy == (int)state.z) {
						int neighborx = Mathf.RoundToInt(neighbor.x); 
						int neighbory = Mathf.RoundToInt(neighbor.y);
						insertValuesInMap(neighborx, neighbory, NEEDSUPDATE, 1.0f);	
					}
				}
			}
		}
	}
	

	
	void OnDrawGizmos() {
		if(path	!= null) {
			foreach(Vector3 position in path) {
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(position, 0.25f);	
			}
		}
		Gizmos.color = Color.yellow;
		if(selectedGameObject != null){
			if(selectedGameObject.collider is BoxCollider)
				Gizmos.DrawWireCube(selectedGameObject.transform.position, (selectedGameObject.collider as BoxCollider).size);	
			else if(selectedGameObject.collider is SphereCollider)
				Gizmos.DrawWireSphere(selectedGameObject.transform.position, (selectedGameObject.collider as SphereCollider).radius);	
		}
		
		Gizmos.DrawWireCube(new Vector3(columns/2, 1.0f, rows/2), new Vector3(columns, 0.0f, rows)); 
		
		if (showColorMap && stateColorValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateColorValues[i*columns+j];

					float blue = 1 * val;
					float green = 1 - blue;
					Gizmos.color = new Color(0.0f, green, blue, 0.5f);	
					
					Gizmos.DrawCube(new Vector3(j, 1.0f, i), new Vector3(1.0f, 0.0f, 1.0f));
				}
			}
		}
		
		if (showGrayScaleMap && stateGrayScaleValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateGrayScaleValues[i*columns+j];
					
					if (val == 50000.0f) {
						Gizmos.color = Color.black;	
					} else {
						Gizmos.color = new Color(1-val, 1-val, 1-val, 0.5f);	
					}
					
					Gizmos.DrawCube(new Vector3(j, 1.0f, i), new Vector3(1.0f, 0.0f, 1.0f));
				}
			}
		}
		
		
	}
	
}
