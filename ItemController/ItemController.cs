using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Fougerite.Events;
using Fougerite;
using UnityEngine;

namespace ItemController
{
    public class ItemController : Fougerite.Module
    {
        private IniParser config;
        private Dictionary<string, bool> List = new Dictionary<string, bool>();
        private string Craft = "You can't craft this!";
        private string Research = "You can't research this!";
        private int C4Usage = 0, GrenadeUsage = 0;
        private bool AllowMods = true;
        private float C4DMG = 1000f, GrenadeDMG = 100f;
        private bool _loaded = false;
        private bool _created = false;

        public override string Name
        {
            get { return "ItemController"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "ItemController"; }
        }

        public override Version Version
        {
            get { return new Version("1.2.2"); }
        }

        public override void Initialize()
        {
            Fougerite.Hooks.OnCrafting += OnCrafting;
            Fougerite.Hooks.OnResearch += OnResearch;
            Fougerite.Hooks.OnEntityHurt += OnEntityHurt;
            Fougerite.Hooks.OnCommand += OnCommand;
            Fougerite.Hooks.OnPlayerConnected += OnPlayerConnected;
            if (!File.Exists(Path.Combine(ModuleFolder, "Settings.ini")))
            {
                File.Create(Path.Combine(ModuleFolder, "Settings.ini")).Dispose();
                config = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                _created = true;
            }
            else
            {
                config = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                Craft = config.GetSetting("Settings", "CMessage");
                Research = config.GetSetting("Settings", "RMessage");
                C4Usage = int.Parse(config.GetSetting("Settings", "C4Usage"));
                GrenadeUsage = int.Parse(config.GetSetting("Settings", "GrenadeUsage"));
                AllowMods = config.GetBoolSetting("Settings", "AllowMods");
                C4DMG = float.Parse(config.GetSetting("Settings", "C4DMG"));
                GrenadeDMG = float.Parse(config.GetSetting("Settings", "GrenadeDMG"));
            }
        }

        public override void DeInitialize()
        {
            Fougerite.Hooks.OnCrafting -= OnCrafting;
            Fougerite.Hooks.OnResearch -= OnResearch;
            Fougerite.Hooks.OnEntityHurt -= OnEntityHurt;
            Fougerite.Hooks.OnCommand -= OnCommand;
            Fougerite.Hooks.OnPlayerConnected -= OnPlayerConnected;
        }

        public void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (cmd == "itemcontroller")
            {
                if (player.Admin || (player.Moderator && AllowMods))
                {
                    config = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                    Craft = config.GetSetting("Settings", "CMessage");
                    Research = config.GetSetting("Settings", "RMessage");
                    C4Usage = int.Parse(config.GetSetting("Settings", "C4Usage"));
                    GrenadeUsage = int.Parse(config.GetSetting("Settings", "GrenadeUsage"));
                    AllowMods = config.GetBoolSetting("Settings", "AllowMods");
                    C4DMG = float.Parse(config.GetSetting("Settings", "C4DMG"));
                    GrenadeDMG = float.Parse(config.GetSetting("Settings", "GrenadeDMG"));
                    var section = config.EnumSection("Disable");
                    foreach (string item in section)
                    {
                        var founditem = Fougerite.Util.GetUtil().ConvertNameToData(item);
                        bool b = config.GetBoolSetting("Disable", item);
                        List[founditem.name] = b;
                    }
                    player.MessageFrom("ItemController", "Config reloaded!");
                }
            }
        }

        public void OnResearch(ResearchEvent re)
        {
            var item = re.ItemDataBlock;
            if (List.ContainsKey(item.name))
            {
                if (List[item.name])
                {
                    re.Cancel();
                    re.Player.Notice(Research);
                }
            }
        }

        public void OnCrafting(CraftingEvent e)
        {
            var item = e.ResultItem;
            if (List.ContainsKey(item.name))
            {
                if (List[item.name])
                {
                    e.Cancel();
                    e.Player.Notice(Craft);
                }
            }
        }

        public void OnEntityHurt(HurtEvent he)
        {
            if (he.AttackerIsPlayer && !he.VictimIsPlayer && !he.IsDecay)
            {
                if (he.Attacker != null && he.Entity != null)
                {
                    if (he.WeaponName.Equals("Explosive Charge"))
                    {
                        if (C4Usage == 1)
                        {
                            he.DamageAmount = 0f;
                            return;
                        }
                        if (C4Usage == 2)
                        {
                            if (!he.Entity.Name.ToLower().Contains("door"))
                            {
                                he.DamageAmount = 0f;
                            }
                            return;
                        }
                        if ((int) C4DMG == 0)
                        {
                            return;
                        }
                        he.DamageAmount = C4DMG;
                    }
                    else if (he.WeaponName.Equals("F1 Grenade"))
                    {
                        if (GrenadeUsage == 1)
                        {
                            he.DamageAmount = 0f;
                            return;
                        }
                        if (GrenadeUsage == 2)
                        {
                            if (!he.Entity.Name.ToLower().Contains("door"))
                            {
                                he.DamageAmount = 0f;
                            }
                            return;
                        }
                        if ((int) GrenadeDMG == 0)
                        {
                            return;
                        }
                        he.DamageAmount = GrenadeDMG;
                    }
                }
            }
        }

        public void OnPlayerConnected(Fougerite.Player player)
        {
            if (!_loaded)
            {
                try
                {
                    if (_created)
                    {
                        config.AddSetting("Settings", "CMessage", Craft);
                        config.AddSetting("Settings", "RMessage", Research);
                        config.AddSetting("Settings", "GrenadeUsage", GrenadeUsage.ToString());
                        config.AddSetting("Settings", "C4Usage", C4Usage.ToString());
                        config.AddSetting("Settings", "AllowMods", "true");
                        config.AddSetting("Settings", "C4DMG", C4DMG.ToString());
                        config.AddSetting("Settings", "GrenadeDMG", GrenadeDMG.ToString());
                        foreach (ItemDataBlock block in DatablockDictionary.All)
                        {
                            if (block.name.ToLower().Contains("bp") || block.name.ToLower().Contains("blueprint"))
                            {
                                continue;
                            }
                            config.AddSetting("Disable", block.name, "false");
                            List.Add(block.name, false);
                        }
                        config.Save();
                    }
                    else
                    {
                        var section = config.EnumSection("Disable");
                        foreach (string item in section)
                        {
                            var founditem = Fougerite.Util.GetUtil().ConvertNameToData(item);
                            bool b = config.GetBoolSetting("Disable", item);
                            List.Add(founditem.name, b);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("[ItemController] Loading Error: " + ex);
                }
                _loaded = true;
            }   
        }
    }
}
