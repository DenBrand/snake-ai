using UnityEngine;
using MLAgents;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

public class SnakeAgent : Agent {
    
    public Vector2 dir = Vector2.right;
    public List<Transform> tail = new List<Transform>();
    public GameObject tailPrefab;
    public GameObject foodPrefab;
    public Transform borderTop;
    public Transform borderBottom;
    public Transform borderLeft;
    public Transform borderRight;
    bool ate = false; // ate something
    bool lost = false; // hit border or tail
    private GameObject Target;

    // Start is called before the first frame update
    void Start() {
        SpawnFood();
        // Move the Snake every 200ms
        InvokeRepeating("Move", 0.20f, 0.20f);
    }

    public override void AgentReset() {
        // ???: Need to be implemented?
    }

    public override void AgentAction(float[] vectorAction, string textAction) {
        // ???: Wie sieht vectorAction in unserem diskreten Beispiel hier aus?
        // Move in a new Direction?

        if (vectorAction[1] == 1) dir = Vector2.right;
        else if (vectorAction[0] == 1) dir = Vector2.down;
        else if (vectorAction[1] == -1) dir = Vector2.left;
        else if (vectorAction[0] == -1) dir = Vector2.up;

        // Move head into new direction (now there is a gap)
        transform.Translate(dir);
    }

    void Move() {
        // Save current position (gap will be here)
        Vector2 gapPosition = transform.position;

        if (ate) {
            // Load Prefab into the world
            GameObject newTailElement = (GameObject)Instantiate( tailPrefab,
                                                    gapPosition,
                                                    Quaternion.identity);
            newTailElement.tag = "tail";

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
        action.x = 0f;
        action.y = 0f;
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
        GameObject newFood = (  GameObject)Instantiate(foodPrefab, 
                                new Vector2(x, y), 
                                Quaternion.identity); // default rotation
        newFood.tag = "Food";

        Target = newFood;
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
        // ???: Maybe show game over screen for ~1.5 seconds?

        // Reload Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public override float[] Heuristic() {
        var act = new float[2];
        act[0] = action.x;
        act[1] = action.y;
        return act;
    }

}