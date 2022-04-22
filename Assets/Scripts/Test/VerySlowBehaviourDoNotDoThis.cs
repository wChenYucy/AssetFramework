using UnityEngine;
using System.Collections.Generic;
using System;

public class VerySlowBehaviourDoNotDoThis : MonoBehaviour {
    [Serializable]
    public class Node {
        public string interestingValue = "value";
        //The field below is what makes the serialization data become huge because
        //it introduces a 'class cycle'.
        public List<Node> children = new List<Node>();
    }
    //this gets serialized
    public Node root = new Node();
    void OnGUI() {
        Display (root);
    }
    void Display(Node node) {
        GUILayout.Label ("Value: ");
        node.interestingValue = GUILayout.TextField(node.interestingValue, GUILayout.Width(200));
        GUILayout.BeginHorizontal ();
        GUILayout.Space (20);
        GUILayout.BeginVertical ();
        foreach (var child in node.children) {
            Display (child);
        }
        if (GUILayout.Button ("Add child")) {
            node.children.Add (new Node ());
        }
        GUILayout.EndVertical ();
        GUILayout.EndHorizontal ();
    }
}
