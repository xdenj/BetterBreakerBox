
# BetterBreakerBox

This mod adds new functionality to the Breaker Box by assigning actions to different combinations of breaker switches.

## Actions
There are a number of actions—some beneficial, some detrimental—that can be triggered by interacting with the Breaker Box. The Breaker Box has 5 switches and each individual combination of switches will trigger a different action.
Actions are assigned to each combination of switches at random at the beginning of a round and are persistent throughout the round (until you reach the quota or the game ends), so you can learn the combinations and their effects as you progress throught the round.


### Available actions:
- disarming Turrets in the facility
- making Turrets enter berserk mode
- making the Ship leave early
- turning the breaker box into a charger for battery-powered items
- zapping the player
- swapping the states of automatic doors
- toggling the power in the facility
- causing an EMP that disables all electronic devices on the moon

## Configuration
There are a number of configuration options available for this mod. The actions get assigned to the switch combinations based on a weight system, so you can adjust the likelihood of each action being assigned to a combination of switches.

### Weights
 The weight option of an action can be used to adjust the likelihood of an action being assigned to a combination of switches. A higher weight means that the action is more likely to be assigned, while a weight of 0 means that the action will not be assigned to any combination of switches.

### Limits
The limit option of an action can be used to allow an action to be assigned to a combination of switches only once. This can be useful if you want an action the have a high chance of being assigned to a combination of switches (a high weight) but you don't want it to be assigned to multiple combinations of switches.

### Timers
The timer option of certain actions can be used to set the duration of the action.

### General
zapDamage: The amount of damage the player takes when they trigger the zap action.
hintPrice: The price of the `breakerbox` command in the terminal.
lockDoorsOnEmp: Whether electronic doors should be locked when the EMP action is triggered.


## [Changelog](https://thunderstore.io/c/lethal-company/p/den/BetterBreakerBox/changelog/)

### 1.0.0
- initial release