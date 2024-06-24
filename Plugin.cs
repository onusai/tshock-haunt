using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Text.Json;

namespace Haunt
{
    [ApiVersion(2, 1)]
    public class Haunt : TerrariaPlugin
    {

        public override string Author => "Onusai";
        public override string Description => "Makes it easier for dead players to spectate";
        public override string Name => "Haunt";
        public override Version Version => new Version(1, 0, 0, 0);

        public class ConfigData
        {
            public bool CommandEnabled { get; set; } = true;
            public bool OnlyAllowIfOnTeam { get; set; } = true;
        }

        ConfigData configData;

        public Haunt(Main game) : base(game) { }

        public override void Initialize()
        {
            configData = PluginConfig.Load("Haunt");
            ServerApi.Hooks.GameInitialize.Register(this, OnGameLoad);
        }

        void OnGameLoad(EventArgs e)
        {
            RegisterCommand("haunt", "", HauntPlayer, "Haunt your fellow companions.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name)
            { HelpText = helptext });
        }

        void HauntPlayer(CommandArgs args)
        {
            if (!configData.CommandEnabled) return;
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("Usage: /haunt <player name>\n target player must be on the same team as you\n<player name> can just be the first couple letters of the name");
                return;
            }

            if (args.Player.Difficulty == 2 && args.Player.Dead)
            {
                string target = args.Parameters[0];
                var players = TSPlayer.FindByNameOrID(target);

                if (players.Count == 0)
                {
                    args.Player.SendErrorMessage(String.Format("Player \"{0}\" doesn't exist", target));
                    return;
                }

                var player = players[0];

                if (configData.OnlyAllowIfOnTeam && args.Player.Team != player.Team)
                {
                    args.Player.SendErrorMessage(String.Format("Unable to haunt {0}, they are not on your team", target));
                    return;
                }

                args.Player.Teleport(player.X, player.Y, 255);
                player.SendMessage(String.Format("{0} is watching you", args.Player.Name), Color.DarkSlateGray);

            }
            else args.Player.SendErrorMessage("You must be a ghost to use this command");
        }

        public static class PluginConfig
        {
            public static string filePath;
            public static ConfigData Load(string Name)
            {
                filePath = String.Format("{0}/{1}.json", TShock.SavePath, Name);

                if (!File.Exists(filePath))
                {
                    var data = new ConfigData();
                    Save(data);
                    return data;
                }

                var jsonString = File.ReadAllText(filePath);
                var myObject = JsonSerializer.Deserialize<ConfigData>(jsonString);

                return myObject;
            }

            public static void Save(ConfigData myObject)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(myObject, options);

                File.WriteAllText(filePath, jsonString);
            }
        }

    }
}