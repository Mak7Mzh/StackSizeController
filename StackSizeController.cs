using System.Collections.Generic;
using System.Linq;
using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Stack Size Controller", "Canopy Sheep", "2.0.3", ResourceId = 2320)]
    [Description("Allows you to set the max stack size of every item.")]
    public class StackSizeController : RustPlugin
    {
        #region Data

        private bool pluginLoaded = false;
        Items items;
        class Items
        {
            public Dictionary<string, int> itemlist = new Dictionary<string, int>();
        }

        private bool LoadData()
        {
            var itemsdatafile = Interface.Oxide.DataFileSystem.GetFile("StackSizeController");
            try
            {
                items = itemsdatafile.ReadObject<Items>();
                return true;
            }
            catch (Exception ex)
            {
                PrintWarning("Error: Data file is corrupt. Debug info: " + ex.Message);
                return false;
            }
        }

        private void UpdateItems()
        {
            var gameitemList = ItemManager.itemList;
            List<string> itemCategories = new List<string>();
            int stacksize;

            foreach (var item in gameitemList)
            {
                if (!itemCategories.Contains(item.category.ToString()))
                {
                    if (!(configData.Settings.CategoryDefaultStack.ContainsKey(item.category.ToString())))
                    {
                        configData.Settings.CategoryDefaultStack[item.category.ToString()] = configData.Settings.NewCategoryDefaultSetting;
                        Puts("Added item category: '" + item.category.ToString() + "' to the config.");
                    }
                    itemCategories.Add(item.category.ToString());
                }

                if (!(items.itemlist.ContainsKey(item.displayName.english)))
                {
                    stacksize = DetermineStack(item);
                    items.itemlist.Add(item.displayName.english, stacksize);
                }
            }

            List<string> KeysToRemove = new List<string>();

            foreach (KeyValuePair<string ,int> category in configData.Settings.CategoryDefaultStack)
            {
                if (!itemCategories.Contains(category.Key)) { KeysToRemove.Add(category.Key); }
            }

            if (KeysToRemove.Count > 0)
            {
                Puts("Cleaning config categories...");
                foreach (string Key in KeysToRemove)
                {
                    configData.Settings.CategoryDefaultStack.Remove(Key);
                }
            }

            SaveConfig();

            KeysToRemove = new List<string>();
            bool foundItem = false;

            foreach (KeyValuePair<string, int> item in items.itemlist)
            {
                foreach (var itemingamelist in gameitemList)
                {
                    if (itemingamelist.displayName.english == item.Key)
                    {
                        foundItem = true;
                        break;
                    }
                }
                if (!(foundItem)) { KeysToRemove.Add(item.Key); }
                foundItem = false;
            }

            if (KeysToRemove.Count > 0)
            {
                Puts("Cleaning data file...");
                foreach (string key in KeysToRemove)
                {
                    items.itemlist.Remove(key);
                }
            }

            SaveData();
            LoadStackSizes();
        }

        private int DetermineStack(ItemDefinition item)
        {
            if (item.condition.enabled && item.condition.max > 0 && (!configData.Settings.StackHealthItems))
            {
                return 1;
            }
            else
            {
                if (configData.Settings.DefaultStack != 0 && (!configData.Settings.CategoryDefaultStack.ContainsKey(item.category.ToString())))
                {
                    return configData.Settings.DefaultStack;
                }
                else if (configData.Settings.CategoryDefaultStack.ContainsKey(item.category.ToString()) && configData.Settings.CategoryDefaultStack[item.category.ToString()] != 0)
                {
                    return configData.Settings.CategoryDefaultStack[item.category.ToString()];
                }
                else if (configData.Settings.DefaultStack != 0 && configData.Settings.CategoryDefaultStack[item.category.ToString()] == 0)
                {
                    return configData.Settings.DefaultStack;
                }
                else
                {
                    return item.stackable;
                }
            }
        }

        private void LoadStackSizes()
        {
            var gameitemList = ItemManager.itemList;

            foreach (var item in gameitemList)
            {
                item.stackable = items.itemlist[item.displayName.english];
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("StackSizeController", items);
        }

        #endregion

        #region Config

        ConfigData configData;
        class ConfigData
        {
            public SettingsData Settings { get; set; }
        }

        class SettingsData
        {
            public int DefaultStack { get; set; }
            public int NewCategoryDefaultSetting { get; set; }
            public bool StackHealthItems { get; set; }
            public Dictionary<string, int> CategoryDefaultStack { get; set; }
        }

        private void TryConfig()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch (Exception ex)
            {
                PrintWarning("Corrupt config detected, debug: " + ex.Message);
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Generating a new config file...");

            Config.WriteObject(new ConfigData
            {
                Settings = new SettingsData
                {
                    DefaultStack = 0,
                    NewCategoryDefaultSetting = 0,
                    StackHealthItems = true,
                    CategoryDefaultStack = new Dictionary<string, int>()
                    {
                        { "Ammunition", 0 },
                        { "Weapon", 0 },
                    },
                },
            }, true);
        }

        private void SaveConfig()
        {
            Config.WriteObject(configData);
        }
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            TryConfig();
            pluginLoaded = LoadData();

            if (pluginLoaded) { UpdateItems(); }
            else { Puts("Stack Sizes could not be changed due to a corrupt data file."); }

        }

        #endregion
    }
}