namespace AutoFish
{
    public class ModConfig
    {
        /// <summary>
        ///     强制最大力度抛竿
        /// </summary>
        public bool MaxCastPower { get; set; } = true;


        /// <summary>
        ///     上钩时自动点击
        /// </summary>
        public bool AutoHit { get; set; } = true;


        /// <summary>
        ///     快速上钩
        /// </summary>
        public bool FastBite { get; set; } = false;


        /// <summary>
        ///     捕捉宝箱（当遇到传说鱼时忽略）
        /// </summary>
        public bool CatchTreasure { get; set; } = true;


        /// <summary>
        ///     绿条移动模式
        /// </summary>
        public BarSpeedModeValue BarSpeedMode { get; set; } = BarSpeedModeValue.Normal;

        /// <summary>
        ///     连续钓鱼
        /// </summary>
        public ContinuousFishingValue ContinuousFishing { get; set; } = ContinuousFishingValue.None;

        /// <summary>
        ///     自动吃食物时食物的位置
        /// </summary>
        public int FoodIndex { get; set; } = 0;
    }

    public enum ContinuousFishingValue
    {
        None,
        UntilStaminaTooLow,
        AutoFishAndEat,
    }

    public enum BarSpeedModeValue
    {
        Normal,
        Faster,
        Attach,
    }
}