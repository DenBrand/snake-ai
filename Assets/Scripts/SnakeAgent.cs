using UnityEngine;
using MLAgents;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class SnakeAgent : Agent {

    public GameObject Target;
    private List<Transform> tail;
    private Vector2 dir;

    // Start is called before the first frame update
    void Start() {

    }

    void Awake() {
        tail = this.gameObject.GetComponent<Snake>().tail;
        dir = this.gameObject.GetComponent<Snake>().dir;

        RequestDecision();
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
        transform.Translate(dir); // !!!: Mit Snake.cs vereinigen!

        RequestDecision();
    }


    public override float[] Heuristic() {
        var action = new float[2];
        action[0] = 0f;
        action[1] = 0f;

        if(Input.GetKeyDown(KeyCode.UpArrow)) action[0] = -1f;
        if(Input.GetKeyDown(KeyCode.DownArrow)) action[0] = 1f;
        if(Input.GetKeyDown(KeyCode.RightArrow)) action[1] = 1f;
        if(Input.GetKeyDown(KeyCode.LeftArrow)) action[1] = -1f;

        return action;
    }

}
