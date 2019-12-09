/*
InvokeRepeating in etwa line 35 war für die Zeitdiskretisierung verantwortlich. Vereinige die Snake-Klasse mit der SnakeAgent-Klasse und
arbeite mit InvokeRepeating und RequestDecision, um einen zeitdiskreten Entscheidungsprozess zu bewirken.
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class Snake : MonoBehaviour {

    // Current movement direction
    // (by default right) ???: Maybe randomly?
    public Vector2 dir = Vector2.right;
    public List<Transform> tail = new List<Transform>();
    
    // Tail Prefab
    public GameObject tailPrefab;
    public GameObject foodPrefab;
    public Transform borderTop;
    public Transform borderBottom;
    public Transform borderLeft;
    public Transform borderRight;
    private SnakeAgent agent;

    // Did the snake eat something in last time interval?
    bool ate = false;
    bool lost = false; // hit border or tail

    // Start is called before the first frame update
    void Start() {
        // Move the Snake every 200ms
        InvokeRepeating("Move", 0.20f, 0.20f);
        SpawnFood();
    }

    void Awake() {
        agent = this.gameObject.GetComponent<SnakeAgent>();
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
        }

        // Do we have a Tail?
        else if (tail.Count > 0) {
            // Move last Tail Element to where the Head was
            tail.Last().position = gapPosition;

            // Add to front of list, remove from the back
            tail.Insert(0, tail.Last());
            tail.RemoveAt(tail.Count-1);
        }
    }

    void OnTriggerEnter2D(Collider2D collider) {
        // Food?
        if (collider.CompareTag("Food")) {
            // Get longer in next Move call
            ate = true;

            // Remove the Food
            Destroy(collider.gameObject);
            SpawnFood();

        } else { // Collided with Tail or Border
            // Reset flag
            lost = true;
        }
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
            if(true) validPosition = true; // TODO: Define a condition
        }

        // Instantiate the food at (x, y)
        GameObject newFood = (  GameObject)Instantiate(foodPrefab, 
                                new Vector2(x, y), 
                                Quaternion.identity); // default rotation
        newFood.tag = "Food";

        agent.Target = newFood;
    }

    void Lost() {
        // TODO: Maybe show game over screen for 1.5 seconds?

        // Reload Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
