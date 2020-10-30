using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grenade_Pickup
{
    /*---------------------------------------------------------------------------------------------------------------------------
     Because our explosion particle system has sub emitters, we cannot rely on Unity's built in particle system stop actions.
     We'll use this script to destroy finished particles after a short delay so the flying debris has some time to finish.
     --------------------------------------------------------------------------------------------------------------------------*/
    public class GrenadeParticleCallback : MonoBehaviour
    {
        /*---1.5 seconds of extra lifetime---*/
        private const float ExtraLifetime = 1.5f;
        private bool _particlesDestroyed;
        private float _extendedLife = 0.0f;

        /*---Called when the particle system attached to the same object tells us that it's finished---*/
        private void OnParticleSystemStopped()
        {
            _particlesDestroyed = true;
        }

        private void Update()
        {
            if (!_particlesDestroyed) return;

            _extendedLife += Time.deltaTime;
            if (_extendedLife > ExtraLifetime) Destroy(gameObject);
        }
    }
}
