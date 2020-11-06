using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
		/// <summary>
		/// This enum stores all known containers that can connect to the pipes.
		/// The value of each enum the prefab id of that item
		/// Each enum should have an StorageAttribute which defines its name and partial icon url.
		/// Offsets can also be defined in the attribute 
		/// </summary>
        public enum Storage: uint
        {
			[Storage("fireplace.deployed", "https://static.wikia.nocookie.net/rust_gamepedia/images/c/c2/Stone_Fireplace.png/revision/latest/scale-to-width-down/{0}", 0,-1.3f,0,false)]
            Fireplace = 110576239,

			[Storage("mailbox.deployed", "1/17/Mail_Box_icon.png", 0, 0, -0.15f)]
			Mailbox = 2697131904,

			[Storage("stocking_small_deployed", "9/97/Small_Stocking_icon.png")]
			SmallStocking = 3141927338,

			[Storage("stocking_large_deployed", "6/6a/SUPER_Stocking_icon.png")]
			SuperStocking = 771996658,

			[Storage("mining.pumpjack", "c/c9/Pump_Jack_icon.png")]
			PumpJack = 1599225199,

			[Storage("survivalfishtrap.deployed", "9/9d/Survival_Fish_Trap_icon.png")]
			SurvivalFishTrap = 3119617183,

			[Storage("researchtable_deployed", "2/21/Research_Table_icon.png", 0.8f, -0.5f, -0.3f)]
			ResearchTable = 146554961,

			[Storage("planter.small.deployed", "a/a7/Small_Planter_Box_icon.png")]
			SmallPlanterBox = 467313155,

			[Storage("planter.large.deployed", "3/35/Large_Planter_Box_icon.png")]
			LargePlanterBox = 1162882237,

			[Storage("jackolantern.happy", "9/92/Jack_O_Lantern_Happy_icon.png")]
			JackOLanternHappy = 630866573,
			
            [Storage("jackolantern.angry", "9/96/Jack_O_Lantern_Angry_icon.png")]
			JackOLanternAngry = 1889323056,

			[Storage("furnace.large", "e/ee/Large_Furnace_icon.png", 0, -1.5f)]
			LargeFurnace = 1374462671,

			[Storage("campfire", "3/35/Camp_Fire_icon.png")]
			CampFire = 1946219319,

			[Storage("skull_fire_pit", "3/32/Skull_Fire_Pit_icon.png")]
			SkullFirePit = 1906669538,

			[Storage("bbq.deployed", "f/f8/Barbeque_icon.png", -0.2f, -0.2f)]
            Barbeque = 2409469892,

			[Storage("furnace", "e/e3/Furnace_icon.png", 0, -0.5f)]
			Furnace = 2931042549,

			[Storage("box.wooden.large", "b/b2/Large_Wood_Box_icon.png")]
			LargeWoodBox = 2206646561,

			[Storage("mining_quarry", "b/b8/Mining_Quarry_icon.png")]
			MiningQuarry = 672916883,

			[Storage("repairbench_deployed", "3/3b/Repair_Bench_icon.png")]
			RepairBench = 3846783416,

			[Storage("refinery_small_deployed", "a/ac/Small_Oil_Refinery_icon.png", -0.3f, -0.2f, -0.1f)]
			SmallOilRefinery = 1057236622,

			[Storage("small_stash_deployed", "5/53/Small_Stash_icon.png")]
			SmallStash = 2568831788,

			[Storage("woodbox_deployed", "f/ff/Wood_Storage_Box_icon.png")]
			WoodStorageBox = 1560881570,

			[Storage("vendingmachine.deployed", "5/5c/Vending_Machine_icon.png", 0, -0.5f)]
			VendingMachine = 186002280,

			[Storage("dropbox.deployed", "4/46/Drop_Box_icon.png", 0, 0.4f, 0.3f)]
			DropBox = 661881069,

			[Storage("fridge.deployed", "8/88/Fridge_icon.png", 0, -0.5f)]
			Fridge = 1844023509,

			[Storage("guntrap.deployed", "6/6c/Shotgun_Trap_icon.png")]
			ShotgunTrap = 1348746224,

			[Storage("flameturret.deployed", "f/f9/Flame_Turret_icon.png", 0, -0.3f, 0.1f)]
			FlameTurret = 4075317686,

			[Storage("recycler_static", "e/ef/Recycler_icon.png")]
			Recycler = 1729604075,

			[Storage("cupboard.tool.deployed", "5/57/Tool_Cupboard_icon.png", 0, -0.5f)]
			ToolCupboard = 2476970476,

			// Need to work out how to connect to this
			//[Storage("small_fuel_generator.deployed", "")]
			//SmallFuelGenerator = 3518207786,

			//[Storage("sam_site_turret_deployed", "")]
			//SAMSite = 2059775839,

            // Need to workout how to connect to this.
			[Storage("autoturret_deployed", "f/f9/Auto_Turret_icon.png", 0, -0.58f)]
			AutoTurret = 3312510084,

			//Temporary need to replace this image
			[Storage("composter", "https://i.imgur.com/qpA7I8P.png", partialUrl: false)]
			Composter = 1921897480,

			[Storage("hopperoutput", "b/b8/Mining_Quarry_icon.png", 0,-0.6f,-0.3f)]
			QuarryHopperOutput = 875142383,

			[Storage("fuelstorage", "b/b8/Mining_Quarry_icon.png", -0.5f,0, -0.4f)]
			QuarryFuelInput = 362963830,

            [Storage("fuelstorage", "c/c9/Pump_Jack_icon.png", -0.5f, 0.1f, -0.3f)]
			PumpJackFuelInput = 4260630588,

            [Storage("crudeoutput", "c/c9/Pump_Jack_icon.png", 0, 0, -0.5f)]
			PumpJackCrudeOutput = 70163214,

			[Storage("unknown.container", "https://i.imgur.com/cayN7SQ.png", partialUrl: false)]
			Default = 0
		}
		
        public class StorageData
        {
            public StorageData(string shortName, string url, Vector3 offset, bool partialUrl = true)
            {
                ShortName = shortName;
                Url = url;
                PartialUrl = partialUrl;
                Offset = offset;
            }

            /// <summary>
            /// The url or partial url of an container entity
            /// </summary>
            public readonly string Url;

            /// <summary>
            /// The shortname of a container entity. Currently not used but may be useful for debugging
            /// </summary>
            public readonly string ShortName;

            /// <summary>
            /// Indicates if this is attribute contains a full or partial url
            /// </summary>
            public readonly bool PartialUrl;

            /// <summary>
            /// In game offset of the pipe end points
            /// </summary>
            public readonly Vector3 Offset;
		}

        // This stores an indexed form of the Storage enum list
		private static Dictionary<Storage, StorageData> _storageDetails;

		static class StorageHelper
        {

			///// <summary>
			///// Converts the enum list into an Dictionary of Storage Attributes by the Storage enum
			///// </summary>
   //         static StorageHelper()
   //         {
   //             _storageDetails = Enum.GetValues(typeof(Storage)).OfType<Storage>()
   //                 .ToDictionary(a => a, a => GetAttribute<StorageAttribute>(a).Value);
   //         }

			/// <summary>
			/// Return the image url of the requested entity
			/// </summary>
			/// <param name="storageEntity">The entity to get the image url for</param>
			/// <param name="size">The size of the image required</param>
			/// <returns>The full url of storage entity
			/// If the storage entity is not found it will return the url of the Default storage enum</returns>
            public static string GetImageUrl(BaseEntity storageEntity, int size = 140)
            {
                if (storageEntity == null) return _storageDetails[Storage.Default].Url;
                var storageDetails = GetDetails(storageEntity);
				if(storageDetails != null)
                {
                    var url = storageDetails.PartialUrl
                        ? string.Format(
                            "http://vignette2.wikia.nocookie.net/play-rust/images/{0}/revision/latest/scale-to-width-down/{1}",
                            storageDetails.Url, size)
                        : string.Format(storageDetails.Url, size);
                    return url;
                }

                var parent = storageEntity.parentEntity.Get(true);
                if (parent != null)
                    return GetImageUrl(parent, size);
                return _storageDetails[Storage.Default].Url;
            }

			/// <summary>
			/// Get the offset vector of the pipe connection to the storage entity
			/// </summary>
			/// <param name="storageEntity">The entity to get the vector offset for</param>
			/// <returns>The pipe connection vector offset</returns>
            public static Vector3 GetOffset(BaseEntity storageEntity)
            {
                if (storageEntity == null) return Vector3.zero;
                var storageDetails = GetDetails(storageEntity);
                return storageEntity.transform.rotation * storageDetails?.Offset ?? Vector3.zero;
            }

			/// <summary>
			/// Get the storage details of the storage entity
			/// </summary>
			/// <param name="storageEntity"></param>
			/// <returns>The pipe details of the storage entity</returns>
            private static StorageData GetDetails(BaseEntity storageEntity)
            {
                if (storageEntity == null) return null;
                var storageItem = (Storage) storageEntity.prefabID;
                return _storageDetails.ContainsKey(storageItem) ? _storageDetails[storageItem] : null;
            }
        }
    }
}
