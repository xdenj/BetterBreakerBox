using BepInEx.Configuration;

namespace BetterBreakerBox.Configs
{
    public class BetterBreakerBoxConfig
    {
        public static ConfigEntry<int> weightDisarmTurrets;
        public static ConfigEntry<int> weightBerserkTurrets;
        public static ConfigEntry<int> weightShipLeave;
        public static ConfigEntry<int> weightDoNothing;
        public static ConfigEntry<int> weightEnableCharge;
        public static ConfigEntry<int> weightZap;
        public static ConfigEntry<int> weightSwapDoors;
        public static ConfigEntry<int> weightSwitchPower;
        public static ConfigEntry<int> weightEMP;

        public static ConfigEntry<bool> disarmTurretsOnce;
        public static ConfigEntry<bool> berserkTurretsOnce;
        public static ConfigEntry<bool> shipLeaveOnce;
        public static ConfigEntry<bool> doNothingOnce;
        public static ConfigEntry<bool> enableChargeOnce;
        public static ConfigEntry<bool> zapOnce;
        public static ConfigEntry<bool> swapDoorsOnce;
        public static ConfigEntry<bool> switchPowerOnce;
        public static ConfigEntry<bool> empOnce;

        public static ConfigEntry<int> disarmTurretsTimer;
        public static ConfigEntry<int> berserkTurretsTimer;
        public static ConfigEntry<int> shipLeaveTimer;

        public static ConfigEntry<bool> lockDoorsOnEmp;
        public static ConfigEntry<int> zapDamage;
        public static ConfigEntry<int> hintPrice;
        public static ConfigEntry<bool> resetAfterDay;
        public static ConfigEntry<bool> ensureAction;
        public static ConfigEntry<bool> enableChainZap;


        public BetterBreakerBoxConfig(ConfigFile cfg)
        {
            string descDisarmTurrets = "disarming Turrets in the facility";
            string descBerserkTurrets = "making Turrets enter berserk mode";
            string descShipLeave = "making the Ship leave early";
            string descDoNothing = "having no action";
            string descEnableCharge = "enabling charging battery-powered items on the breaker box";
            string descZap = "zapping the player with a small amount of damage";
            string descSwapDoors = "swapping the state of all doors in the facility";
            string descSwitchPower = "toggling the power in the facility";
            string descEMP = "disabling all electronic devices on the moon";

            string weightPreDesc = "Adjusts the probability that a switch combination will trigger the action of";
            string weightPostDesc = "Higher weights make this action more likely to be assigned to one of the switch combinations, a weight of 0 will prevent the action from being assigned.";
            string oncePreDesc = "Select if the action of";
            string oncePostDesc = "should only be able to be assigned to one switch combination, regardless of the weight assigned to it.";
            string timerPostDesc = "The configured time represents the maximum time. Should the remaining time of day be less than the configured time, the timer will be set to the remaining time of day instead.";

            //weights
            weightDisarmTurrets = cfg.Bind("Weights", "weightDisarmTurrets", 15, $"{weightPreDesc} {descDisarmTurrets}. {weightPostDesc}");
            weightBerserkTurrets = cfg.Bind("Weights", "weightBerserkTurrets", 10, $"{weightPreDesc} {descBerserkTurrets}. {weightPostDesc}");
            weightShipLeave = cfg.Bind("Weights", "weightShipLeave", 1, $"{weightPreDesc} {descShipLeave}. {weightPostDesc}");
            weightDoNothing = cfg.Bind("Weights", "weightDoNothing", 30, $"{weightPreDesc} {descDoNothing}. {weightPostDesc}");
            weightEnableCharge = cfg.Bind("Weights", "weightEnableCharge", 10, $"{weightPreDesc} {descEnableCharge}. {weightPostDesc}");
            weightZap = cfg.Bind("Weights", "weightZap", 20, $"{weightPreDesc} {descZap}. {weightPostDesc}");
            weightSwapDoors = cfg.Bind("Weights", "weightSwapDoors", 20, $"{weightPreDesc} {descSwapDoors}. {weightPostDesc}");
            weightSwitchPower = cfg.Bind("Weights", "weightSwitchPower", 20, $"{weightPreDesc} {descSwitchPower}. {weightPostDesc}");
            weightEMP = cfg.Bind("Weights", "weightEMP", 1, $"{weightPreDesc} {descEMP}. {weightPostDesc}");

            //limits
            disarmTurretsOnce = cfg.Bind("Limits", "disarmTurretsOnce", false, $"{oncePreDesc} {descDisarmTurrets} {oncePostDesc}");
            berserkTurretsOnce = cfg.Bind("Limits", "berserkTurretsOnce", false, $"{oncePreDesc} {descBerserkTurrets} {oncePostDesc}");
            shipLeaveOnce = cfg.Bind("Limits", "shipLeaveOnce", true, $"{oncePreDesc} {descShipLeave} {oncePostDesc}");
            doNothingOnce = cfg.Bind("Limits", "doNothingOnce", false, $"{oncePreDesc} {descDoNothing} {oncePostDesc}");
            enableChargeOnce = cfg.Bind("Limits", "enableChargeOnce", false, $"{oncePreDesc} {descEnableCharge} {oncePostDesc}");
            zapOnce = cfg.Bind("Limits", "zapOnce", false, $"{oncePreDesc} {descZap} {oncePostDesc}");
            swapDoorsOnce = cfg.Bind("Limits", "swapDoorsOnce", false, $"{oncePreDesc} {descSwapDoors} {oncePostDesc}");
            switchPowerOnce = cfg.Bind("Limits", "switchPowerOnce", false, $"{oncePreDesc} {descSwitchPower} {oncePostDesc}");
            empOnce = cfg.Bind("Limits", "empOnce", false, $"{oncePreDesc} {descEMP} {oncePostDesc}");

            //timers
            disarmTurretsTimer = cfg.Bind("Timers", "disarmTurretsTimer", 30, $"Time in seconds before Turrets are re-armed after being disarmed. {timerPostDesc}");
            berserkTurretsTimer = cfg.Bind("Timers", "berserkTurretsTimer", 30, $"Time in seconds before Turrets exit berserk mode. {timerPostDesc}");
            shipLeaveTimer = cfg.Bind("Timers", "shipLeaveTimer", 120, $"Time in seconds before the Ship leaves after being triggered to leave early. {timerPostDesc}");

            //misc
            lockDoorsOnEmp = cfg.Bind("Misc", "lockDoorsOnEmp", false, "If true, all automatic doors will be locked when the EMP action is triggered");
            zapDamage = cfg.Bind("Misc", "zapDamage", 25, "Amount of damage dealt to the player when the Zap action is triggered");
            hintPrice = cfg.Bind("Misc", "hintPrice", 50, "Credits required to purchase a hint from the terminal.");
            resetAfterDay = cfg.Bind("Misc", "resetAfterDay", true, "If enabled, the switch combinations will be reset after each day.");
            ensureAction = cfg.Bind("Misc", "ensureAction", true, "If enabled, each action with a weight greater than 0 is guaranteed to be assigned to at least one switch combination. The remaining actions will be assigned based on their weights.");
            enableChainZap = cfg.Bind("Misc", "enableChainZap", true, "If enabled, the Zap action will chain to other players in the vicinity of the player that triggered the action.");
        }
    }
}