﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        class SyncPipesConfig
        {
            private static readonly SyncPipesConfig Default = New();

            public static SyncPipesConfig New()
            {
                return new SyncPipesConfig
                {
                    FilterSizes = new List<int> { 0, 6, 18, 30, 42 },
                    FlowRates = new List<int> { 1, 5, 10, 30, 50 },
                    MaximumPipeDistance = 64f,
                    MinimumPipeDistance = 2f,
                    NoDecay = true,
                    CommandPrefix = "p",
                    HotKey = "p",
                    UpdateRate = 2,
                    AttachXmasLights = false
                };
            }

            [JsonProperty("filterSizes")] 
            public List<int> FilterSizes { get; set; }

            [JsonProperty("flowRates")] 
            public List<int> FlowRates { get; set; }

            [JsonProperty("maxPipeDist")]
            public float MaximumPipeDistance { get; set; }

            [JsonProperty("minPipeDist")]
            public float MinimumPipeDistance { get; set; }

            [JsonProperty("noDecay")]
            public bool NoDecay { get; set; }

            [JsonProperty("commandPrefix")]
            public string CommandPrefix { get; set; }

            [JsonProperty("hotKey")]
            public string HotKey { get; set; }

            [JsonProperty("updateRate")]
            public int UpdateRate { get; set; }

            [JsonProperty("xmasLights")]
            public bool AttachXmasLights { get; set; }

            [JsonProperty("permLevels", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Dictionary<string, PermissionLevel> PermissionLevels { get; set; }

            [JsonProperty("salvageDestroy")] public bool DestroyWithSalvage { get; set; } = false;

            public class PermissionLevel
            {
                [JsonProperty("upgradeLimit")]
                public int MaximumGrade { get; set; } = (int)BuildingGrade.Enum.TopTier;

                [JsonProperty("pipeLimit")]
                public int MaximumPipes { get; set; } = -1;

                public static readonly PermissionLevel Default = new PermissionLevel() {MaximumGrade = (int)BuildingGrade.Enum.Twigs, MaximumPipes = 0};
            }

            private string[] Validate()
            {
                var errors = new List<string>();
                var filterSizeError = FilterSizes.Count != 5;
                if (!filterSizeError)
                {
                    for (var i = 0; i < FilterSizes.Count; i++)
                    {
                        if (FilterSizes[i] < 0 || FilterSizes[i] > 42)
                        {
                            filterSizeError = true;
                            break;
                        }
                    }
                }
                if (filterSizeError)
                {
                    errors.Add("filterSizes must have 5 values between 0 and 42");
                    FilterSizes = new List<int>(Default.FilterSizes);
                }

                var flowRateError = FlowRates.Count != 5;
                if (!flowRateError)
                {
                    for (var i = 0; i < FlowRates.Count; i++)
                    {
                        if (FlowRates[i] <= 0)
                        {
                            flowRateError = true;
                            break;
                        }
                    }
                }
                if (flowRateError)
                {
                    errors.Add("flowRates must have 5 values greater than 0");
                    FlowRates = new List<int>(Default.FlowRates);
                }

                if (UpdateRate <= 0)
                {
                    errors.Add("updateRage must be greater than 0");
                    UpdateRate = Default.UpdateRate;
                }

                return errors.ToArray();
            }

            public static SyncPipesConfig Load()
            {
                Instance.Puts("Loading Config");
                var config = Instance.Config.ReadObject<SyncPipesConfig>();
                if (config?.FilterSizes == null)
                {
                    Instance.Puts("Setting Defaults");
                    config = New();
                    Instance.Config.WriteObject(config);
                }

                var errors = config.Validate();
                for(var i =0; i < errors.Length; i++)
                    Instance.PrintWarning(errors[i]);
                return config;
            }


            /// <summary>
            /// Register the level permission keys to Oxide
            /// </summary>
            public void RegisterPermissions()
            {
                if (PermissionLevels != null)
                {
                    foreach (var permissionKey in PermissionLevels.Keys)
                    {
                        Instance.permission.RegisterPermission($"{Instance.Name}.level.{permissionKey}", Instance);
                    }
                }
            }
        }

        /// <summary>
        /// Oxide hook for loading default config settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config?.Clear();
            _config = SyncPipesConfig.New();
            Config?.WriteObject(_config);
            SaveConfig();
        }

        /// <summary>
        /// Config for this plugin instance.
        /// </summary>
        static SyncPipesConfig InstanceConfig => Instance._config;

        private SyncPipesConfig _config; // the config store for this plugin instance

        /// <summary>
        /// New Hook: Exposes the No Decay config to external plugins
        /// </summary>
        private bool IsNoDecayEnabled => InstanceConfig.NoDecay;
    }
}
