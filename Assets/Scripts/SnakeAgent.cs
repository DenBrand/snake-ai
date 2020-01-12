using UnityEngine;
using MLAgents;
using System.Collections.Generic;
using System.Linq;

public class SnakeAgent : Agent {
    
    private Vector3 dir = Vector3.right;
    private Vector3 lastDir = Vector3.right;
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
    private float action = 1f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    int width;
    int height;
    private bool foodIsRight;
    private bool foodIsFront;
    private bool foodIsLeft;
    private bool foodIsBehind;
    private int score;

    // Start is called before the first frame update
    void Start() {
        score = 0;
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        borderLeft = GameObject.FindGameObjectWithTag("BorderLeft").transform;
        borderRight = GameObject.FindGameObjectWithTag("BorderRight").transform;
        borderTop = GameObject.FindGameObjectWithTag("BorderTop").transform;
        borderBottom = GameObject.FindGameObjectWithTag("BorderBottom").transform;
        width = (int)Mathf.Abs(borderRight.position.x - borderLeft.position.x) - 1;
        height = (int)Mathf.Abs(borderTop.position.y - borderBottom.position.y) - 1;

        // Move the Snake every 200ms
        InvokeRepeating("RequestDecision", 0f, 1f/tickRate);
    }

    void FixedUpdate() {
        if(Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow)) action = 2f;
        else if(Input.GetKeyDown(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow)) action = 0f;
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

        // Move in a new direction?
        if(Mathf.FloorToInt(vectorAction[0]) == 0) dir = Quaternion.Euler(0f, 0f, -90f) * dir; // turn right
        else if(Mathf.FloorToInt(vectorAction[0]) == 2) dir = Quaternion.Euler(0f, 0f, 90f) * dir; // turn left

        // Reward, if direction is towards food
        if(((foodIsRight && Mathf.RoundToInt(Vector3.Angle(dir, Quaternion.Euler(0f, 0f, -90f) * lastDir)) == 0f) ||
                (foodIsLeft && Mathf.RoundToInt(Vector3.Angle(dir, Quaternion.Euler(0f, 0f, 90f) * lastDir)) == 0f) ||
                (foodIsFront && Mathf.RoundToInt(Vector3.Angle(dir, lastDir)) == 0f))) {

            //Debug.Log("+1.0)");
            AddReward(1f);
        }

        // Punish gently but strictly, if agent goes away from food
        if(((foodIsRight && Mathf.RoundToInt(Vector3.Angle(dir, Quaternion.Euler(0f, 0f, 90f) * lastDir)) == 0f) ||
                (foodIsLeft && Mathf.RoundToInt(Vector3.Angle(dir, Quaternion.Euler(0f, 0f, -90f) * lastDir)) == 0f) ||
                (foodIsBehind && Mathf.RoundToInt(Vector3.Angle(dir, lastDir)) == 0f))) {

            //Debug.Log("-0.5)");
            AddReward(-0.5f);
        }

        // Save current position (gap will be here)
        Vector2 gapPosition = transform.position;

        // Move head into new direction (now there is a gap)
        transform.Translate(dir);
        lastDir = dir;


        // Evaluate consequences if decision
        if(ate) {
            score++;
            //Debug.Log("+5.0");
            AddReward(5f);

            // Load Prefab into the world
            GameObject newTailElement = (GameObject)Instantiate(tailPrefab,
                                                    gapPosition,
                                                    Quaternion.identity);
            newTailElement.tag = "Tail";

            // Keep track of it in our tail list
            tail.Insert(0, newTailElement.transform);

            // Reset the flag
            ate = false;
        }

        if(lost) {
            // Punish brutally, if agents dares to die
            //Debug.Log("!-1.0");
            AddReward(-5f);
            Done();
        }
        else if(tail.Count > 0) {
            // Move last Tail Element to where the Head was
            tail.Last().position = gapPosition;

            // Add to front of list, remove from the back
            tail.Insert(0, tail.Last());
            tail.RemoveAt(tail.Count - 1);
        }

        // Reset action flag
        action = 1f;
    }

    void SpawnFood() {

        // Find valid food positon
        int x = 0, y = 0;
        bool validPosition = false;
        int tries = 5;
        while (!validPosition) {

            // x position between left & right border
            x = (int)Random.Range(borderLeft.position.x, borderRight.position.x);
            // y postition in top & bottom border
            y = (int)Random.Range(borderBottom.position.y, borderTop.position.y);

            // Check if position is valid <=> no other entity on position
            // Neither the snake head
            if(Mathf.RoundToInt(transform.position.x) != x || Mathf.RoundToInt(transform.position.y) != y) {

                // Nor a tail element
                foreach(var tailElement in tail) {
                    if(Mathf.RoundToInt(tailElement.position.x) != x || Mathf.RoundToInt(tailElement.position.y) != y) validPosition = true;
                }
            }
            tries--;
            if(tries == 0) validPosition = true;
        }

        // Instantiate the food at (x, y)
        food = (GameObject)Instantiate(foodPrefab, new Vector2(x, y), Quaternion.identity); // default rotation
        food.tag = "Food";
    }

    public override void CollectObservations() {

        // HOPEFULLY SMARTER OBS DESIGN:

        // adjacent obstacles
        bool right = false;
        bool front = false;
        bool left = false;

        // check if a border is beside of snake (in respect to it's moving direction)
        {
            // right beside the snake
            if(Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir).y) == Mathf.RoundToInt(borderTop.position.y) ||     // top
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir).x) == Mathf.RoundToInt(borderLeft.position.x) ||    // left
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir).y) == Mathf.RoundToInt(borderBottom.position.y) ||  // bottom
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir).x) == Mathf.RoundToInt(borderRight.position.x))     // right
                right = true; // one of the four borders is on the right of the snake head

            // left beside the snake
            if(Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir).y) == Mathf.RoundToInt(borderTop.position.y) ||     // top
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir).x) == Mathf.RoundToInt(borderLeft.position.x) ||    // left
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir).y) == Mathf.RoundToInt(borderBottom.position.y) ||  // bottom
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir).x) == Mathf.RoundToInt(borderRight.position.x))     // right
                left = true; // one of the four borders is on the left of the snake head

            // in front of the snake
            if(Mathf.RoundToInt((transform.position + lastDir).y) == Mathf.RoundToInt(borderTop.position.y) ||     // top
               Mathf.RoundToInt((transform.position + lastDir).x) == Mathf.RoundToInt(borderLeft.position.x) ||    // left
               Mathf.RoundToInt((transform.position + lastDir).y) == Mathf.RoundToInt(borderBottom.position.y) ||  // bottom
               Mathf.RoundToInt((transform.position + lastDir).x) == Mathf.RoundToInt(borderRight.position.x))     // right
                front = true; // one of the four borders is in front of the snake head
        }

        // check if a tail element is beside of snake (in respect to it's moving direction)
        foreach(Transform tailElement in tail) {
            // right beside the snake
            if(Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir - tailElement.position).x) == 0f &&
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, -90f) * lastDir - tailElement.position).y) == 0f)
                right = true;

            // left beside the snake
            if(Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir - tailElement.position).x) == 0f &&
               Mathf.RoundToInt((transform.position + Quaternion.Euler(0f, 0f, 90f) * lastDir - tailElement.position).y) == 0f)
                left = true;

            // in front of the snake
            if(Mathf.RoundToInt((transform.position + lastDir - tailElement.position).x) == 0f &&
               Mathf.RoundToInt((transform.position + lastDir - tailElement.position).y) == 0f)
                front = true;
        }

        AddVectorObs(right);
        AddVectorObs(front);
        AddVectorObs(left);

        // relative position of food
        Vector2 relativeFoodPosition = food.transform.position - transform.position;

        // find nearest direction to food
        foodIsRight = false;
        foodIsFront = false;
        foodIsLeft = false;
        foodIsBehind = false;

        if      (Vector2.Angle(relativeFoodPosition, lastDir) <= 45f)                                   foodIsFront = true;
        else if (Vector2.Angle(relativeFoodPosition, Quaternion.Euler(0f, 0f, -90f) * lastDir) <= 45f)  foodIsRight = true;    
        else if (Vector2.Angle(relativeFoodPosition, Quaternion.Euler(0f, 0f, 90f) * lastDir) <= 45f)   foodIsLeft = true;    
        else                                                                                            foodIsBehind = true;

        // reach in Obs
        AddVectorObs(foodIsRight);
        AddVectorObs(foodIsFront);
        AddVectorObs(foodIsLeft);
        AddVectorObs(foodIsBehind);

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

    public override float[] Heuristic() {
        var act = new float[2];
        act[0] = action;

        return act;
    }

}