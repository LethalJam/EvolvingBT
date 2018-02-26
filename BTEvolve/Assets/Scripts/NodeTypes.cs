using System.Collections.Generic;
using UnityEngine;
using System.Linq;


// ALL NODES ARE DENOTED BY N_
public enum Response
{ Success, Failure, Running}

#region Basic nodes
public abstract class Node {
    // Send signal to node.
    public abstract Response Signal();
}
// Rootnode has one child and no parents
public class N_Root
{
    private Node child;
    // Set and get child
    public Node Child { get { return child; } set { child = value; } }

    // Execute node and thereby the tree connected to it.
    public void Execute()
    {
        child.Signal();
    }
}
// Nodes for testing purposes. Always return success and failure.
public class N_Success : Node
{
    public override Response Signal()
    {
        return Response.Success;
    }
}
public class N_Failure : Node
{
    public override Response Signal()
    {
        return Response.Failure;
    }
}
#endregion
#region Decorators
public abstract class N_Decorator : Node
{
    protected Node child;
    public N_Decorator (Node child)
    {
        this.child = child;
    }

    public Node Child { get { return child; } set { child = value; } }
}
public class N_DecFlip : N_Decorator
{   
    public N_DecFlip(Node child) : base(child) { }
    // Flip success and failure to the opposite. Return response as usual.
    public override Response Signal()
    {
        if (child != null)
        {
            Response childResponse = child.Signal();
            if (childResponse == Response.Success)
                return Response.Failure;
            else if (childResponse == Response.Failure)
                return Response.Success;
        }
        else
            Debug.LogError("Decorator node missing child.");

        return Response.Failure;
    }
}
public class N_DecSuccess : N_Decorator
{
    public N_DecSuccess (Node child) : base(child) { }
    public override Response Signal()
    {
        child.Signal();
        return Response.Success;
    }
}
#endregion
#region Compositions
public abstract class N_CompositionNode : Node
{
    // Child nodes.
    protected List<Node> children = new List<Node>();

    public virtual void AddChild(Node node)
    {
        children.Add(node);
    }
    public virtual void RemoveChild(Node node)
    {
        children.Remove(node);
    }
}
public class N_Sequence : N_CompositionNode
{
    public override Response Signal()
    {
        // Execute all nodes in sequence and return child response if one fails or is running.
        foreach (Node n in children)
        {
            // Signal child
            Response childResponse = n.Signal();
            if (childResponse == Response.Failure
                || childResponse == Response.Running)
            {
                return childResponse;
            }
        }
        // If all children succeeded, return success.
        return Response.Success;
    }
}
public class N_Selection : N_CompositionNode
{
    public override Response Signal()
    {
        // Execute children in sequence until one that succeeds or is running is found.
        // then return it
        foreach (Node n in children)
        {
            // Signal child
            Response childResponse = n.Signal();
            if (childResponse == Response.Success
                || childResponse == Response.Running)
            {
                return childResponse;
            }
        }
        // If no child succeeded, return failure
        return Response.Failure;
    }
}
public class N_ProbabilitySelector : N_CompositionNode
{
    Dictionary<Node, float> probabilityMapping = new Dictionary<Node, float>();
    private Node m_chosenNode;
    private bool m_isNodeChosen = false;

    // Modify add and remove to also map a probability value to each childNode.
    public override void AddChild(Node node)
    {
        base.AddChild(node);
        probabilityMapping.Add(node, 1.0f);
    }
    public void AddChild(Node node, float prob)
    {
        base.AddChild(node);
        probabilityMapping.Add(node, prob);
    }
    public override void RemoveChild(Node node)
    {
        base.RemoveChild(node);
        probabilityMapping.Remove(node);
    }
    // Set the probability value of the given node or index.
    public void SetProbabilityWeight(Node node, float prob)
    {
        if (probabilityMapping.ContainsKey(node))
            probabilityMapping[node] = prob;
        else
            Debug.LogError("SetProbabilityWeight: Trying to access non-existent node.");
    }
    public void SetProbabilityWeight(int index, float prob)
    {
        if (index < probabilityMapping.Count)
            probabilityMapping[probabilityMapping.ElementAt(index).Key] = prob;
        else
            Debug.LogError("SetProbabilityWeight: Trying to access non-existent node.");
    }
    
    public override Response Signal()
    {
        if (!m_isNodeChosen && probabilityMapping.Count != 0)
        {
            // Actual collection of relative probability between nodes.
            Dictionary<Node, float> prob_actual = new Dictionary<Node, float>();
            float weightTotal = 0.0f;
            // Sum up the weight of each node.
            foreach (var pair in probabilityMapping)
            {
                weightTotal += pair.Value;
            }
            // Calculate the relative probability for each node.
            float probSum = 0.0f;
            foreach (var pair in probabilityMapping)
            {
                float actualProbability = probSum + (pair.Value / weightTotal);
                prob_actual.Add(pair.Key, actualProbability);
                probSum += (pair.Value / weightTotal);
            }


            float random = Random.Range(0.0f, 1.0f);
            for (int i = 0; i < prob_actual.Count; i++)
            {
                var current = prob_actual.ElementAt(i);
                // If this element is not the last, continue with regular comparison of values.
                if (i == 0)
                {
                    if (random < current.Value)
                    {
                        m_isNodeChosen = true;
                        m_chosenNode = current.Key;
                        //Debug.Log("Chose node " + i + " with prob: " + current.Value);
                        return m_chosenNode.Signal();
                    }
                }
                // If none of the first elements were chosen, pick the last one.
                else
                {
                    var prev = prob_actual.ElementAt(i - 1);
                    if (random < current.Value && random > prev.Value)
                    {
                        m_isNodeChosen = true;
                        m_chosenNode = current.Key;
                        //Debug.Log("Chose node " + i + " with prob: " + current.Value);
                        return m_chosenNode.Signal();
                    }
                }
            }
        }
        else if (m_isNodeChosen && probabilityMapping.Count != 0)
        {
            // Keep signaling same node if its running.
            // If it succeeds, open up for the possibility of other nodes being chosen.
            Response childResponse = m_chosenNode.Signal();
            if (childResponse == Response.Success)
            {
                m_isNodeChosen = false;
                //Debug.Log("Rechoose node.");
            }

            return childResponse;
        }

        // If the composition has no children, return failure.
        return Response.Failure;
    }
}
#endregion