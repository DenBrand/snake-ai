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

    // Start is called before the first frame update
    void Start() {
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        borderLeft = GameObject.FindGameObjectWithTag("BorderLeft").transform;
        borderRight = GameObject.FindGameObjectWithTag("BorderRight").transform;
        borderTop = GameObject.FindGameObjectWithTag("BorderTop").transform;
        borderBottom = GameObject.FindGameObjectWithTag("BorderBottom").transform;

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

    public override void AgentAction(float[] vectorAction, string textAction) {

        Debug.Log("0: " + vectorAction[0]);
        Debug.Log("1: " + vectorAction[1]);

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
            AddReward(1f);

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

            // DEBUG
            x = 13;
            y= -7;

            // Check if position is valid
            if(true) validPosition = true; // TODO: Define a condition. In snake itself or other food would be invalid.
        }

        // Instantiate the food at (x, y)
        GameObject newFood = (GameObject)Instantiate(   foodPrefab, 
                                                        new Vector2(x, y), 
                                                        Quaternion.identity); // default rotation
        newFood.tag = "Food";
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
        SetReward(-2f);
        Done();
    }

    public override float[] Heuristic() {

        var act = new float[2];
        act[0] = action.y;
        act[1] = action.x;

        return act;
    }

}