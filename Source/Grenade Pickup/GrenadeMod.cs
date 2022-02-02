using System;
using System.IO;
using System.Text;
using BallisticUnityTools.AssetApi;
using NgModding;
using NgMp;
using NgMp.Packets;
using NgPickups;
using NgShips;
using UnityEngine;

namespace Grenade_Pickup
{
    public class GrenadeMod : CodeMod
    {
        /// <summary>
        /// The pickup entry that will be applied to the ship.
        /// </summary>
        public static Pickup RegisteredPickup;

        /// <summary>
        /// The icon sprite for the grenade pickup on the HUD.
        /// </summary>
        public static Sprite GrenadeIcon;

        /// <summary>
        /// The loaded grenade assets package.
        /// </summary>
        public static ModAssets GrenadeAssets;

        /// <summary>
        /// The loaded grenade prefab.
        /// </summary>
        public static CustomPrefab GrenadePrefab;

        /// <summary>
        /// The loaded grenade explosion prefab.
        /// </summary>
        public static GameObject ExposionPrefab;

        /// <summary>
        /// The pickup line for the grenades.
        /// </summary>
        public static AudioClip GrenadePickupLine;

        /// <summary>
        /// The warning line for the grenades.
        /// </summary>
        public static AudioClip GrenadeWarningLine;

        /*---Called when our mod is registered into the game. Use this for setting stuff up---*/
        public override void OnRegistered(string modPath)
        {
            // load assets
            GrenadeAssets = ModAssets.Load(Path.Combine(modPath, "grenade.nga"));
            GrenadePrefab = GrenadeAssets.GetComponent<CustomPrefab>("Grenade", false);
            ExposionPrefab = GrenadeAssets.GetAsset<GameObject>("Explosion");

            GrenadePickupLine = GrenadeAssets.GetAsset<AudioClip>("Voice_Pickup");
            GrenadeWarningLine = GrenadeAssets.GetAsset<AudioClip>("Voice_Warning");

            Texture2D iconTexture = GrenadeAssets.GetAsset<Texture2D>("Icon"); // sprites are always saved as texture2d, we'll need to convert it back to a sprite manually
            GrenadeIcon = Sprite.Create(iconTexture, new Rect(0.0f, 0.0f, iconTexture.width, iconTexture.height), Vector2.zero);

            // setup prefab explosion with callback (see the script for more details)
            ExposionPrefab.AddComponent<GrenadeParticleCallback>();

            /*---------------------------------------------------------------------------
             Register the pickup. There's a lot going on here, so let's break this down
             --------------------------------------------------------------------------*/
            RegisteredPickup = new Pickup(
                r => new GrenadePickup(r),                         // when the ship goes to setup the pickup logic, this returns a new instance of our weapon pickup class

                "grenades",                                        // this is the name of the pickup and will be used for stuff like the give command

                GrenadeIcon,                                       // this is the icon of the pickup for the hud

                Pickup.EHudColor.Offensive,                        // this is the color of the pickup icon. Offensive is red, defensive is green

                GrenadePickupLine,                                 // the audio clip to play when a player picks this pickup up

                GrenadeWarningLine,                                // the audio clip to play for AI warning callouts for this pickup

                new [] {"Grenades_Pickup", "Grenades_Warning"},    // the name of the .wav files to load for custom sound packs. First entry is the pickup line, 2nd is the warning

                (ship, chance) => ship.CurrentPlace > 1,           // called when determining to give this pickup to a ship. Return true to always give the pickup to a ship,
                                                                   //    otherwise return your condition as true/false here
                                                                   //    the chance value is a random value between 0 and 100 that you can also use for extra randomness

                100);                                              // the weighting of the pickup. This is how likely this pickup is to be picked. A higher number = better chance of being picked

            // register Network Packet Callback
            NgNetworkBase.OnRecievedCustomPacketData += OnRecievedCustomPacketData;
        }

        /*---Called whenever the game recieves a code mod packet---*/
        private void OnRecievedCustomPacketData(NgNetworkBase network, byte subheader, NgPacket packet)
        {
            // we're using 0 as a base to identify packets for this mod
            if (subheader != 0) return;

            // open the packet
            packet.Open();
            {
                /*---------------------------------------------------------------------------------------------------------------------------------------------------------------
                 Check for ID that confirms this packet is for this mod. This allows us to prevent conflicts if another mod is also using 0 as the value for its subheader

                 We've wrapped the byte reading in a try/catch block because NgPacket will throw an exception if it tries to read data past its data length, so we can use this
                    to log the exception to the log file and not break code execution.
                 --------------------------------------------------------------------------------------------------------------------------------------------------------------*/
                byte[] packetIdRaw = new byte[7];

                try
                {
                    for (int i = 0; i < 7; ++i) packetIdRaw[i] = packet.ReadByte();
                }
                catch (Exception e)
                {
                    // note: if you want to write to the ingame console instead of the unity log, replace Debug with DebugConsole
                    Debug.LogError(e);
                    packet.Close();

                    return;
                }

                // verify packet
                string packetId = Encoding.UTF8.GetString(packetIdRaw, 0, 7);
                if (packetId != "XPL_GRN")
                {
                    packet.Close();
                    return;
                }

                // we've validated that this is an example mod grenade packet. Let's spawn the grenade
                // all we need to do this is figure out who fired the grenade. For other weapons, you might need to send more information then just who fired it.
                NgPeer peer = NgNetworkBase.CurrentNetwork.GetPeerById(packet.ReadNetworkConnection());
                if (peer == null) return;

                // get the ship from the peer that fired the grenades
                ShipController peerShip = peer.LinkedShip;
                if (!peerShip) return;

                // now spawn the grenades
                GrenadePickup.CreateGrenade(peerShip);

                // if we're the server then also relay this packet to the other peers using the weapon spawn channel
                if (NgNetworkBase.IsServer) NgNetworkBase.CurrentNetwork.SendToAllPeers(packet.ToBytes(), NgNetworkBase.CurrentNetwork.WeaponSpawnChannel);

            }
            // close the packet. This disposes the resources needed for reading the packet
            packet.Close();
        }

        /*---Will build a grenade spawn trigger packet and automatically send it to the right place.---*/
        public static void SendGrenadeSpawnTrigger(NgNetworkConnection clientId)
        {
            // create a new packet build to pack out data into
            // we're using the sub header value of 0 to categorize this packet
            NgPacketBuilder builder = new NgPacketBuilder(NgPacketHeader.CustomMessage, 0);

            // write our packet id
            byte[] packetId = Encoding.UTF8.GetBytes("XPL_GRN");
            for (int i = 0; i < packetId.Length; ++i) builder.Add(packetId[i]);

            // write peer id
            builder.Add(clientId);

            // convert builder to an actual packet
            NgPacket packet = builder.ToPacket();

            /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
             Send the packet to the target. If we're the server then we'll relay it to all connected peers, otherwise we'll send it to the server using NgNetworkBase.CurrentNetwork.ConnectionId.

             We'll send the packet over the weapon spawn channel.
             ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
            if (NgNetworkBase.IsServer) NgNetworkBase.CurrentNetwork.SendToAllPeers(packet.ToBytes(), NgNetworkBase.CurrentNetwork.WeaponSpawnChannel);
            else NgNetworkBase.CurrentNetwork.SendTo(packet.ToBytes(), NgNetworkBase.CurrentNetwork.WeaponSpawnChannel, NgNetworkBase.CurrentNetwork.ConnectionId);
        }
    }
}
