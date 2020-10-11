using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using MoreArtifacts;
using R2API;
using Phedg1Studios.ItemDropAPIFixes;
using System;

namespace ArtifactOfSimplicity {

    /// <summary>
    /// The Artifact of Simplicity mod adds a new artifact which limits all normal and lunar drops to a random subset each level.
    /// </summary>
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("com.Phedg1Studios.ItemDropAPIFixes")]
    [BepInDependency(MoreArtifacts.MoreArtifacts.ModGUID, MoreArtifacts.MoreArtifacts.ModVersion)]
    public class ArtifactOfSimplicityMod : BaseUnityPlugin {
        public const string ModGUID = "com.poik.simplicityartifact";
        public const string ModName = "Artifact of Simplicity";
        public const string ModVersion = "0.1.0";


        internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; }

        public static ArtifactOfSimplicity simplicityArtifact;

        public void Awake() {
            Logger = base.Logger;

            // initialize artifacts and other things here
            simplicityArtifact = new ArtifactOfSimplicity();
        }
    }

    /// <summary>
    /// This sets up the artifact itself, using MoreArtifact's stub.
    /// </summary>
    public class ArtifactOfSimplicity : NewArtifact<ArtifactOfSimplicity> {

        public override string Name => "Artifact of Simplicity";
        public override string Description => "Reduces the item pool for each rarity to two per stage.";
        public override Sprite IconSelectedSprite => CreateSprite(Properties.Resources.simplicity_selected, Color.magenta);
        public override Sprite IconDeselectedSprite => CreateSprite(Properties.Resources.simplicity_deselected, Color.gray);

        protected override void InitManager()
        {
            ArtifactOfSimplicityManager.Init();
        }
    }

    /// <summary>
    /// Overarching Manager for this artifact. Handles hooking and unhooking actions.
    /// </summary>
    public static class ArtifactOfSimplicityManager {
        private static ArtifactDef myArtifact {
            get
            {
                // In order to keep compatibility with ItemDropList and other mods, first fetch the currently active item sets.
                return ArtifactOfSimplicity.Instance.ArtifactDef; }
        }

        static private List<PickupIndex> alwaysItems = new List<PickupIndex>();
        private static List<PickupIndex> tier1Items = new List<PickupIndex>();
        private static List<PickupIndex> tier2Items = new List<PickupIndex>();
        private static List<PickupIndex> tier3Items = new List<PickupIndex>();
        private static List<PickupIndex> lunarItems = new List<PickupIndex>();
        private static List<PickupIndex> equipItems = new List<PickupIndex>();
        static public List<PickupIndex> runItems = new List<PickupIndex>();
        static public List<PickupIndex> currentItems = new List<PickupIndex>();

        public static void Init() {
            // initialize stuff here, like fields, properties, or things that should run only one time
            RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
            RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
        }

        private static void OnArtifactEnabled(RunArtifactManager man, ArtifactDef artifactDef)
        {
            if (!NetworkServer.active || artifactDef != myArtifact) {
                return;
            }

            // hook things
            Stage.onServerStageComplete += RandomizeItems;
            ItemDropAPIFixes.setDropLists += SetDropList;
        }

        private static void RevertItems()
        {
            ItemDropAPIFixes.playerItems = runItems;
            ItemDropAPIFixes.monsterItems = runItems;
            currentItems = new List<PickupIndex>();
            runItems = new List<PickupIndex>();
        }

        private static void OnArtifactDisabled(RunArtifactManager man, ArtifactDef artifactDef)
        {
            if (artifactDef != myArtifact) {
                return;
            }

            // unhook things
            Stage.onServerStageComplete -= RandomizeItems;
            ItemDropAPIFixes.setDropLists -= SetDropList;
            RevertItems();
        }

        private static void RandomizeItems(Stage _=null)
        {
            System.Random rnd = new System.Random();
            int i = 0;
            foreach (PickupIndex item in tier1Items.OrderBy(x => rnd.Next()).Take(2))
            {
                currentItems[i] = item;
                ArtifactOfSimplicityMod.Logger.LogInfo(item);
            }
            foreach (PickupIndex item in tier2Items.OrderBy(x => rnd.Next()).Take(2))
            {
                currentItems[i] = item;
                ArtifactOfSimplicityMod.Logger.LogInfo(item);
            }
            foreach (PickupIndex item in tier3Items.OrderBy(x => rnd.Next()).Take(2))
            {
                currentItems[i] = item;
                ArtifactOfSimplicityMod.Logger.LogInfo(item);
            }
            foreach (PickupIndex item in runItems.OrderBy(x => rnd.Next()).Take(2))
            {
                currentItems[i] = item;
                ArtifactOfSimplicityMod.Logger.LogInfo(item);
            }
            foreach (PickupIndex item in equipItems.OrderBy(x => rnd.Next()).Take(2))
            {
                currentItems[i] = item;
                ArtifactOfSimplicityMod.Logger.LogInfo(item);
            }
        }

        private static void SetDropList()
        {
            System.Random rnd = new System.Random();
            if (runItems.Count == 0) runItems = ItemDropAPIFixes.playerItems;
            if (currentItems.Count != 0) currentItems = new List<PickupIndex>();
            int tier1count = 0; // white
            int tier2count = 0; // green
            int tier3count = 0; // red
            int tier4count = 0; // lunar
            int tier5count = 0; // equipment
            List<string> currentItemNames = new List<string>();
            foreach (PickupIndex item in runItems.OrderBy(x => rnd.Next()))
            {
                PickupDef itemDef = PickupCatalog.GetPickupDef(item);
                if (currentItems.Contains(item))
                {
                    //ArtifactOfSimplicityMod.Logger.LogInfo("DUPLICATE!!!!!!");
                    //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                }
                else if (currentItemNames.Contains(item.ToString()))
                {
                    //ArtifactOfSimplicityMod.Logger.LogInfo("DUPLICATE NAME?!!?!!!!");
                    //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                }
                else if (itemDef.isBoss)
                { // Just add them, they're special drops
                    currentItems.Add(item);
                    alwaysItems.Add(item);
                } 
                else if (itemDef.isLunar)
                { // I hope this gets both equipment and items
                    if (tier4count < 2)
                    {
                        tier4count++;
                        currentItems.Insert(tier1count+tier2count+tier3count, item);
                        //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                    }
                    lunarItems.Add(item);
                } 
                else if (itemDef.internalName.ToLower().Contains("scrap"))
                { // Scrap should never be removed, in my opinion
                    currentItems.Add(item);
                    alwaysItems.Add(item);
                }
                else if (EquipmentCatalog.equipmentList.Contains(itemDef.equipmentIndex))
                { // This should be non-lunar, non-aspect equipment
                    if (tier5count < 2)
                    {
                        tier5count++;
                        currentItems.Insert(tier1count + tier2count + tier3count + tier4count, item);
                        //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                    }
                    equipItems.Add(item);
                }
                else if (EquipmentCatalog.allEquipment.Contains(itemDef.equipmentIndex))
                { // These should be the aspects, hopefully including from other mods
                    currentItems.Add(item);
                    alwaysItems.Add(item);
                }
                else if (!ItemCatalog.allItems.Contains(itemDef.itemIndex))
                { // No clue what these are
                    currentItems.Add(item);
                    alwaysItems.Add(item);
                }
                else if (ItemCatalog.tier1ItemList.Contains(itemDef.itemIndex))
                { // white items
                    if (tier1count < 2)
                    {
                        tier1count++;
                        currentItems.Insert(0, item);
                        //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                    }
                    tier1Items.Add(item);
                }
                else if (ItemCatalog.tier2ItemList.Contains(itemDef.itemIndex))
                { // green items
                    if (tier2count < 2)
                    {
                        tier2count++;
                        currentItems.Insert(tier1count, item);
                        //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                    }
                    tier2Items.Add(item);
                }
                else if (ItemCatalog.tier3ItemList.Contains(itemDef.itemIndex))
                { // red items
                    if (tier3count < 2)
                    {
                        tier3count++;
                        currentItems.Insert(tier1count + tier2count, item);
                        //ArtifactOfSimplicityMod.Logger.LogInfo(item);
                    }
                    tier3Items.Add(item);
                }
                currentItemNames.Add(item.ToString());
            }
            ItemDropAPIFixes.playerItems = currentItems;
            ItemDropAPIFixes.monsterItems = currentItems;
        }
    }
}
