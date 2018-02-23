using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public static class BehaviourSubtrees {

    // Returns true if health is above threshold or if no hp was found.
    // Returns running if in progress of going to hp.
    public static Node Tree_GetHealthpack (ShooterAgent agent, int threshold)
    {
        N_Selection hpTree = new N_Selection();
        N_DecFlip flipHealthCheck = new N_DecFlip(new N_HealthThreshold(agent, threshold));
        N_DecFlip flipGetHp = new N_DecFlip(new N_GotoHealthpack(agent));

        hpTree.AddChild(flipHealthCheck);
        hpTree.AddChild(flipGetHp);

        return hpTree;
    }

    // Returns false if no enemy is in sights, returns false if in sights and agent shot.
    public static Node Tree_ShootAtEnemy (ShooterAgent agent)
    {
        N_Sequence shootTree = new N_Sequence();
        shootTree.AddChild(new N_IsEnemyVisible(agent));
        shootTree.AddChild(new N_ShootAtEnemy(agent));

        return shootTree;
    }

    // Returns false if patroling and no enemy was found,
    // Returns success if patroling and enemy was found.
    public static Node Tree_Patrol (ShooterAgent agent)
    {
        N_Sequence patrolSequence = new N_Sequence();
        N_DecSuccess patrolSuccess = new N_DecSuccess(new N_Patrol(agent));
        patrolSequence.AddChild(patrolSuccess);
        patrolSequence.AddChild(Tree_ShootAtEnemy(agent));
        return patrolSequence;
    }

    // Sequence for moving and then shooting
    // If an enemy is seen, their's a probability
    public static Node Tree_PatrolOrKite (ShooterAgent agent)
    {
        N_ProbabilitySelector kiteOrPatrol = new N_ProbabilitySelector();
        kiteOrPatrol.AddChild(new N_Kite(agent));
        kiteOrPatrol.AddChild(new N_Patrol(agent));
        N_DecSuccess kiteOrPatrolSuccess = new N_DecSuccess(kiteOrPatrol);

        N_Sequence enemyThenKiteOrPatrol = new N_Sequence();
        enemyThenKiteOrPatrol.AddChild(new N_IsEnemyVisible(agent));
        enemyThenKiteOrPatrol.AddChild(kiteOrPatrolSuccess);

        N_DecSuccess patrolSuccess = new N_DecSuccess(new N_Patrol(agent));
        N_Selection enemyOrPatrol = new N_Selection();
        enemyOrPatrol.AddChild(enemyThenKiteOrPatrol);
        enemyOrPatrol.AddChild(patrolSuccess);

        N_Sequence rootMoveThenShoot = new N_Sequence();
        rootMoveThenShoot.AddChild(enemyOrPatrol);
        rootMoveThenShoot.AddChild(Tree_ShootAtEnemy(agent));

        return rootMoveThenShoot;
    }
}


