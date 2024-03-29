﻿using UnityEngine;

namespace SinglePlayerMode
{
    public interface IDamageable
    {
        void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection);

        //For the player
        void TakeDamage(float damage);
    }
}