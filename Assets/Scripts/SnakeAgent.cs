using UnityEngine;
using MLAgents;
using System.Collections.Generic;
using System.Linq;
using MLAgents.Sensor;

public class SnakeAgent : Agent {
    
    public Vector2 dir = Vector2.right;
    public List<Transform> tail = new List<Transform>();
    public List<Transform> foods = new List<Transform>();
    public GameObject agentPrefab;
    public GameObject tailPrefab;
    public GameObject foodPrefab;
    private Transform borderTop;
    private Transform borderBottom;
    private Transform borderLeft;
    private Transform borderRight;
    bool ate = false;
    bool lost = false;
    private Vector2 action = new Vector2(1f, 1f);
    private Vector3 startPosition;
    private Quaternion startRotation;
    int width;
    int height;

    // Start is called before the first frame update
    void Start() {
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        borderLeft = GameObject.FindGameObjectWithTag("BorderLeft").transform;
        borderRight = GameObject.FindGameObjectWithTag("BorderRight").transform;
        borderTop = GameObject.FindGameObjectWithTag("BorderTop").transform;
        borderBottom = GameObject.FindGameObjectWithTag("BorderBottom").transform;
        width = (int)Mathf.Abs(borderRight.position.x - borderLeft.position.x) - 1;
        height = (int)Mathf.Abs(borderTop.position.y - borderBottom.position.y) - 1;

        // Move the Snake every 200ms
        InvokeRepeating("Move", 0.15f, 0.15f);
        SpawnFood();
    }

    void FixedUpdate() {
        if(Input.GetKey(KeyCode.UpArrow)) {
            action.y = 0f;
            action.x = 1f;
        } else if(Input.GetKey(KeyCode.LeftArrow)) {
            action.x = 0f;
            action.y = 1f;
        } else if(Input.GetKey(KeyCode.DownArrow)) {
            action.y = 2f;
            action.x = 1f;
        } else if(Input.GetKey(KeyCode.RightArrow)) {
            action.x = 2f;
            action.y = 1f;
        }
    }

    public void RespawnNewAgent() {
        Instantiate(agentPrefab, startPosition, startRotation);
    }

    public override void AgentReset() {
        tail.Clear();

        var foods = GameObject.FindGameObjectsWithTag("Food");
        foreach(var food in foods) {
            Destroy(food);
        }

        var tailParts = GameObject.FindGameObjectsWithTag("Tail");
        foreach(var tailPart in tailParts) {
            Destroy(tailPart);
        }

        SpawnFood();

        gameObject.transform.position = startPosition;
        lost = false;
    }

    public override void AgentAction(float[] vectorAction) {

        // Move in a new Direction?
        if (vectorAction[1] == 2) dir = Vector2.right;
        else if (vectorAction[0] == 2) dir = Vector2.down;
        else if (vectorAction[1] == 0) dir = Vector2.left;
        else if (vectorAction[0] == 0) dir = Vector2.up;

        // Move head into new direction (now there is a gap)
        transform.Translate(dir);

        action.x = 1f;
        action.y = 1f;
    }

    void Move() {
        // Save current position (gap will be here)
        Vector2 gapPosition = transform.position;

        if (ate) {
            AddReward(10f);

            // Load Prefab into the world
            GameObject newTailElement = (GameObject)Instantiate( tailPrefab,
                                                    gapPosition,
                                                    Quaternion.identity);
            newTailElement.tag = "Tail";

            // Keep track of it in our tail list
            tail.Insert(0, newTailElement.transform);

            // Reset the flag
            ate = false;
        }

        if (lost) {
            Lost();
        } else if (tail.Count > 0) {
            // Move last Tail Element to where the Head was
            tail.Last().position = gapPosition;

            // Add to front of list, remove from the back
            tail.Insert(0, tail.Last());
            tail.RemoveAt(tail.Count-1);
        }

        RequestDecision();
    }

    void SpawnFood() {

        // Find valid food positon
        int x = 0, y = 0;
        bool validPosition = false;
        while (!validPosition) {

            // x position between left & right border
            x = (int)Random.Range(borderLeft.position.x, borderRight.position.x);
            // y postition in top & bottom border
            y = (int)Random.Range(borderBottom.position.y, borderTop.position.y);

            // Check if position is valid
            if(true) validPosition = true; // TODO: Define a condition. In snake itself or other food would be invalid.
        }

        // Instantiate the food at (x, y)
        GameObject newFood = (GameObject)Instantiate(   foodPrefab, 
                                                        new Vector2(x, y), 
                                                        Quaternion.identity); // default rotation
        newFood.tag = "Food";
    }

    public override void CollectObservations() {

        // HOPEFULLY SMARTER IMPLEMENTATION:
        // relative position of nearest food
        Vector2 nearestFood = new Vector2(Mathf.Infinity, Mathf.Infinity);
        foreach(var food in foods) {
            if((food.transform.positon - transform.position).Abs < nearestFood.Abs) {
                nearestFood = food.transform.position - transform.position;
            }
        }
        
        // nearest direction to nearestFood
        int nearestDirection = 0;   // 0: right
                                    // 1: up
                                    // 2: left
                                    // 3: down
        if(Vector2.Angle(nearestFood, Vector.right) <= 45) nearestDirection = 0
        else if(Vector2.Angle(nearestFood, Vector.up) <= 45))

        AddVectorObs(nearestFood);

        // 

        // IMPLEMENTATION WITH SIMULATED VISUAL OBSERVATIONS:
        // int numObs = width * height;

        // int[] obs = new int[numObs];

        // for(int i = 0; i < numObs; i++) obs[i] = 0;

        // int idx = (int)(transform.position.y - borderBottom.position.y - 1) * width + (int)(transform.position.x - borderLeft.position.x) - 1;
        // // Debug.Log("player idx: " + idx);
        // obs[idx] = 1;

        // foreach(var tailElement in tail) {

        //     idx = (int)(tailElement.position.y - borderBottom.position.y - 1) * width + (int)(tailElement.position.x - borderLeft.position.x) - 1;
        //     // Debug.Log("tail idx: " + idx);
        //     obs[idx] = 2;

        // }

        // foreach(var foodElement in foods) {

        //     idx = (int)(foodElement.position.y - borderBottom.position.y - 1) * width + (int)(foodElement.position.x - borderLeft.position.x) - 1;
        //     // Debug.Log("food idx: " + idx);
        //     obs[idx] = 3;

        // }

        // foreach(int ob in obs) AddVectorObs(ob);

        // // Debug.Log(string.Join(" ", new List<int>(Obs).ConvertAll(i => i.ToString()).ToArray()));

    }

    void OnTriggerEnter2D(Collider2D collider) {
        // Food?
        if (collider.CompareTag("Food")) {
            // Get longer in next Move call
            ate = true;

            // Remove the Food
            Destroy(collider.gameObject);
            SpawnFood();

        } else // wall or tail!
        {
            // Reset flag
            lost = true;
        }
    }

    void Lost() {
        AddReward(-1f);
        Done();
    }

    public override float[] Heuristic() {

        var act = new float[2];
        act[0] = action.y;
        act[1] = action.x;

        return act;
    }

}