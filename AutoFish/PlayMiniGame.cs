using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace AutoFish
{
    public abstract record GameState
    {
        private GameState(Farmer Player, ModEntry Mod, IReflectionHelper Reflection)
        {
            this.Player = Player;
            this.Mod = Mod;
            this.Reflection = Reflection;
        }

        private Farmer Player { get; }
        private ModEntry Mod { get; }
        private ModConfig Config => Mod.Config;
        private IReflectionHelper Reflection { get; }

        public abstract GameState Next();

        public static GameState DefaultState(Farmer Player, ModEntry Mod, IReflectionHelper Reflection)
        {
            return new AfterMiniGame(Player, Mod, Reflection);
        }

        /// <summary>
        ///     获取当前正在使用的鱼竿
        /// </summary>
        /// <returns>如果当前工具是鱼竿而且正在使用，则返回该鱼竿，否则返回 <see langword="null" /> </returns>
        private FishingRod? GetUsingFishingRod()
        {
            if (Player.CurrentTool is FishingRod fishingRod && (fishingRod.inUse() || fishingRod.castedButBobberStillInAir))
                return fishingRod;
            return null;
        }

        private void ClickFishingRod()
        {
            ((FishingRod)Player.CurrentTool).DoFunction(Player.currentLocation, (int)Player.GetToolLocation().X, (int)Player.GetToolLocation().Y, 1, Player);
        }

        public record AfterMiniGame : GameState
        {
            public AfterMiniGame(GameState preState) : base(preState)
            {
            }

            public AfterMiniGame(Farmer Player, ModEntry Mod, IReflectionHelper Reflection) : base(Player, Mod, Reflection)
            {
            }

            public override GameState Next()
            {
                if (Game1.activeClickableMenu is ItemGrabMenu)
                    return this;
                if (GetUsingFishingRod() is not { } fishingRod)
                    return this;

                switch (fishingRod)
                {
                    case { isTimingCast: true }: // 蓄力中
                        if (Config.MaxCastPower)
                            fishingRod.castingPower = 1;
                        return this;
                    case { isCasting: true }: // 抛竿中，鱼钩还没抛出
                        return this;
                    case { castedButBobberStillInAir: true }: // 鱼钩抛出但是还没入水
                        return this;
                    case { isFishing: true, isNibbling: false }: // 钓鱼中，鱼还没咬钩
                        if (Config.FastBite && fishingRod.timeUntilFishingBite > 0)
                            fishingRod.timeUntilFishingBite /= 2;
                        return this;
                    case { isNibbling: true, hit: true }: // 鱼咬钩了而且点击收杆了
                        return this;
                    case { isNibbling: true, pullingOutOfWater: true }: // 钓到垃圾了
                        return new PullingOutOfWater(this);
                    case { isReeling: true }: // 进入收杆状态，玩小游戏状态
                        return PlayMiniGame.DefaultState(this);
                    case { isNibbling: true, fishCaught: true }:
                    case { isNibbling: true, treasureCaught: true }:
                        return this;
                    case { isNibbling: true, isReeling: false, hit: false, pullingOutOfWater: false, fishCaught: false, showingTreasure: false }:
                        ClickFishingRod();
                        return this;
                    default:
                        throw new Exception();
                }
            }
        }

        private abstract record PlayMiniGame : GameState
        {
            private const int FishHeight = 28;

            private PlayMiniGame(GameState preState) : base(preState)
            {
            }

            public static PlayMiniGame DefaultState(GameState preState)
            {
                return new CatchFish(preState);
            }

            private static BobberBar? GetBobberBar()
            {
                return Game1.activeClickableMenu as BobberBar;
            }

            private static bool IsOnPressedUseToolButton()
            {
                return Game1.oldMouseState.LeftButton == ButtonState.Pressed ||
                       Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton) ||
                       (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.X) || Game1.oldPadState.IsButtonDown(Buttons.A)));
            }

            private static float GetSpeed(float deltaSpeed, float targetDisplacement)
            {
                return targetDisplacement switch
                {
                    > 0 => MathF.Sqrt(2 * deltaSpeed * targetDisplacement),
                    0 => 0,
                    < 0 => -MathF.Sqrt(2 * deltaSpeed * -targetDisplacement),
                    _ => throw new ArgumentOutOfRangeException(nameof(targetDisplacement), targetDisplacement, null),
                };
            }

            /// <summary>
            ///     默认绿条速度（包括钓具影响）
            /// </summary>
            /// <param name="bar"></param>
            /// <returns></returns>
            private float DeltaSpeed(BobberBar bar)
            {
                if (!Reflection.GetField<bool>(bar, "bobberInBar").GetValue())
                    return 0.25f;
                var deltaSpeed = 0.25f * 0.6f;
                var whichBobber = Reflection.GetField<int>(bar, "whichBobber").GetValue();
                if (whichBobber == 691) // 倒刺钩
                    deltaSpeed = 0.25f * 0.3f;
                return deltaSpeed;
            }

            /// <summary>
            ///     更新速度
            /// </summary>
            /// <param name="bar"></param>
            /// <param name="barTargetPos"></param>
            /// <param name="barTargetSpeed">绿条的目标的速度</param>
            private void UpdateBarSpeed(BobberBar bar, float barTargetPos, float barTargetSpeed)
            {
                EliminatePlayerEffectForBarSpeed(bar);

                var barPos = Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                var barHeight = Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
                var barSpeed = Reflection.GetField<float>(bar, "bobberBarSpeed").GetValue();
                var barPosMax = BobberBar.bobberBarTrackHeight - barHeight;

                var targetDisplacement = Math.Clamp(barTargetPos - 0.5f * barHeight, 0.0f, barPosMax) - barPos;

                float autoDeltaSpeed;
                switch (Config.BarSpeedMode)
                {
                    case BarSpeedModeValue.Faster:
                        autoDeltaSpeed = 1f;
                        break;
                    case BarSpeedModeValue.Attach:
                        Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(targetDisplacement);
                        return;
                    case BarSpeedModeValue.Normal:
                    default:
                        autoDeltaSpeed = 0.25f; // 此处采用固定的速度，而不是像星露谷代码里面（像DeltaSpeed方法那样）的会随着鱼是否在绿条内而变化
                        break;
                }
                var targetSpeed = GetSpeed(autoDeltaSpeed, targetDisplacement);

                // 让鱼的移动速度参与影响绿条的移动速度，可能没有什么意义
                var maxSpeed = GetSpeed(autoDeltaSpeed, Math.Clamp(barTargetPos + (barHeight + FishHeight) / 3f - 0.5f * barHeight, 0.0f, barPosMax) - barPos);
                var minSpeed = GetSpeed(autoDeltaSpeed, Math.Clamp(barTargetPos - (barHeight + FishHeight) / 3f - 0.5f * barHeight, 0.0f, barPosMax) - barPos);
                targetSpeed = Math.Clamp(targetSpeed + barTargetSpeed / 2, minSpeed, maxSpeed);

                if (barSpeed < targetSpeed)
                    barSpeed += autoDeltaSpeed;
                else
                    barSpeed -= autoDeltaSpeed;

                Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(barSpeed);
            }

            /// <summary>
            ///     消除玩家操作影响
            /// </summary>
            /// <param name="bar"></param>
            private void EliminatePlayerEffectForBarSpeed(BobberBar bar)
            {
                var barSpeed = Reflection.GetField<float>(bar, "bobberBarSpeed").GetValue();
                var deltaSpeed = DeltaSpeed(bar);
                var onPressed = IsOnPressedUseToolButton();
                barSpeed += onPressed ? deltaSpeed : -deltaSpeed;
                Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(barSpeed);
            }

            private record CatchFish : PlayMiniGame
            {
                public CatchFish(GameState preState) : base(preState)
                {
                }

                public override GameState Next()
                {
                    if (GetUsingFishingRod() is not { } fishingRod)
                        return new AfterMiniGame(this);

                    switch (fishingRod)
                    {
                        case { pullingOutOfWater: true }:
                            return new PullingOutOfWater(this);
                        case { isReeling: false }:
                            return new AfterMiniGame(this);
                    }

                    if (GetBobberBar() is not { } bar)
                        return this;

                    if (Config.CatchTreasure)
                    {
                        var isBossFish = Reflection.GetField<bool>(bar, "bossFish").GetValue();
                        var treasureCaught = Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                        var hasTreasure = Reflection.GetField<bool>(bar, "treasure").GetValue();
                        var distanceFromCatching = Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
                        if (!isBossFish && hasTreasure && !treasureCaught && distanceFromCatching > 0.75)
                            return new CatchTreasure(this);
                    }

                    var fishPos = Reflection.GetField<float>(bar, "bobberPosition").GetValue();
                    var barTargetPos = fishPos + 30;

                    var fishSpeed = Reflection.GetField<float>(bar, "bobberSpeed").GetValue();
                    UpdateBarSpeed(bar, barTargetPos, fishSpeed);

                    return this;
                }
            }

            private record CatchTreasure : PlayMiniGame
            {
                public CatchTreasure(GameState preState) : base(preState)
                {
                }

                public override GameState Next()
                {
                    if (GetUsingFishingRod() is not { } fishingRod)
                        return new AfterMiniGame(this);

                    switch (fishingRod)
                    {
                        case { pullingOutOfWater: true }:
                            return new PullingOutOfWater(this);
                        case { isReeling: false }:
                            return new AfterMiniGame(this);
                    }

                    if (GetBobberBar() is not { } bar)
                        return this;

                    var treasureCaught = Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                    var distanceFromCatching = Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
                    if (treasureCaught || distanceFromCatching < 0.25)
                        return new CatchFish(this);

                    var treasurePos = Reflection.GetField<float>(bar, "treasurePosition").GetValue();
                    var barTargetPos = treasurePos + 30;

                    UpdateBarSpeed(bar, barTargetPos, 0);

                    return this;
                }
            }
        }

        private record PullingOutOfWater : GameState
        {
            public PullingOutOfWater(GameState preState) : base(preState)
            {
            }

            public override GameState Next()
            {
                if (GetUsingFishingRod() is not { } fishingRod)
                    return new AfterMiniGame(this);

                if (Config.ContinuousFishing is not (ContinuousFishingValue.UntilStaminaTooLow or ContinuousFishingValue.AutoFishAndEat))
                    return new AfterMiniGame(this);

                if (fishingRod.fishCaught is true)
                {
                    object oldKbState = Game1.oldKBState;
                    var action = oldKbState.GetType().GetMethod("InternalSetKey", BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance)!
                        .CreateDelegate<Action<Keys>>(oldKbState);
                    action(Game1.options.useToolButton[0].key);
                    Game1.oldKBState = (KeyboardState)oldKbState;
                    if (fishingRod.treasureCaught is true)
                        return new WaitCleanTreasure(this);
                    return new EndFishing(this);
                }

                if (fishingRod.fishCaught is false && fishingRod.treasureCaught is false)
                    Debug.Assert(fishingRod is not { pullingOutOfWater: false }, "pullingOutOfWater是false但是还没有退出");

                return this;
            }
        }

        private record WaitCleanTreasure : GameState
        {
            public WaitCleanTreasure(GameState preState) : base(preState)
            {
            }

            public override GameState Next()
            {
                if (Game1.activeClickableMenu is ItemGrabMenu menu)
                {
                    var actualInventory = menu.ItemsToGrabMenu.actualInventory;
                    if (actualInventory.Count > 0)
                    {
                        var item = actualInventory[0];
                        Game1.playSound("coin");
                        if (Player.addItemToInventoryBool(item))
                        {
                            if (actualInventory.Contains(item))
                                actualInventory.Remove(item);
                            return this;
                        }
                    }

                    if (actualInventory.Count == 0)
                        menu.exitThisMenu();
                }

                if (GetUsingFishingRod() is not { })
                    return new EndFishing(this);

                return this;
            }
        }

        private record EndFishing : GameState
        {
            private int? _facingDirection;
            private int? _fishingPodIndex;

            public EndFishing(GameState preState) : base(preState)
            {
            }

            public override GameState Next()
            {
                switch (Config.ContinuousFishing)
                {
                    case ContinuousFishingValue.AutoFishAndEat when Player is { Stamina: < 20, CurrentTool: FishingRod } && GetFood() is { } obj:
                        _fishingPodIndex = Player.CurrentToolIndex;
                        _facingDirection = Player.FacingDirection;
                        Player.CurrentToolIndex = Config.FoodIndex - 1;
                        Player.eatHeldObject();
                        return this;
                    case ContinuousFishingValue.UntilStaminaTooLow or ContinuousFishingValue.AutoFishAndEat when Player is { Stamina: >= 20 }:
                        if (_fishingPodIndex.HasValue)
                            Player.CurrentToolIndex = _fishingPodIndex.Value;
                        if (_facingDirection.HasValue)
                            Player.FacingDirection = _facingDirection.Value;
                        _fishingPodIndex = null;
                        _facingDirection = null;
                        if (Player.CurrentTool is FishingRod)
                            Game1.pressUseToolButton();
                        return new AfterMiniGame(this).Next();
                    case ContinuousFishingValue.None:
                    case ContinuousFishingValue.UntilStaminaTooLow when Player is { Stamina: < 20 }:
                    case ContinuousFishingValue.AutoFishAndEat when Player is { Stamina: < 20, isEating: false }:
                    case not ContinuousFishingValue.None and not ContinuousFishingValue.UntilStaminaTooLow and not ContinuousFishingValue.AutoFishAndEat:
                        return new AfterMiniGame(this);
                }

                return this;
            }

            private Object? GetFood()
            {
                if (Config.FoodIndex > 0 && Config.FoodIndex <= Player.Items.Count)
                    if (Player.Items[Config.FoodIndex - 1] is Object { Edibility: > 0 } obj && GetFoodValue(obj) is { stamina: > 0, health: >= 0 })
                        return obj;

                return null;
            }

            private static (int stamina, int health) GetFoodValue(Object o)
            {
                var strArray = Game1.objectInformation[o.ParentSheetIndex].Split('/');
                return (Convert.ToInt32(strArray[1]), Convert.ToInt32(strArray[1]));
            }
        }
    }
}