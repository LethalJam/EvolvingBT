using System.Collections.Generic;
using UnityEngine;
using System.Linq;


// ALL NODES ARE DENOTED BY N_

public enum Response
{ Success, Failure, Running}

// Node baseclass
public abstract class Node {
    // Send signal to node.
    public abstract Response Signal();
}

// Agent nodes require access to the ShooterAgent-script
public abstract class N_AgentNode : Node
{
    protected ShooterAgent m_agent;
    protected N_AgentNode (ShooterAgent agent)
    {
        m_agent = agent;
    }
}

// Condition nodes.
// Thresholds return Success if the value has reached the given threshold. Else, return failure.
public class N_HealthThreshold : N_AgentNode
{
    int healthThreshold;
    public N_HealthThreshold (ShooterAgent agent, int threshold) : base(agent)
    {
        healthThreshold = threshold;
    }
    public void SetHealthThreshold(int threshold)
    {
        healthThreshold = threshold;
    }
    public override Response Signal()
    {
        return m_agent.Health <= healthThreshold ? Response.Success : Response.Failure;
    }
}
public class N_AmmoThreshold : N_AgentNode
{
    int ammoThreshold;
    public N_AmmoThreshold(ShooterAgent agent, int threshold) : base (agent)
    {
        ammoThreshold = threshold;
    }
    public void SetAmmoThreshold(int threshold)
    {
        ammoThreshold = threshold;
    }
    public override Response Signal()
    {
        return m_agent.Bullets <= ammoThreshold ? Response.Success : Response.Failure;
    }
}
public class N_HasPath : N_AgentNode
{
    public N_HasPath (ShooterAgent agent) : base (agent)
    { }

    public override Response Signal()
    {
        return m_agent.HasPath() == true ? Response.Success : Response.Failure;
    }
}
// Check to see if agent was shot recently. If so, return success and reset "takendamage" boolean.
public class N_WasShot : N_AgentNode
{
    public N_WasShot (ShooterAgent agent) : base(agent)
    {
    }

    public override Response Signal()
    {
        return m_agent.HasTakenDamage() == true ? Response.Success : Response.Failure;
    }
}

// Action nodes.
// Look up to and start walking towards nearest healthpack
public class N_GotoHealthpack : N_AgentNode
{
    public N_GotoHealthpack(ShooterAgent agent) : base (agent)
    { }
    public override Response Signal()
    {
        m_agent.GetHealthPack();

        return m_agent.HasFoundHealthpack() == true ? Response.Success : Response.Failure;
    }
}
// Check if there's an enemy in sight and save his current position.
public class N_IsEnemyVisible : N_AgentNode
{
    public N_IsEnemyVisible (ShooterAgent agent) : base (agent)
    { }

    public override Response Signal()
    {
        return m_agent.EnemyVisible() == true ? Response.Success : Response.Failure;
    }
}
// Shoot towards the enemy position (which might be the last seen position if the enemy is not visible). Always returns success.
public class N_ShootAtEnemy : N_AgentNode
{
    public N_ShootAtEnemy (ShooterAgent agent) : base (agent)
    {}
    public override Response Signal()
    {
        m_agent.ShootAt(m_agent.EnemyPosition);
        return Response.Success;
    }
}
// Reload gun. Returns running if still reloading and Success if ammo is full.
public class N_Reload : N_AgentNode
{
    public N_Reload (ShooterAgent agent) : base (agent)
    { }
    public override Response Signal()
    {
        return m_agent.Reload() == true ? Response.Running : Response.Success;
    }
}
// Node that picks random points on the navmesh and patrols between them contionously.
public class N_Patrol : N_AgentNode
{
    public N_Patrol (ShooterAgent agent) : base(agent)
    { }
    // Set random destination and walk towards it.
    public override Response Signal()
    {
        // If no current path, randomize new one and walk towards it.
        if (!m_agent.HasPath())
        {
            m_agent.SetRandomDestination();
            m_agent.WalkTowards(m_agent.WalkingDestination);
            return Response.Success;
        }
        else
            return Response.Running;
    }
}
// Cancels path towards current destination.
public class N_CancelPath : N_AgentNode
{
    public N_CancelPath (ShooterAgent agent) : base(agent)
    { }
    // Cancels current agent path and always returns success.
    public override Response Signal()
    {
        m_agent.CancelPath();
        return Response.Success;
    }
}
// Turns the agent 180 degrees.
public class N_TurnAround : N_AgentNode
{
    public N_TurnAround (ShooterAgent agent) : base(agent)
    {
    }
    public override Response Signal()
    {
        m_agent.TurnAround();
        return Response.Success;
    }
}
// Follow the enemy position if not lost.
public class N_FollowEnemy : N_AgentNode
{
    public N_FollowEnemy (ShooterAgent agent) : base(agent)
    {}
    public override Response Signal()
    {
        // If not lost and has no path, walk towards enemy position
        if (!m_agent.EnemyLost && !m_agent.HasPath())
            m_agent.WalkTowards(m_agent.EnemyPosition);
        else if (m_agent.AtEnemyPosition() && !m_agent.EnemyVisible())
        {
            m_agent.EnemyLost = true;
            return Response.Failure;
        }

        return Response.Running;
    }
}
// Kite away backwards relative to forward facing position.
public class N_Kite : N_AgentNode
{
    public N_Kite (ShooterAgent agent) : base(agent)
    { }

    public override Response Signal()
    {
        m_agent.Kite();
        return Response.Success;
    }
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

/// <summary>
/// Following nodes are compition nodes, signaling an arbitrary amount of child nodes.
/// </summary>
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
                && childResponse == Response.Running)
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
                && childResponse == Response.Running)
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
                m_isNodeChosen = false;
            return childResponse;
        }

        // If the composition has no children, return failure.
        return Response.Failure;
    }
}