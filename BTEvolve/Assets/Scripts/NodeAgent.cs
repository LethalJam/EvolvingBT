using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Nodes are based on the ones defined in NodeTypes.cs

// Agent nodes require access to the ShooterAgent-script
public abstract class N_AgentNode : Node
{
    protected ShooterAgent m_agent;
    protected N_AgentNode(ShooterAgent agent)
    {
        m_agent = agent;
    }
}
// Condition nodes.
// Thresholds return Success if the value has reached the given threshold. Else, return failure.
public class N_HealthThreshold : N_AgentNode
{
    int healthThreshold;
    public N_HealthThreshold(ShooterAgent agent, int threshold) : base(agent)
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
    public N_AmmoThreshold(ShooterAgent agent, int threshold) : base(agent)
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
    public N_HasPath(ShooterAgent agent) : base(agent)
    { }

    public override Response Signal()
    {
        return m_agent.HasPath() == true ? Response.Success : Response.Failure;
    }
}
// Check to see if agent was shot recently. If so, return success and reset "takendamage" boolean.
public class N_WasShot : N_AgentNode
{
    public N_WasShot(ShooterAgent agent) : base(agent)
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
    public N_GotoHealthpack(ShooterAgent agent) : base(agent)
    { }
    // Return running if hp is found. If no hp exists, return success.
    public override Response Signal()
    {
        return m_agent.GetHealthPack() == true ? Response.Running : Response.Failure;
    }
}
// Check if there's an enemy in sight and save his current position.
public class N_IsEnemyVisible : N_AgentNode
{
    public N_IsEnemyVisible(ShooterAgent agent) : base(agent)
    { }

    public override Response Signal()
    {
        return m_agent.EnemyVisible() == true ? Response.Success : Response.Failure;
    }
}
// Shoot towards the enemy position (which might be the last seen position if the enemy is not visible). Always returns success.
public class N_ShootAtEnemy : N_AgentNode
{
    public N_ShootAtEnemy(ShooterAgent agent) : base(agent)
    { }
    public override Response Signal()
    {
        m_agent.ShootAt(m_agent.EnemyPosition);
        return Response.Success;
    }
}
// Reload gun. Returns running if still reloading and Success if ammo is full.
public class N_Reload : N_AgentNode
{
    public N_Reload(ShooterAgent agent) : base(agent)
    { }
    public override Response Signal()
    {
        return m_agent.Reload() == true ? Response.Running : Response.Success;
    }
}
// Node that picks random points on the navmesh and patrols between them contionously.
public class N_Patrol : N_AgentNode
{
    public N_Patrol(ShooterAgent agent) : base(agent)
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
    public N_CancelPath(ShooterAgent agent) : base(agent)
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
    public N_TurnAround(ShooterAgent agent) : base(agent)
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
    public N_FollowEnemy(ShooterAgent agent) : base(agent)
    { }
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
    public N_Kite(ShooterAgent agent) : base(agent)
    { }

    public override Response Signal()
    {
        m_agent.Kite();
        return Response.Success;
    }
}