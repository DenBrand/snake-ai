using UnityEngine;
using MLAgents;
using System.Collections.Generic;
using System.Linq;
using MLAgents.Sensor;

public class SnakeAgent : Agent {
    
    public Vector2 dir = Vector2.right;
    private List<Transform> tail = new List<Transform>();
    public GameObject food;
    public GameObject agentPrefab;
    public GameObject tailPrefab;
    public GameObject foodPrefab;
    public float tickRate;
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
        InvokeRepeating("Move", 0f, tickRate);
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

        Destroy(food);

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
        food = (GameObject)Instantiate(foodPrefab, new Vector2(x, y), Quaternion.identity); // default rotation
        food.tag = "Food";
    }

    public override void CollectObservations() {

        // HOPEFULLY SMARTER OBS DESIGN:

        // adjacent obstacles
        bool right = false;
        bool top = false;
        bool left = false;
        bool bottom = false;

        // border next to snake head
        if(Mathf.Abs(transform.position.x - borderRight.position.x) <= 1f)  right = true;
        if(Mathf.Abs(transform.position.y - borderTop.position.y) <= 1f)    top = true;
        if(Mathf.Abs(transform.position.x - borderLeft.position.x) <= 1f)   left = true;
        if(Mathf.Abs(transform.position.y - borderBottom.position.y) <= 1f) bottom = true;

        // tail next to snake head
        foreach(Transform tailElement in tail) {
            if(transform.position + Vector3.right == tailElement.position)  right = true;
            if(transform.position + Vector3.up == tailElement.position)     top = true;
            if(transform.position + Vector3.left == tailElement.position)   left = true;
            if(transform.position + Vector3.down == tailElement.position)   bottom = true;
        }

        //Debug.Log("right: " + right
        //          + " top: " + top
        //          + "\nleft: " + left
        //          + " bottom: " + bottom);

        AddVectorObs(right);
        AddVectorObs(top);
        AddVectorObs(left);
        AddVectorObs(bottom);

        // relative position of food
        Vector2 relativeFoodPosition = food.transform.position;
        // Debug.Log(relativeFoodPosition);
        
        // nearest direction to nearestFood
        if(Vector2.Angle(relativeFoodPosition, Vector2.right) <= 45f) {
            // Debug.Log("RIGHT");
            AddVectorObs(true); // right
            AddVectorObs(false);
            AddVectorObs(false);
            AddVectorObs(false);
        } else if(Vector2.Angle(relativeFoodPosition, Vector2.up) <= 45f) {
            // Debug.Log("UP");
            AddVectorObs(false);
            AddVectorObs(true); // up
            AddVectorObs(false);
            AddVectorObs(false);
        } else if(Vector2.Angle(relativeFoodPosition, Vector2.left) <= 45f) {
            // Debug.Log("LEFT");
            AddVectorObs(false);
            AddVectorObs(false);
            AddVectorObs(true); // left
            AddVectorObs(false);
        } else {
            // Debug.Log("DOWN");
            AddVectorObs(false);
            AddVectorObs(false);
            AddVectorObs(false);
            AddVectorObs(true); // down
        }

        // OBS DESIGN WITH SIMULATED VISUAL OBSERVATIONS:
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

            // Remove the Food and respawn new one
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