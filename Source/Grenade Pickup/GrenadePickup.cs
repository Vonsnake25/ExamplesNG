using NgAudio;
using NgData;
using NgMp;
using NgPickups;
using NgShips;
using UnityEngine;

namespace Grenade_Pickup
{
    public class GrenadePickup : PickupBase
    {
        /*---The horizontal spread for spawning grenades---*/
        public static float GrenadeSpawnSpread = 0.6f;

        public GrenadePickup(ShipController r) : base(r) { }

        /*---Called when the pickup is used---*/
        public override void OnUse()
        {
            CreateGrenade(R);

            // if we're playing on multiplayer then send out a spawn trigger
            if (NgNetworkBase.CurrentNetwork) GrenadeMod.SendGrenadeSpawnTrigger(NgNetworkBase.CurrentNetwork.ClientId);

            base.OnUse();
        }

        /*---Called when the pickup is dropped---*/
        public override void OnDrop()
        {
            base.OnDrop();
        }

        /*---Called per frame---*/
        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        /*---Called per physics tick---*/
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        /*---Called to init the AI control for this pickup--*/
        public override void OnAiInit()
        {
            // specify some delay before the AI will think about using the pickup. For this grenade we're using a random time between 10 and 25 seconds
            AiUsageDelay = Random.Range(10, 25);

            // warn the player that the ai has picked the grenade up if it's within 30 sections of the player
            ShipController player = Ships.PlayerOneShip;
            if (!player || !player.IsPlayer || R.IsPassiveTowards(player)) return;

            if (Ships.SectionOffsetBetween(Ships.PlayerOneShip, R) < 30) R.CurrentPickupRegister.WarnPlayer();
        }

        /*-------------------------------------------------------------------------------------------------------------------------
         Called per Ai physics tick.
         
         holdTimer is the time that the AI has been holding the weapon, including timer modifications from gamemodes.
         unscaledHoldTimer is the time that the AI has been holding the weapon, not including their modifications from gamemodes.
        --------------------------------------------------------------------------------------------------------------------------*/
        public override void AiUpdate(float holdTimer, float unscaledHoldTimer)
        {
            // if the total hold time is over the usage time then drop the pickup
            if (unscaledHoldTimer > AiUsageDelay) OnDrop();

            // check if there a ship in front of the ai
            bool canSeeShip = Physics.Raycast(R.RBody.position, R.Forward, out RaycastHit hit, 15.0f, Layers.ShipToShip); // cast against the ship to ship layer, which are the bounding box colliders of each ship
            if (!canSeeShip) return;

            // figure out which ship is at the hit point. If the AI is passive towards the ship, do nothing
            ShipController targetShip = Ships.GetClosestShipToPoint(hit.point);
            if (R.IsPassiveTowards(targetShip)) canSeeShip = false;

            if (canSeeShip) OnUse();
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         Spawns a new set of grenades. networkSpawn allows us to determine if the server is spawning the grenades and if it's a local spawn, we'll tell the server we've fired grenades
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        public static void CreateGrenade(ShipController ship)
        {
            // if we're a player then play a 2D deployment sound
            if (ship.IsPlayer) NgSound.PlayOneShot(NgSound.GetAudioClip(NgSound.Weapons_MineDrop), EAudioChannel.Sfx, 1.0f, 1.0f);

            // create the grenades
            GrenadeObject.CreateNew(ship.transform.TransformPoint(-GrenadeSpawnSpread, 0.0f, ship.MeshBoundsFront.z), ship.RBody.rotation, ship);
            GrenadeObject.CreateNew(ship.transform.TransformPoint(0.0f, 0.0f, 1.0f), ship.RBody.rotation, ship);
            GrenadeObject.CreateNew(ship.transform.TransformPoint(GrenadeSpawnSpread, 0.0f, ship.MeshBoundsFront.z), ship.RBody.rotation, ship);
        }
    }
}
