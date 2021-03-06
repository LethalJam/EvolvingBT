﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.Serialization;


// ALL NODES ARE DENOTED BY N_
public enum Response
{ Success, Failure, Running}

#region Basic nodes
[Serializable]
public abstract class Node : ICloneable
{
    protected bool m_isSubtree = false;
    public object Clone()
    {
        return MemberwiseClone();
    }

    protected Node parent = null;

    public bool IsSubtree { get { return m_isSubtree; } set { m_isSubtree = value; } }
    public Node Parent { get { return parent; } set { parent = value; } }
    // Send signal to node.
    public abstract Response Signal();
}
// Rootnode has one child and no parents
[Serializable]
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
[Serializable]
public class N_Success : Node
{
    public override Response Signal()
    {
        return Response.Success;
    }
}
[Serializable]
public class N_Failure : Node
{
    public override Response Signal()
    {
        return Response.Failure;
    }
}
#endregion
#region Decorators
[Serializable]
public abstract class N_Decorator : Node
{
    protected Node child;
    public N_Decorator (Node child)
    {
        this.child = child;
    }
    public N_Decorator()
    {

    }

    public Node Child { get { return child; } set { child = value; } }
}
[Serializable]
public class N_DecFlip : N_Decorator
{   
    public N_DecFlip(Node child) : base(child) { }
    public N_DecFlip () { }
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
[Serializable]
public class N_DecSuccess : N_Decorator
{
    public N_DecSuccess (Node child) : base(child) { }
    public N_DecSuccess () { }
    public override Response Signal()
    {
        child.Signal();
        return Response.Success;
    }
}
#endregion
#region Compositions
[Serializable]
public abstract class N_CompositionNode : Node
{
    // Child nodes.
    protected List<Node> children = new List<Node>();

    // Create a new list with the given node as the first element.
    // Then, add the remaining elements from old list.
    public virtual void AddFirst(Node node)
    {
        // Set this node as arguments parent node.
        node.Parent = this;

        List<Node> newChildren = new List<Node> {node};
        foreach (Node n in children)
        {
            newChildren.Add(n);
        }
        // Override old list of children with new one.
        children = newChildren;
    }
    public virtual void AddLast(Node node)
    {
        // Set this node as arguments parent node.
        node.Parent = this;

        children.Add(node);
    }
    public virtual void RemoveChild(Node node)
    {
        children.Remove(node);
    }
    public virtual void ReplaceChild(Node oldNode, Node newNode)
    {
        // Find the indexed position of the old node in the list.
        int index = children.IndexOf(oldNode);

        // If the old node exists in the list, replace that index with the new one instead.
        if (index != -1)
        {
            children[index] = newNode;
            newNode.Parent = this;
            // If the oldnode parent is set to this comp, set it to null instead.
            if (oldNode.Parent == this)
                oldNode.Parent = null;
        }
        else
            Debug.LogError("Replacing node not found in list of children.");
    }

    public List<Node> GetChildren ()
    {
        return children;
    }
    public void SetChildren(List<Node> newChildren)
    {
        children = newChildren;
    }
}
[Serializable]
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
[Serializable]
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
[Serializable]
public class N_ProbabilitySelector : N_CompositionNode
{
    Dictionary<Node, float> probabilityMapping = new Dictionary<Node, float>();
    private Node m_chosenNode;
    private bool m_isNodeChosen = false;

    public override void ReplaceChild(Node oldNode, Node newNode)
    {
        base.ReplaceChild(oldNode, newNode);
        // Remove old and add new while transfering probability weight.
        float oldProb = GetProbabilityWeight(oldNode);
        probabilityMapping.Remove(oldNode);
        probabilityMapping.Add(newNode, oldProb);
    }

    // Modify add and remove to also map a probability value to each childNode.
    // If no probability weight is given, assume one.
    public override void AddFirst(Node node)
    {
        base.AddFirst(node);
        probabilityMapping.Add(node, 1.0f);
    }
    public void AddFirst (Node node, float prob)
    {
        base.AddFirst(node);
        probabilityMapping.Add(node, prob);
    }
    public override void AddLast(Node node)
    {
        base.AddLast(node);
        probabilityMapping.Add(node, 1.0f);
    }
    public void AddLast(Node node, float prob)
    {
        base.AddLast(node);
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
    public void OffsetProbabilityWeight(Node node, float offset)
    {
        if (probabilityMapping.ContainsKey(node))
        {
            probabilityMapping[node] += offset;
            if (probabilityMapping[node] < 0)
                probabilityMapping[node] = 0;
        }
        else
            Debug.LogError("OffsetProbabilityWeight: Trying to acces no-existent node.");

    }
    // Get the weight of the given node if there is one.
    public float GetProbabilityWeight(Node node)
    {
        if (probabilityMapping.ContainsKey(node))
            return probabilityMapping[node];
        else
            throw new AccessViolationException();
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


            float random = UnityEngine.Random.Range(0.0f, 1.0f);
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