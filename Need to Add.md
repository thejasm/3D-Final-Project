
# Mechanics
 - [x] Third Person Camera
	 - [x] Aiming
		 - [x] Zoom
		 - [x] Reticle

# Enemies
- [ ] Turret
	- [ ] Model
	- [ ] AI
		States
		- Idle
			- Turret faces random directions
		- Attacking
			- When player comes into turret's view in range
			- for 5 seconds after last engagement with player
			- When player shoots at turret
			- Aims at player's last known location and fires
	- [ ] Mechanics
		- Slow shooting with explosion
- [ ] Grunt
	- [ ] Model
	- [ ] AI
		States
		- Idle
			- Faces random Directions
		- Pursuing
			- When player comes within FOV
			- Move at max speed towards player
		- Strafing
			- When player is within view and range
			- randomly strafe around player and shoot
	- [ ] Mechanics
		- machine guns x2
- [ ] Tank
	- [ ] Model
	- [ ] AI
		States
		- Idle
			- Faces random Directions
		- Pursuing
			- When player comes within FOV
			- Move slowly towards player
		- Shooting
			- When player is within view and range
			- Stops moving and shoots at player
		- Searching
			- Move towards player's last known position
			- Go back to idle
	- [ ] Mechanics
		- charge up gauss shot

# Abilities
- [ ] Dash
	- [ ] Dash Effects
- [ ] Shield
	- [ ] Shader
- [ ] Gauss Gun
- [ ] Wraithe Shot
	- [ ] Model
- [x] Machine Gun

# Misc
- [ ] Main Menu
- [ ] Pause Menu
- [ ] UI