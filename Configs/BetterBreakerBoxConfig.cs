using BepInEx.Configuration;
using Unity.Networking.Transport.Error;

namespace BetterBreakerBox.Configs
{
    public class BetterBreakerBoxConfig
    {
        public static ConfigEntry<int> hintPrice;

        public static ConfigEntry<int> weightDisarmTurrets;
        public static ConfigEntry<int> weightBerserkTurrets;
        public static ConfigEntry<int> weightShipLeave;
        public static ConfigEntry<int> weightDoNothing;
        public static ConfigEntry<int> weightEnableCharge;
        public static ConfigEntry<int> weightZap;
        public static ConfigEntry<int> weightSwapDoors;
        public static ConfigEntry<int> weightPowerOff;

        public static ConfigEntry<bool> disarmTurretsOnce;
        public static ConfigEntry<bool> berserkTurretsOnce;
        public static ConfigEntry<bool> shipLeaveOnce;
        public static ConfigEntry<bool> doNothingOnce;
        public static ConfigEntry<bool> enableChargeOnce;
        public static ConfigEntry<bool> zapOnce;
        public static ConfigEntry<bool> swapDoorsOnce;
        public static ConfigEntry<bool> powerOffOnce;

        public static ConfigEntry<int> disarmTurretsTimer;
        public static ConfigEntry<int> berserkTurretsTimer;
        public static ConfigEntry<int> shipLeaveTimer;


        public BetterBreakerBoxConfig(ConfigFile cfg)
        {
            string descDisarmTurrets = "disarming Turrets in the facility";
            string descBerserkTurrets = "making Turrets enter berserk mode";
            string descShipLeave = "making the Ship leave early";
            string descDoNothing = "having no action";
            string descEnableCharge = "enabling charging battery-powered items on the breaker box";
            string descZap = "zapping the player with a small amount of damage";
            string descSwapDoors = "swapping the state of all doors in the facility";
            string descPowerOff = "turning off the facility's power";

            string weightPreDesc = "Adjusts the probability that a switch combination will trigger the action of";
            string weightPostDesc = "Higher weights make this action more likely to be assigned to one of the switch combinations, a weight of 0 will prevent the action from being assigned.";
            string oncePreDesc = "Select if the action of";
            string oncePostDesc = "should only be able to be assigned to one switch combination, regardless of the weight assigned to it.";

            //prices
            hintPrice = cfg.Bind("General", "hintPrice", 50, "Credits required to purchase a hint from the terminal.");

            //weights
            weightDisarmTurrets = cfg.Bind("Weights", "weightDisarmTurrets", 2, $"{weightPreDesc} {descDisarmTurrets}. {weightPostDesc}");
            weightBerserkTurrets = cfg.Bind("Weights", "weightBerserkTurrets", 2, $"{weightPreDesc} {descBerserkTurrets}. {weightPostDesc}");
            weightShipLeave = cfg.Bind("Weights", "weightShipLeave", 1, $"{weightPreDesc} {descShipLeave}. {weightPostDesc}");
            weightDoNothing = cfg.Bind("Weights", "weightDoNothing", 20, $"{weightPreDesc} {descDoNothing}. {weightPostDesc}");
            weightEnableCharge = cfg.Bind("Weights", "weightEnableCharge", 4, $"{weightPreDesc} {descEnableCharge}. {weightPostDesc}");
            weightZap = cfg.Bind("Weights", "weightZap", 2, $"{weightPreDesc} {descZap}. {weightPostDesc}");
            weightSwapDoors = cfg.Bind("Weights", "weightSwapDoors", 2, $"{weightPreDesc} {descSwapDoors}. {weightPostDesc}");
            weightPowerOff = cfg.Bind("Weights", "weightPowerOff", 1, $"{weightPreDesc} {descPowerOff}. {weightPostDesc}");

            //limit
            disarmTurretsOnce = cfg.Bind("Limit", "disarmTurretsOnce", false, $"{oncePreDesc} {descDisarmTurrets} {oncePostDesc}");
            berserkTurretsOnce = cfg.Bind("Limit", "berserkTurretsOnce", false, $"{oncePreDesc} {descBerserkTurrets} {oncePostDesc}");
            shipLeaveOnce = cfg.Bind("Limit", "shipLeaveOnce", true, $"{oncePreDesc} {descShipLeave} {oncePostDesc}");
            doNothingOnce = cfg.Bind("Limit", "doNothingOnce", false, $"{oncePreDesc} {descDoNothing} {oncePostDesc}");
            enableChargeOnce = cfg.Bind("Limit", "enableChargeOnce", false, $"{oncePreDesc} {descEnableCharge} {oncePostDesc}");
            zapOnce = cfg.Bind("Limit", "zapOnce", false, $"{oncePreDesc} {descZap} {oncePostDesc}");
            swapDoorsOnce = cfg.Bind("Limit", "swapDoorsOnce", false, $"{oncePreDesc} {descSwapDoors} {oncePostDesc}");
            powerOffOnce = cfg.Bind("Limit", "powerOffOnce", true, $"{oncePreDesc} {descPowerOff} {oncePostDesc}");

            //timers
            disarmTurretsTimer = cfg.Bind("Timers", "disarmTurretsTimer", 60, "Time in seconds before Turrets are re-armed after being disarmed");
            berserkTurretsTimer = cfg.Bind("Timers", "berserkTurretsTimer", 60, "Time in seconds before Turrets exit berserk mode");
            shipLeaveTimer = cfg.Bind("Timers", "shipLeaveTimer", 120, "Time in seconds before the Ship leaves after being triggered to leave early");
        }
    }
}