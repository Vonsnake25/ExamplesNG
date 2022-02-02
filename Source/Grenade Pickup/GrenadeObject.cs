using BallisticUnityTools.AssetApi;
using BallisticUnityTools.Triggers;
using NgAudio;
using NgData;
using NgData.Constants;
using NgPickups;
using NgPickups.Physical;
using NgPickups.Physical.Projectiles;
using NgShips;
using NgStats;
using NgTrackData;
using UnityEngine;

namespace Grenade_Pickup
{
    /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     Our grenade object inherits from the projectile class.
     This does a lot of the heavy lifting for us and we just need to define the physics forces and collision response behaviours.

     Just as a note, if you're making a projectile that you want to follow the track, you can get the information you need from this in OnFixedUpdate by using:
     
     TrackResult result = GetTrackRotation(offsetFromPosition, maxCheckDistance, positionOfProjectile * CurrentSection.InverseTransformPoint(transform.position).x * -0.1f);

     NOTE:
        When you use DestroyProjectile there is an optional boolean parameter to play the generic projectile explode sound. Include false as a second parameter to disable this 


     ===============================================================================================================================================================================
     SPECIAL EFFECTS:
     ===============================================================================================================================================================================

     The grenade doesn't make use of any special effects, but if you want to achieve what projectiles like the rockets do:

     Vortex Trails:
     -------------
     - Create Trail Renderers in your prefab and add them as references in your custom prefab
     - When spawning your projectile, attach ProjectileMeshTrail to your trail renderer objects via gameObject.AddComponent<ProjectileMeshTrail>()
     - Assign Trail in ProjectileMeshTraill to the Trail Renderer. Set FadeOutOnDeath to true and then set FadeOutTime to some time that you want them fade out over.
     - Once you have attached ProjectileMeshTrail to all of your Trail Renderers, create an array of DestroyableProjectileTrail and then pump in your ProjectileMeshTrail references
     - Assign the DestroyableProjectileTrail array to the Trails array in the projectile before you call ConfigureProjectile()

     Flame:
     ------
     - Make a flame effect mesh like the 3d engine trail for the player. Make sure you have UV scrolling (use the BallisticNG Standard shader)
     - That's it, there's nothing else to this :)
     -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
    public class GrenadeObject : Projectile
    {
        /*---The travel speed of the grenade---*/
        public static float ConfigSpeed = 25.0f;

        /*---The gravity force of the grenade---*/
        public static float ConfigGravity = 35.0f;

        /*---How hard grenades will bounce off of surfaces---*/
        public static float ConfigBounceForce = 4.0f;

        /*---How long the grenades will live for until they despawn---*/
        public static float ConfigLifetime = 8.0f;

        /*---How many bounces the grenades can make before they explode---*/
        public static int ConfigMaxBounces = 3;

        /*---The settings that will be used for the impact when this grenade makes an impact with a ship. This uses the values for rockets---*/
        private static readonly WeaponImpactSettings ConfigImpactSettings = new WeaponImpactSettings
        {
            Damage = 12.0f,                 // the damage stat of the projectile
            DealRawDamage = false,          // set to true for the damage to be the same across all ships, ignoring their shielding stats

            EngineThrustReduce = 0.4f,      // how much to reduce the ships engine thrust by
            EnginePowerReduce = 0.4f,       // how much to reduce the ships engine power by
            EngineAccelReduce = 0.4f,       // how much to reduce the ships engine acceleration by
            VelocityLoss = 0.3f,            // how much raw velocity the ship will loose on impact

            SlideForce = 15.0f,             // how much this projectile will cause impacted ai ships to slide
            SlideTime = 1.5f,               // how long the ai will slide for

            IgnoreHoldingShield = false,    // set to true for auto shield deployment to be ignored
            IgnoreShield = false            // set to true for an active shield to be ignored entirely
        };

        /*---Creates a new grenade object---*/
        public static GrenadeObject CreateNew(Vector3 position, Quaternion rotation, ShipController owner)
        {
            // create object
            CustomPrefab grenadePrefab = Instantiate(GrenadeMod.GrenadePrefab);
            GrenadeObject grenade = grenadePrefab.gameObject.AddComponent<GrenadeObject>();
            grenade.Owner = owner;

            // setup the grenades transform
            grenade.transform.position = position;
            grenade.transform.rotation = rotation;

            // get the components from the custom prefab and apply them to the grenade
            MeshRenderer grenadeRenderer = grenadePrefab.GetComponent<MeshRenderer>("Mesh");
            BoxCollider shipCollider = grenadePrefab.GetComponent<BoxCollider>("ShipCollider");
            BoxCollider environmentCollider = grenadePrefab.GetComponent<BoxCollider>("EnvCollider");

            // setup Components
            environmentCollider.gameObject.layer = LayerMask.NameToLayer("Weapon"); // the weapon layer only collides with scenery

            // setup speed
            float velBasedSpeed = owner.InverseTransformDirection(owner.RBody.velocity).z * ConfigSpeed * 0.06f;
            grenade.StatSpeed = Mathf.Max(ConfigSpeed, velBasedSpeed);

            // setup other stats
            grenade.ImpactSettings = ConfigImpactSettings;
            grenade.StatLifetime = ConfigLifetime;
            grenade.StatGravity = ConfigGravity;
            grenade.StatBounceForce = ConfigBounceForce;
            grenade.StatMaxBounces = ConfigMaxBounces;

            // setup Colliders
            grenade.ShipCollider = shipCollider;
            grenade.EnvironmentCollider = environmentCollider;
            grenade.ProjectileMeshRenderer = grenadeRenderer;

            // cache transform of the grenade mesh object.
            grenade.GrenadeMeshT = grenadeRenderer.transform;

            // assign explosion prefab
            grenade.PrefabExplosion = GrenadeMod.ExposionPrefab;

            grenade.ConfigureProjectile(owner.CollisionMeshObject, grenade.StatSpeed, owner);

            // check if there is environment between the ship and grenade spawn position and destroy the grenade if there is
            bool envBlockingGrenade = Physics.Linecast(owner.RBody.position, position, Layers.FloorMask | Layers.TrackWall);
            if (envBlockingGrenade) grenade.DestroyProjectile(null);

            return grenade;
        }

        /*---How hard grenades will bounce off of surfaces---*/
        public float StatBounceForce;

        /*----How many bounces the grenades can make before they explode--*/
        public int StatMaxBounces;

        /*---How many times the grenade has bounced---*/
        public int BounceCount;

        /*---The cached transform reference for the grenade mesh---*/
        public Transform GrenadeMeshT;

        /*---This is called every rendered frame---*/
        public override void OnUpdate()
        {
            /*-------------------------------------------------------------------------------------------------------------------------------------------------
             Update the grenade mesh transform to point along the grenades velocity. Since this is a direction vector we want to make sure we normalize it.

             This will provide the illusion of the grenades rotating with physics and sell the visuals of them a bit more.
             -------------------------------------------------------------------------------------------------------------------------------------------------*/
            GrenadeMeshT.forward = Body.velocity.normalized;
        }

        /*---This is called every physics step---*/
        public override void OnFixedUpdate()
        {
            /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
             Get the color of the cloest track tile. Since we're not using GetTrackRotation(), which will contain a tile in the data it returns, 
                we can get it from the projectiles current track section instead.

             We'll compare the distance between the two tiles to the projectiles position. We're going to only calculate the square magnitude of the distance, which isn't accurate but
                since we're just comparing the values it doesn't actually matter.
             ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
            if (CurrentSection && CurrentSection.tiles.Length == 2)
            {
                Vector3 position = Body.position;

                Tile tileA = CurrentSection.tiles[0];
                Tile tileB = CurrentSection.tiles[1];
                float fstDistToA = (tileA.position - position).sqrMagnitude;
                float fstDistToB = (tileB.position - position).sqrMagnitude;

                // set the tint color property of the projectile. This marks the projectile lighting as dirty and will automatically update the ProjectileMeshRenderer we assigned above.
                TintColor = fstDistToA < fstDistToB ? tileA.color : tileB.color;
            }

            // keep grenades moving at a fixed rate forwards
            Vector3 vel = transform.InverseTransformDirection(Body.velocity);
            vel.z = StatSpeed;
            Body.velocity = transform.TransformDirection(vel);

            // gravity
            if (CurrentSection) Body.AddForce(-CurrentSection.Up * StatGravity, ForceMode.Acceleration);

            // update the life time of the projectile
            UpdateLifetime();
        }

        /*----------------------------------------------------------------------------------------------------------------------
         Assuming the prefab is setup correctly with the environment collider being solid and the ship collider being a trigger,
                this will trigger when the ship collider starts to collide with something.
        ----------------------------------------------------------------------------------------------------------------------*/
        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            // don't do anything against the object that owns the projectile or trigger behaviours
            if (other.gameObject == Origin || other.GetComponent<NgTriggerBehaviour>()) return;

            // destroy Mines
            if (other.CompareTag("Explosive"))
            {
                DestroyExplosive(other.GetComponent<Explosive>());
                StatHelpers.MinesweeperStatAndAchievementHandler(Owner);
            }

            // do nothing if the rocket doesn't collide with a track or ship
            int layer = other.gameObject.layer;
            if (layer != LayerIDs.Ship) return;

            // get the other ship from the hit objects root transform
            Transform shipT = other.gameObject.transform.root;
            ShipController hitRef = shipT.GetComponent<ShipController>();
            bool hitShip = ShipImpact(hitRef);

            // if we can increase the score from the impact (team race), increase the ships score
            if (hitShip && Owner && Owner.CanIncreaseScoreFrom(hitRef)) Owner.IncreaseScore(WeaponConstants.EliminatorSettings.Mines);

            // finally destroy the grenade
            DestroyProjectile(shipT);
        }

        /*-----------------------------------------------------------------------------------------------------------------------
         Assuming the prefab is setup correctly with the environment collider being solid and the ship collider being a trigger,
                this will trigger when the environment collider starts to collide with something.
         ----------------------------------------------------------------------------------------------------------------------*/
        private void OnCollisionEnter(Collision other)
        {
            int otherLayer = other.gameObject.layer;

            /*------------------------------------------------------
             Determine if we've hit the track or floor.
             We could also determine if we hit a wall by using:

             bool hitwall = otherLayer == LayerIDs.TrackWall;
             -----------------------------------------------------*/
            bool hitFloor = otherLayer == LayerIDs.SmoothFloor || other.gameObject.layer == LayerIDs.FakeFloor || other.gameObject.layer == LayerIDs.TrackFloor;

            // reflect the grenade
            if (hitFloor)
            {
                Vector3 bounceNormal = other.contacts[0].normal;

                // reflect the forward vector of the grenade
                Vector3 forward = transform.forward;
                forward = Vector3.Reflect(forward, bounceNormal);
                transform.forward = forward;

                Body.AddForce(bounceNormal * StatBounceForce, ForceMode.VelocityChange);
                StatBounceForce *= 0.9f;

                // play Impact Sound
                NgSound.PlayOneShot(NgSound.GetAudioClip(NgSound.Ship_FloorHit), EAudioChannel.Sfx, 0.4f, 1.0f, transform.position, null, 15.0f, 30.0f);

                if (BounceCount > StatMaxBounces) DestroyProjectile(null);

                // temporary bounce count immunity
                if (Lifetime > 0.5f) ++BounceCount;
            } else DestroyProjectile(null);
        }
    }
}