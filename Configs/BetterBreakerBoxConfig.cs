using BepInEx.Configuration;

namespace BetterBreakerBox.Configs
{
    public class BetterBreakerBoxConfig
    {
        public static ConfigEntry<int> weightDisarmTurrets;
        public static ConfigEntry<int> weightBerserkTurrets;
        public static ConfigEntry<int> weightShipLeave;
        public static ConfigEntry<int> weightDoNothing;

        public static ConfigEntry<bool> disarmTurretsOnce;
        public static ConfigEntry<bool> berserkTurretsOnce;
        public static ConfigEntry<bool> shipLeaveOnce;
        public static ConfigEntry<bool> doNothingOnce;

        public static ConfigEntry<int> disarmTurretsTimer;
        public static ConfigEntry<int> berserkTurretsTimer;
        public static ConfigEntry<int> shipLeaveTimer;


        public BetterBreakerBoxConfig(ConfigFile cfg)
        {
            string descDisarmTurrets = "disarming Turrets in the facility";
            string descBerserkTurrets = "making Turrets enter berserk mode";
            string descShipLeave = "making the Ship leave early";
            string descDoNothing = "having no action";
            string weightPreDesc = "Adjusts the probability that a switch combination will trigger the action of";
            string weightPostDesc = "Higher weights make this action more likely to be assigned to one of the switch combinations, a weight of 0 will prevent the action from being assigned.";
            string oncePreDesc = "Select if the action of";
            string oncePostDesc = "should only be able to be assigned to one switch combination, regardless of the weight assigned to it.";


            //weights
            weightDisarmTurrets = cfg.Bind("Weights", "weightDisarmTurrets", 1, $"{weightPreDesc} {descDisarmTurrets}. {weightPostDesc}");
            weightBerserkTurrets = cfg.Bind("Weights", "weightBerserkTurrets", 1, $"{weightPreDesc} {descBerserkTurrets}. {weightPostDesc}");
            weightShipLeave = cfg.Bind("Weights", "weightShipLeave", 1, $"{weightPreDesc} {descShipLeave}. {weightPostDesc}");
            weightDoNothing = cfg.Bind("Weights", "weightDoNothing", 10, $"{weightPreDesc} {descDoNothing}. {weightPostDesc}");

            //limit
            disarmTurretsOnce = cfg.Bind("Limit", "disarmTurretsOnce", false, $"{oncePreDesc} {descDisarmTurrets} {oncePostDesc}");
            berserkTurretsOnce = cfg.Bind("Limit", "berserkTurretsOnce", false, $"{oncePreDesc} {descBerserkTurrets} {oncePostDesc}");
            shipLeaveOnce = cfg.Bind("Limit", "shipLeaveOnce", false, $"{oncePreDesc} {descShipLeave} {oncePostDesc}");
            doNothingOnce = cfg.Bind("Limit", "doNothingOnce", false, $"{oncePreDesc} {descDoNothing} {oncePostDesc}");

            //timers
            disarmTurretsTimer = cfg.Bind("Timers", "disarmTurretsTimer", 10, "Time in seconds before Turrets are re-armed after being disarmed");
            berserkTurretsTimer = cfg.Bind("Timers", "berserkTurretsTimer", 10, "Time in seconds before Turrets exit berserk mode");
            shipLeaveTimer = cfg.Bind("Timers", "shipLeaveTimer", 10, "Time in seconds before the Ship leaves after being triggered to leave early");
        }
    }
}
