using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public static class BehaviourSubtrees {

    // Returns success if health is above threshold or if no hp was found.
    // Returns running if in progress of going to hp.
    public static Node Tree_GetHealthpackIfLow (ShooterAgent agent, int threshold)
    {
        N_Selection hpTree = new N_Selection();
        N_DecFlip flipHealthCheck = new N_DecFlip(new N_HealthThreshold(agent, threshold));
        N_DecFlip flipGetHp = new N_DecFlip(new N_GotoHealthpack(agent));

        hpTree.AddLast(flipHealthCheck);
        hpTree.AddLast(flipGetHp);

        return hpTree;
    }

    // Returns success if ammo full. 
    // Returns failure if not beneath threshold.
    // Returns running if in process of reloading.
    public static Node Tree_ReloadIfLow(ShooterAgent agent, int threshold)
    {
        // Create nodes.
        N_AmmoThreshold ammoCheck = new N_AmmoThreshold(agent, threshold);
        N_Reload reload = new N_Reload(agent);
        N_Sequence reloadSequence = new N_Sequence();

        // Add nodes to sequence. Threshold must be true before attemtping to reload.
        reloadSequence.AddLast(ammoCheck);
        reloadSequence.AddLast(reload);

        return reloadSequence;
    }

    // Returns failure if no enemy is in sights 
    // Returns success if in sights and agent shot.
    public static Node Tree_ShootAtEnemy (ShooterAgent agent)
    {
        N_Sequence shootTree = new N_Sequence();
        shootTree.AddLast(new N_IsEnemyVisible(agent));
        shootTree.AddLast(new N_ShootAtEnemy(agent));

        return shootTree;
    }

    // Returns success if path was found and setting destination.
    // Returns running if in process of moving towards destination.
    public static Node Tree_Patrol (ShooterAgent agent)
    {
        //N_Sequence patrolSequence = new N_Sequence();
        //N_DecSuccess patrolSuccess = new N_DecSuccess(new N_Patrol(agent));
        //patrolSequence.AddLast(patrolSuccess);
        //patrolSequence.AddLast(Tree_ShootAtEnemy(agent));
        return new N_Patrol(agent);
    }

    // Sequence for moving by patroling or kiting.
    // If an enemy is spotted, there's a probability of the agent either patroling
    // or kiting. If no enemy, always patrol.
    // Tree always returns success.
    public static Node Tree_PatrolOrKite (ShooterAgent agent)
    {
        N_ProbabilitySelector kiteOrPatrol = new N_ProbabilitySelector();
        kiteOrPatrol.AddLast(new N_Kite(agent));
        kiteOrPatrol.AddLast(new N_Patrol(agent));
        N_DecSuccess kiteOrPatrolSuccess = new N_DecSuccess(kiteOrPatrol);

        N_Sequence enemyThenKiteOrPatrol = new N_Sequence();
        enemyThenKiteOrPatrol.AddLast(new N_IsEnemyVisible(agent));
        enemyThenKiteOrPatrol.AddLast(kiteOrPatrolSuccess);

        N_DecSuccess patrolSuccess = new N_DecSuccess(new N_Patrol(agent));
        N_Selection enemyOrPatrol = new N_Selection();
        enemyOrPatrol.AddLast(enemyThenKiteOrPatrol);
        enemyOrPatrol.AddLast(patrolSuccess);

        //N_Sequence rootMoveThenShoot = new N_Sequence();
        //rootMoveThenShoot.AddLast(enemyOrPatrol);
        //rootMoveThenShoot.AddLast(Tree_ShootAtEnemy(agent));

        return enemyOrPatrol;
    }

    // Returns success if agent was shot, no enemy was in sight and agent turned around.
    // Returns failure otherwise.
    public static Node Tree_TurnWhenShot (ShooterAgent agent)
    {
        N_Sequence turnSequence = new N_Sequence();
        turnSequence.AddLast(new N_WasShot(agent));
        turnSequence.AddLast(new N_DecFlip(new N_IsEnemyVisible(agent)));
        turnSequence.AddLast(new N_TurnAround(agent));

        return turnSequence;
    }

    // Returns failure if enemy in sights or if agent was lost.
    // Returns success if enemy was found.
    // Returns running if agent is walking towards last seen destination.
    public static Node Tree_FollowEnemy (ShooterAgent agent)
    {
        N_Sequence followSequence = new N_Sequence();
        followSequence.AddLast(new N_DecFlip(new N_IsEnemyVisible(agent)));
        followSequence.AddLast(new N_FollowEnemy(agent));

        return followSequence;
    }

}


