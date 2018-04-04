using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TreeOperations {

	public static List<Node> RetrieveNodesOfType (N_Root tree, Type nodeType)
    {
        Queue<Node> itQ = new Queue<Node>();
        List<Node> found = new List<Node>();
        itQ.Enqueue(tree.Child);

        // Do a breadth-first search to retrieve all nodes of the given type.
        while (itQ.Count > 0)
        {
            Node current = itQ.Dequeue();
            // If the current node ís the same as the search type
            // or a subclass of it, add it to found.
            if (current.GetType().IsSubclassOf(nodeType)
                || current.GetType() == nodeType)
                found.Add(current);

            // If node is a composition or decorator, enqueue its child(ren).
            if (current.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode comp = current as N_CompositionNode;
                foreach (Node n in comp.GetChildren())
                {
                    itQ.Enqueue(n);
                }
            }
            else if (current.GetType().IsSubclassOf(typeof(N_Decorator)))
            {
                N_Decorator dec = current as N_Decorator;
                itQ.Enqueue(dec.Child);
            }
        }

        // Lastly, return the found children.
        return found;
    }

    // Return a list of all nodes that act as roots for a randomized subtree.
    public static List<Node> RetrieveSubtreeNodes (N_Root tree)
    {
        Queue<Node> itQ = new Queue<Node>();
        List<Node> found = new List<Node>();
        itQ.Enqueue(tree.Child);

        while (itQ.Count > 0)
        {
            Node current = itQ.Dequeue();

            // If current node is flagged as subtree, add it to found
            if (current.IsSubtree)
                found.Add(current);

            // If node is a composition or decorator, enqueue its child(ren).
            if (current.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode comp = current as N_CompositionNode;
                foreach (Node n in comp.GetChildren())
                {
                    itQ.Enqueue(n);
                }
            }
            else if (current.GetType().IsSubclassOf(typeof(N_Decorator)))
            {
                N_Decorator dec = current as N_Decorator;
                itQ.Enqueue(dec.Child);
            }
        }

        // Return all nodes found with the subtree flag
        return found;
    }

}
