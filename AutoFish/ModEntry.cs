using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutoFish
{
    public class ModEntry : Mod
    {
        /// <summary>
        ///     配置文件
        /// </summary>
        public ModConfig Config = null!;

        private GameState? gameState;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("MaxCastPower.Name"),
                getValue: () => Config.MaxCastPower,
                setValue: value => Config.MaxCastPower = value,
                tooltip: () => Helper.Translation.Get("MaxCastPower.Tooltip")
            );
            configMenu.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("AutoHit.Name"),
                getValue: () => Config.AutoHit,
                setValue: value => Config.AutoHit = value,
                tooltip: () => Helper.Translation.Get("AutoHit.Tooltip")
            );
            configMenu.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("FastBite.Name"),
                getValue: () => Config.FastBite,
                setValue: value => Config.FastBite = value,
                tooltip: () => Helper.Translation.Get("FastBite.Tooltip")
            );
            configMenu.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("CatchTreasure.Name"),
                getValue: () => Config.CatchTreasure,
                setValue: value => Config.CatchTreasure = value,
                tooltip: () => Helper.Translation.Get("CatchTreasure.Tooltip")
            );
            configMenu.AddEnumTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("BarSpeedMode.Name"),
                getValue: () => Config.BarSpeedMode,
                setValue: value => Config.BarSpeedMode = value,
                formatAllowedValue: value => Helper.Translation.Get($"BarSpeedMode.Value.{value}"),
                tooltip: () => Helper.Translation.Get("BarSpeedMode.Tooltip")
            );
            configMenu.AddEnumTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("ContinuousFishing.Name"),
                getValue: () => Config.ContinuousFishing,
                setValue: value => Config.ContinuousFishing = value,
                formatAllowedValue: value => Helper.Translation.Get($"ContinuousFishing.Value.{value}"),
                tooltip: () => Helper.Translation.Get("ContinuousFishing.Tooltip")
            );
            configMenu.AddNumberOption(
                ModManifest,
                name: () => Helper.Translation.Get("FoodIndex.Name"),
                getValue: () => Config.FoodIndex,
                setValue: value => Config.FoodIndex = Math.Clamp(value, 0, 12),
                tooltip: () => Helper.Translation.Get("FoodIndex.Tooltip")
            );
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            var player = Game1.player;
            if (!Context.IsWorldReady || player == null)
                gameState = null;
            else
                gameState = (gameState ?? GameState.DefaultState(Game1.player, this, Helper.Reflection)).Next();
        }
    }
}