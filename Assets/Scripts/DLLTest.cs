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

public enum PlannerType {Optimal = 0, SubOptimal = 1, MinIndex = 2};

public class DLLTest : MonoBehaviour {
	
	const float NEEDSUPDATE = -1.0f;
	const float GOALSTATE = -3.0f;
	const float STARTSTATE = 0.0f;
	const float OBSTACLESTATE = 50000.0f;
	
	enum Direction {Left, Right, Up, Down};
	
	public GameObject start, goal;
	public PlannerType plannerType = PlannerType.Optimal;
	public int rows, columns;
	private GameObject[] obstacles;
	private GameObject selectedGameObject;
	private float[] computedCosts;
	private float[] stateHValues, stateGValues, stateCostValues;
	private float[] stateGrayScaleValues;
	private float[] hMap, gMap, costMap;
	private StateStruct[] minIndexStates;
	private float minh, maxh, minf, maxf, ming, maxg, minCost, maxCost;
	public bool showHMap, showFMap, showGMap, showCostMap;
	List<StateStruct> path;
	
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
	
	[DllImport("CUDA-DLL")]
	private static extern void returnGMap(float[] gMap);
	
	[DllImport("CUDA-DLL")]
	private static extern void returnCostMap(float[] costMap);
	
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
		stateGrayScaleValues = generateScaledValues(computedCosts, maxf);
	}
	
	private void generateGMap()
	{
		ming = Mathf.Infinity;
		maxg = Mathf.NegativeInfinity;
		gMap = new float[rows*columns];
		returnGMap(gMap);
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				float g = gMap[i*columns+j];
				if (ming > g) ming = g;
				if (maxg < g && g != OBSTACLESTATE) maxg = g;
			}
		}
		stateGValues = generateScaledValues(gMap, maxg);
	}
	
	private float[] generateScaledValues(float[] rawValuesMap, float maxValues)
	{
		float[] scaledValues = new float[rows*columns];
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				if (rawValuesMap[index] == OBSTACLESTATE) {
					scaledValues[index] = OBSTACLESTATE;	
				} else {
					scaledValues[index] = rawValuesMap[index] / maxValues;
				}
			}
		}
		return scaledValues;
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
		stateHValues = generateMapValues(hMap, maxh);
	}
	
	private void generateCostMap()
	{
		costMap = new float[rows*columns];
		returnCostMap(costMap);
		minCost = Mathf.Infinity;
		maxCost = Mathf.NegativeInfinity;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				float cost = costMap[i*columns+j];
				
				if (minCost > cost) minCost = cost;
				if (maxCost < cost) maxCost = cost;
				
			}
		}
		stateCostValues = generateMapValues(costMap, maxCost);
	}
	
	private float[] generateMapValues(float[] map, float maxvalue)
	{
		float[] colorValues = new float[rows*columns];
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				colorValues[index] = map[index] / maxvalue;	
			}
		}
		return colorValues;
	}
	
	private List<StateStruct> getStateNeighbors(StateStruct state)
	{
		List<StateStruct> neighbors = new List<StateStruct>();
		if (state.x < columns-1) {
			neighbors.Add(stateAtPosition(state.x+1, state.y));
		}
		if (state.x > 0) {
			neighbors.Add(stateAtPosition(state.x-1, state.y));
		}
		if (state.y < rows-1) {
			neighbors.Add(stateAtPosition(state.x, state.y+1));
		}
		if (state.y > 0) {
			neighbors.Add(stateAtPosition(state.x, state.y-1));
		}
		if (state.x < columns-1 && state.y < rows-1) {
			neighbors.Add(stateAtPosition(state.x+1, state.y+1));
		}
		if (state.x < columns-1 && state.y > 0) {
			neighbors.Add(stateAtPosition(state.x+1, state.y-1));
		}
		if (state.x > 0 && state.y < rows-1) {
			neighbors.Add(stateAtPosition(state.x-1, state.y+1));
		}
		if (state.x > 0 && state.y > 0) {
			neighbors.Add(stateAtPosition(state.x-1, state.y-1));
		}
		return neighbors;
	}
	
	private List<StateStruct> constructPath()
	{
		List<StateStruct> statePath = new List<StateStruct>();
		List<StateStruct> neighbors = new List<StateStruct>();
		List<int> neighborsForMinIndex = new List<int>();
		StateStruct state = stateAtPosition(Mathf.RoundToInt(goal.transform.position.x), Mathf.RoundToInt(goal.transform.position.z));
		if (plannerType == PlannerType.MinIndex) {
			state = minIndexStates[Mathf.RoundToInt(goal.transform.position.z)*columns+Mathf.RoundToInt(goal.transform.position.x)];	
		}
		float x = goal.transform.position.x;
		float y = goal.transform.position.z;
		do {
			statePath.Add(state);
			StateStruct predecessor = new StateStruct();
			if (plannerType == PlannerType.MinIndex) {
				predecessor = minIndexStates[state.predy*columns+state.predx];
			} else {
				predecessor = stateAtPosition(state.predx, state.predy);
			}
			float f = Mathf.Infinity;
			float g = Mathf.Infinity;
			
			state = predecessor;
			x = predecessor.x;
			y = predecessor.y;
		} while(x != start.transform.position.x || y != start.transform.position.z);
		
		return statePath;
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
					state.f = STARTSTATE; state.g = STARTSTATE;
				} else {
					state.f = NEEDSUPDATE; state.g = NEEDSUPDATE;
				}
				minIndexStates[i*columns+j] = state;
			}
		}
		
		

		
		obstacles = GameObject.FindGameObjectsWithTag("movable obstacles");
		for(int i = 0; i < obstacles.Length; ++i) {
			int obstaclex = Mathf.RoundToInt(obstacles[i].transform.position.x);
			int obstacley = Mathf.RoundToInt(obstacles[i].transform.position.z);
			insertValuesInMap(obstaclex, obstacley, OBSTACLESTATE, 20.0f);	
			StateStruct state = minIndexStates[obstacley*columns+obstaclex];
			state.f = OBSTACLESTATE; state.g = OBSTACLESTATE;
			state.predx = -1; state.predy = -1;
			minIndexStates[obstacley*columns+obstaclex] = state;
		}
				
		getCostsMarshal();

		runKernel();
		
		path = constructPath();
		generateHMap();
		generateGMap();
		generateFMap();	
		generateCostMap();
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
		generateGMap();
		generateCostMap();
	}
	
	int indexOfState(Vector3 vec)
	{
		return Mathf.RoundToInt(vec.z)*columns+Mathf.RoundToInt(vec.x);	
	}
	
	void runKernel()
	{
		if (plannerType == PlannerType.Optimal) {
			computeCostsOptimal();	
			getCostsMarshal();
		} else if (plannerType == PlannerType.SubOptimal) {
			computeCostsSubOptimal();	
			getCostsMarshal();
		} else if (plannerType == PlannerType.MinIndex) {
			int startIndex = Mathf.RoundToInt(start.transform.position.z*columns+start.transform.position.x);
			int minIndex = 0;
			int goalIndex = Mathf.RoundToInt(goal.transform.position.z*columns+goal.transform.position.x);
			do {
				StateStruct goalState = minIndexStates[goalIndex];
				goalState.f = NEEDSUPDATE; goalState.g = NEEDSUPDATE;
				minIndexStates[goalIndex] = goalState;
				minIndex = computeCostsMinIndexCPU();	
			} while (minIndex != goalIndex && !statesAreConsistentMinIndexCPU());
			//computeCostsMinIndex();
		}
	}
	
	bool statesAreConsistentMinIndexCPU()
	{
		List<StateStruct> statesToCheck = new List<StateStruct>();
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				StateStruct state = minIndexStates[i*columns+j];
				if (state.f != NEEDSUPDATE && state.f != OBSTACLESTATE) {
					statesToCheck.Add(state);	
				}
			}
		}
		
		for (int i = 0; i < statesToCheck.Count; i++) {
			StateStruct state = statesToCheck[i];
			if (!stateIsConsistent(state)) {
				return false;	
			}
		}
		return true;
	}
	
	bool stateIsConsistent(StateStruct state) 
	{
		List<StateStruct> neighbors = getStateNeighbors(state);
		for (int i = 0; i < neighbors.Count; i++) {
			StateStruct neighbor = neighbors[i];
			if (neighbor.f < minIndexStates[state.predy*columns+state.predx].f) {
				return false;	
			}
		}
		return true;
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
		List<StateStruct> statesToUpdate = new List<StateStruct>();
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < columns; j++) {
				int index = i*columns+j;
				List<int> neighbors = neighborsForPosition(j, i);
				bool hasNieghborToUpdate = false;
				for(int s = 0; s < neighbors.Count; s++) {
					if (minIndexStates[neighbors[s]].f > -1 && minIndexStates[neighbors[s]].f != OBSTACLESTATE) {
						hasNieghborToUpdate = true;	
					}
				}
				
				if (minIndexStates[index].f < 0 && hasNieghborToUpdate) {
					statesToUpdate.Add(minIndexStates[index]);	
				}
			}
		}
		
		float minCost = Mathf.Infinity;
		for (int i = 0; i < statesToUpdate.Count; i++) {
			StateStruct state = statesToUpdate[i];
			List<int> neighborsToUpdateIndex = neighborsForPosition(state.x, state.y);
			int updateIndex = state.y*columns+state.x;
			for (int s = 0; s < neighborsToUpdateIndex.Count; s++) {
				StateStruct neighbor = minIndexStates[neighborsToUpdateIndex[s]];
				
				Vector2 vec1 = new Vector2((float)state.x, (float)state.y);
				Vector2 vec2 = new Vector2((float)neighbor.x, (float)neighbor.y);
				Vector2 goalVec = new Vector2(goal.transform.position.x, goal.transform.position.z);
				float newCost = neighbor.g+Vector2.Distance(vec1, vec2)+Vector2.Distance(vec1,goalVec);
				
				if (neighbor.f != NEEDSUPDATE && (newCost < state.f || state.f < 0)) {
					computedCosts[updateIndex] = newCost;
					state.f = newCost; state.g = neighbor.g+Vector2.Distance(vec1, vec2);
					state.predx = neighbor.x; state.predy = neighbor.y;
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
	
	void updateSuccessorMinIndexCPU(StateStruct state)
	{	
		List<StateStruct> listToUpdate = new List<StateStruct>();
		List<int> neighbors = neighborsForPosition(state.x, state.y);
		for (int i = 0; i < neighbors.Count; i++) {
			int neighborIndex = neighbors[i];
			StateStruct neighbor = minIndexStates[neighborIndex];
			if (neighbor.predx == state.x && neighbor.predy == state.y && neighbor.f != OBSTACLESTATE) {
				listToUpdate.Add(neighbor);
			}
		}
		for (int i = 0; i < listToUpdate.Count; i++) {
			StateStruct updateNeighbor = listToUpdate[i];
			updateNeighbor.f = NEEDSUPDATE; updateNeighbor.g = NEEDSUPDATE;
			minIndexStates[updateNeighbor.y*columns+updateNeighbor.x] = updateNeighbor;
			computedCosts[updateNeighbor.y*columns+updateNeighbor.x] = NEEDSUPDATE;
			updateSuccessorMinIndexCPU(updateNeighbor);
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
		StateStruct state = stateAtPosition(newStateX, newStateY);
		List<StateStruct> neighbors = getStateNeighbors(state);
		foreach (StateStruct neighbor in neighbors) {
			if (stateIsNotAnObstacle(neighbor)) {
				insertValuesInMap(neighbor.x, neighbor.y, NEEDSUPDATE, 1.0f);	
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
		if (plannerType == PlannerType.MinIndex) {
			StateStruct state = minIndexStates[previousStateY*columns+previousStateX];
			state.f = NEEDSUPDATE; state.g = NEEDSUPDATE;
			minIndexStates[previousStateY*columns+previousStateX] = state;
			
			state = minIndexStates[newStateY*columns+newStateX];
			state.f = NEEDSUPDATE; state.g = NEEDSUPDATE;
			minIndexStates[newStateY*columns+newStateX] = state;
			computedCosts[previousStateY*columns+previousStateX] = NEEDSUPDATE;
			computedCosts[newStateY*columns+newStateX] = NEEDSUPDATE;
		} else {
			StateStruct stateToUpdate = stateAtPosition(previousStateX, previousStateY);
			insertValuesInMap(previousStateX, previousStateY, NEEDSUPDATE, 1.0f);	
			updateNeighborsToState(stateToUpdate);
		}
		runKernel();
		path = constructPath();
	}
	
	void updateNeighborsToState(StateStruct state)
	{
		List<StateStruct> neighbors = getStateNeighbors(state);
		List<StateStruct> listToUpdate = new List<StateStruct>();
		foreach (StateStruct neighbor in neighbors) {
			if (neighbor.predx == state.x && neighbor.predy == state.y) {
				listToUpdate.Add(neighbor);	
			}
		}
		foreach (StateStruct updateNeighbor in listToUpdate) {
			insertValuesInMap(updateNeighbor.x, updateNeighbor.y, NEEDSUPDATE, 1.0f);	
			updateNeighborsToState(updateNeighbor);
		}
	}
	
	void handleObstacleMovement(Vector3 previousState, Vector3 newState) 
	{	
		int newx = Mathf.RoundToInt(newState.x); 
		int newy = Mathf.RoundToInt(newState.z);
		int prevx = Mathf.RoundToInt(previousState.x); 
		int prevy = Mathf.RoundToInt(previousState.z);
		if (plannerType == PlannerType.MinIndex) {
			StateStruct state = minIndexStates[prevy*columns+prevx];
			state.f = NEEDSUPDATE; state.g = NEEDSUPDATE;
			minIndexStates[prevy*columns+prevx] = state;
			
			state = minIndexStates[newy*columns+newx];
			state.f = OBSTACLESTATE; state.g = OBSTACLESTATE;
			state.predx = -1; state.predy = -1;
			minIndexStates[newy*columns+newx] = state;
			
			computedCosts[prevy*columns+prevx] = NEEDSUPDATE;
			computedCosts[newy*columns+newx] = OBSTACLESTATE;
			updateSuccessorMinIndexCPU(minIndexStates[newy*columns+newx]);

		} else {
			insertValuesInMap(newx, newy, OBSTACLESTATE, 20.0f);
			insertValuesInMap(prevx, prevy, NEEDSUPDATE, 1.0f);
		}
		foreach (StateStruct pathState in path) {
			if (pathState.x == newx && pathState.y == newy) {
				if (plannerType != PlannerType.MinIndex) {
					StateStruct stateToUpdate = stateAtPosition(newx, newy);
					updateNeighborsToState(stateToUpdate);
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
	
	void OnDrawGizmos() {
		if(path	!= null) {
			foreach(StateStruct state in path) {
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(new Vector3((float)state.x, 0.5f, (float)state.y), 0.25f);	
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
		
		if (showHMap && stateHValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateHValues[i*columns+j];

					float blue = 1 * val;
					float green = 1 - blue;
					Gizmos.color = new Color(0.0f, green, blue, 0.5f);	
					
					Gizmos.DrawCube(new Vector3(j, 1.0f, i), new Vector3(1.0f, 0.0f, 1.0f));
				}
			}
		}
		
		if (showCostMap && stateCostValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateCostValues[i*columns+j];

					float blue = 1 * val;
					float green = 1 - blue;
					Gizmos.color = new Color(0.0f, green, blue, 0.5f);	
					
					Gizmos.DrawCube(new Vector3(j, 1.0f, i), new Vector3(1.0f, 0.0f, 1.0f));
				}
			}
		}
		
		if (showGMap && stateGValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateGValues[i*columns+j];
					if (val == OBSTACLESTATE) {
						Gizmos.color = Color.black;	
					} else {
						Gizmos.color = new Color(1-val, 1-val, 1-val, 0.5f);	
					}
					Gizmos.DrawCube(new Vector3(j, 1.0f, i), new Vector3(1.0f, 0.0f, 1.0f));
				}
			}
		}
		
		if (showFMap && stateGrayScaleValues != null) {
		 	for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					float val = stateGrayScaleValues[i*columns+j];
					
					if (val == OBSTACLESTATE) {
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
