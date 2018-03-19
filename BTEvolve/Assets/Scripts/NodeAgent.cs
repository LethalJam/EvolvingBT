using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

// Nodes are based on the ones defined in NodeTypes.cs

// Agent nodes require access to the ShooterAgent-script
[Serializable]
public abstract class N_AgentNode : Node
{
    public enum AgentType
    {
        agent0, agent1
    }
    protected AgentType agentType;

    protected N_AgentNode(AgentType type)
    {
        agentType = type;
    }
    public void SetAgent(AgentType type)
    {
        agentType = type;
    }
}
#region Condition nodes
// Base class for identifying condition nodes.
[Serializable]
public abstract class N_Condition : N_AgentNode
{
    protected N_Condition (AgentType agent) : base(agent) { }
}

[Serializable]
public abstract class N_Threshold : N_Condition
{
    protected int threshold;
    // Min and max possible thresholds. Adjust depending on subclass in constructor!
    protected int minthresh = 0, maxthresh = 0;

    public N_Threshold(AgentType agent, int threshold) : base(agent)
    {
        SetThreshold(threshold);
    }
    public virtual void SetThreshold(int threshold)
    {
        // Check for min and max thresholds befire setting it.
        if (threshold > maxthresh)
            this.threshold = maxthresh;
        else if (threshold < minthresh)
            this.threshold = minthresh;
        else
            this.threshold = threshold;
    }

    public int Threshold { get { return threshold; } }
}

// Thresholds return Success if the value has reached the given threshold. Else, return failure.
[Serializable]
public class N_HealthThreshold : N_Threshold
{

    public N_HealthThreshold(AgentType agent, int threshold) : base(agent, threshold)
    {
        minthresh = 1;
        maxthresh = 99;
    }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.Health <= threshold ? Response.Success : Response.Failure;
    }
}
[Serializable]
public class N_AmmoThreshold : N_Threshold
{
    public N_AmmoThreshold(AgentType agent, int threshold) : base(agent, threshold)
    {
        minthresh = 0;
        maxthresh = 19;
    }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.Bullets <= threshold ? Response.Success : Response.Failure;
    }
}
[Serializable]
public class N_HasPath : N_Condition
{
    public N_HasPath(AgentType agent) : base(agent)
    { }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.HasPath() == true ? Response.Success : Response.Failure;
    }
}
// Check to see if agent was shot recently. If so, return success and reset "takendamage" boolean.
[Serializable]
public class N_WasShot : N_Condition
{
    public N_WasShot(AgentType agent) : base(agent)
    {
    }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.HasTakenDamage() == true ? Response.Success : Response.Failure;
    }
}
#endregion

#region ActionNodes
// Look up to and start walking towards nearest healthpack
[Serializable]
public class N_GotoHealthpack : N_AgentNode
{
    public N_GotoHealthpack(AgentType agent) : base(agent)
    { }
    // Return running if hp is found. If no hp exists, return success.
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.GetHealthPack() == true ? Response.Running : Response.Failure;
    }
}
// Check if there's an enemy in sight and save his current position.
[Serializable]
public class N_IsEnemyVisible : N_AgentNode
{
    public N_IsEnemyVisible(AgentType agent) : base(agent)
    { }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.EnemyVisible() == true ? Response.Success : Response.Failure;
    }
}
// Shoot towards the enemy position (which might be the last seen position if the enemy is not visible). Always returns success.
[Serializable]
public class N_ShootAtEnemy : N_AgentNode
{
    public N_ShootAtEnemy(AgentType agent) : base(agent)
    { }
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        agent.ShootAt(agent.EnemyPosition);
        return Response.Success;
    }
}
// Reload gun. Returns running if still reloading and Success if ammo is full.
[Serializable]
public class N_Reload : N_AgentNode
{
    public N_Reload(AgentType agent) : base(agent)
    { }
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        return agent.Reload() == true ? Response.Running : Response.Success;
    }
}
// Node that picks random points on the navmesh and patrols between them contionously.
[Serializable]
public class N_Patrol : N_AgentNode
{
    public N_Patrol(AgentType agent) : base(agent)
    { }
    // Set random destination and walk towards it.
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        // If no current path, randomize new one and walk towards it.
        if (!agent.HasPath() || agent.StateOfAgent == ShooterAgent.AgentState.kiting)
        {
            agent.StateOfAgent = ShooterAgent.AgentState.patroling;
            agent.SetRandomDestination();
            agent.WalkTowards(agent.WalkingDestination);
            return Response.Success;
        }
        else
            return Response.Running;
    }
}
// Cancels path towards current destination.
[Serializable]
public class N_CancelPath : N_AgentNode
{
    public N_CancelPath(AgentType agent) : base(agent)
    { }
    // Cancels current agent path and always returns success.
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        agent.CancelPath();
        return Response.Success;
    }
}
// Turns the agent 180 degrees.
[Serializable]
public class N_TurnAround : N_AgentNode
{
    public N_TurnAround(AgentType agent) : base(agent)
    {
    }
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        agent.TurnAround();
        return Response.Success;
    }
}
// Follow the enemy position if not lost.
[Serializable]
public class N_FollowEnemy : N_AgentNode
{
    public N_FollowEnemy(AgentType agent) : base(agent)
    { }
    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        // If not lost and has no path, walk towards enemy position.
        if (!agent.EnemyLost && !agent.HasPath())
        {
            agent.CancelPath();
            agent.WalkTowards(agent.EnemyPosition);
        }
        // If the agent reached the enemypos yet there's no enemy in sight, return failure.
        else if (agent.AtEnemyPosition() && !agent.EnemyVisible())
        {
            agent.EnemyLost = true;
            return Response.Failure;
        }
        // When enemy is found, cancel following and return success.
        else if (agent.EnemyVisible())
        {
            agent.CancelPath();
            return Response.Success;
        }

        return Response.Running;
    }
}
// Kite away backwards relative to forward facing position.
[Serializable]
public class N_Kite : N_AgentNode
{
    public N_Kite(AgentType agent) : base(agent)
    { }

    public override Response Signal()
    {
        ShooterAgent agent = StaticMethods.GetInstance().GetAgentOfType(agentType);
        //Debug.Log("Kiting!");
        return agent.Kite() == true ? Response.Success : Response.Running;
    }
}
#endregion