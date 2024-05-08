---
![BetterBreakerBox](https://i.imgur.com/29SBFvE.png)
---

This mod adds new functionality to the Breaker Box by assigning actions to different combinations of breaker switches.

## Actions
There are a number of actions—some beneficial, some detrimental—that can be triggered by interacting with the Breaker Box. The Breaker Box has 5 switches and each individual combination of switches will trigger a different action.\
Actions are assigned to each combination of switches at random at the beginning of a period (either a new round or a new day) based on configurable weights, and are persistent throughout the period, so you can learn the combinations and their effects as you progress through the game.

### Available actions:
- disarming Turrets in the facility
- making Turrets enter berserk mode
- making the Ship leave early
- turning the breaker box into a charger for battery-powered items
- zapping the player
- swapping the states of automatic doors
- toggling the power in the facility
- causing an EMP that disables all electronic devices on the moon

## Command
There is a new command available in the ship's terminal: `breakerbox`\
This command will retrieve a number of entries from the Facility's handbook, each entry containing an action and the combinations of switches that trigger it.\
The command can retrieve anywhere from 1 to 4 entries per period.

## Configuration
There are a number of configuration options available for this mod. The actions get assigned to the switch combinations based on a weight system, so you can adjust the likelihood of each action being assigned to a combination of switches.

<details>
<summary>Weights</summary>
The weight option of an action can be used to adjust the likelihood of an action being assigned to a combination of switches. A higher weight means that the action is more likely to be assigned, while a weight of 0 means that the action will not be assigned to any combination of switches.
</details>
<details>
<summary>Limits</summary>
The limit option of an action can be used to allow an action to be assigned to a combination of switches only once. This can be useful if you want an action the have a high chance of being assigned to a combination of switches (a high weight) but you don't want it to be assigned to multiple combinations of switches.
</details>
<details>
<summary>Timers</summary>
The timer option of certain actions can be used to set the duration of the action.
</details>
<details>
<summary>Miscellaneous</summary>

- ensureActions: If true, the mod will ensure that every action with a weight greater than 0 is assigned to at least one combination of switches. The remaining combinations of switches will be assigned to actions based on their weights.

- zapDamage: The amount of damage the player takes when they trigger the zap action.
- hintPrice: The price of the `breakerbox` command in the terminal.
- lockDoorsOnEmp: Whether electronic doors should be locked when the EMP action is triggered.
</details>

## Future
This is only a very early version of the mod and there are many more actions we want to add in the future. Some of the actions we are considering include:\
Disarming mines; disabling mine sounds and lights and potentially increasing their radius; a lockdown event in which the entrance to the facility will be locked and players will have to escape through the fire exit; disarming Spike Traps; shortening interval of Spike Traps; integration with other mods (like [MeltdownChance](https://thunderstore.io/c/lethal-company/p/den/Meltdown_Chance/) and weather-related mods)\
If you encounter any bugs (which is very likely), or if you have any suggestions for new actions, features, and improvements, please let us know on the mod's Discord thread.