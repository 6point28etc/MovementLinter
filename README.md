# MovementLinter
MovementLinter is a speedrun practice mod that detects common subtle movement errors and alerts the player through a configurable variety of means. The goal is to detect mistakes which are otherwise difficult to notice in real time, but which can in most cases be avoided very consistently (mostly through leniency mechanics like buffering, dash or wallkick redirects, etc). The implementation is split into two distinct systems -- the *detection* system, which detects when lint rules have been violated (that is, mistakes may have been made) and lives in `src/MovementLinterModule.cs`; and the *response* system, which handles the various methods of alerting the player when a lint rule is triggered and lives in `src/Response.cs`.

## On Compatibility
As you may know, I stopped updating my Everest some time before the release of Everest Core, and I strongly feel that compatibility with pre-Core Everest is a requirement, especially for anything speedrun-related. As such, my top compatibility priority is that MovementLinter will *always* remain compatible with pre-Core Everest. The exact version I've been developing with (and thus can personally guarantee verified compatilibity) is 3678 -- I reserve the posibility to update this to as late as 4449, although I currently have no intention of doing so. On the initial release of this mod, I have also ensured compatibility with recent Everest / SpeedrunTool / CelesteTAS, and I will make a *reasonable* effort to keep it compatible with latest everything in the future, although I make no guarantees here.

Other code mods may interfere with the operation of MovementLinter if they make IL patches that cause MovementLinter's patches to fail to modify the code as intended. For mods I am aware of that cause this problem, I will add an optional dependency on a dummy version of the interfering mod to implement a mutual exclusion requirement (Everest has no mechanism to do this explicitly). If MovementLinter fails to load complaining about some strange version of another mod, that is why, and you need to disable the other mod to use it. The mods that are known to interfere so far are:
* Extended variants

## The Self-Test
This mod ships with a self-test suite to automatically verify that all lint rules are functioning as intended. This is mainly useful for development, but can also be used if you want to verify that the detection system works 100% correct on your setup (especially if you have other mods enabled that may interfere). If you are interested, the steps to run the self-test suite are as follows:

* Install the mod unzipped (just unzip `MovementLinter.zip` in your mods folder).
* Install CelesteTAS if you don't have it already. Version <= 3.39.5 works best, later versions should also work, just not quite as seemlessly.
* If you have CelesteTAS version > 3.39.5 and don't feel like waiting ~20 minutes for the tests to run, make sure you know where your TAS fast-forward hotkey is, and optionally, find the fast-forward speed setting in CelesteTAS and turn it to the max.
* If your version of CelesteTAS has the "Auto-Pause Draft on End" setting (under More Options), it must be turned off.
* Run the console command `linter_selftest`. On CelesteTAS version <= 3.39.5, this will automatically fast-forward through all the tests. On later versions, you'll have to disable the auto-fastforward by running `linter_selftest false false`, then hold the fast-forward key yourself.
* ***Do not press the start/stop TAS hotkey while tests are running***. This will result in incorrect test results.

## The Lint Rules
Players who want to know *exactly* how the detection system actually works should read this section. It is also useful as a starting point for understanding the implementation, for anyone interested in reading the code.

First, some general principles:

* Many rules track some sort of frame timer to measure durations of input patterns. All of these timers are ultimately incremented from the `Player.Update()` function, which means they don't run while Madeline is dead, during screen transitions, while the game is paused, etc -- only during actual gameplay. This means, for example, that if you pause buffer an input sequence, MovementLinter will respond the same way as if you had done the same inputs without pausing.
* Unfortunately, the above also means that no timers run during freeze frames, so MovementLinter will respond as if the freeze frames didn't exist, thus altering the measured input lengths compared to what the player actually inputs. This flaw could likely be resolved, but doing so would require much care and substantial additional testing. Freeze frames suck.
* Timers are cleared / reset on screen transitions (or indeed, any room load, including teleporting and respawning). This means that any lint rule with an associated maximum duration will not trigger if the beginning and end of the input sequence being detected happen across a screen transition. Where appropriate, there are separate lint rules involving room entry or exit to handle this case.
* Many rules look for an input of some short duration, but also check if the short input had any effect on the movement outcome to reduce false positive detections. For these rules, the measured duration of the input is the total length of the input, *not* the length for which that input "mattered". For example, a 3-frame jump release where only one of those frames was in the variable jump window will be measured as a 3-frame release, *not* a 1-frame release.
* Many rules measure the length of an input before a particular action, like dashing or wallkicking. If that action is buffered, the length of the associated input will be measured according to when the action happens, *not* necessarily when the raw input was performed. For example, this means that if jump is released on the same frame as dash is pressed, but the dash doesn't actually come out until a few frames later, the detection will measure a non-zero length for the jump release before the dash.
* The detection system works with Speedrun Tool savestates to make savestates "invisible" to the detection. That is, if you do some movement, save a state, then later load the state and do some more movement, the detection system will work as if you did the inputs before and after the savestate continuously with no savestate in between.

### Jump Release Rules
This section covers the Jump Release Between Jumps, Jump Release Before Dash, and Jump Release Before Room Exit rules, as they share common logic for detecting the jump release. The `JumpReleaseFrames` counter is used to measure the length of the jump release. The counter is:
* At the end of the frame, reset to 0 if jump is held and incremented if jump is not held
* Cleared (reset to a value greater than the maximum configurable jump release length) on `Level.LoadLevel()`, which runs on every screen transition, respawn, teleport, etc
* Cleared at the start of any dash or fastbubble

The `JumpReleaseMatters` flag tracks whether the jump release had any effect on the resulting movement. The flag is:
* Cleared whenever `JumpReleaseFrames` is reset or cleared
* Set within `Player.NormalUpdate()`, in the particular part of the function that checks if jump is held for the purposes of variable-height jumps and half gravity. Thus, only jump releases that affect variable jump height or half gravity will be considered to matter.

Finally, the state of autojump is tracked by the `AutoJumpWasActive` flag. The flag is:
* Set to the value of `Player.AutoJump` within `Player.NormalUpdate()`, in the particular part of the function that checks autojump for the purposes of variable-height jumps and half gravity

The Jump Release Between Jumps check runs at the end of every frame, before the `JumpReleaseFrames` counter is incremented. It activates in the following conditions:
* Jump was `Pressed` this frame (which includes buffered presses), or autojump just became active this frame after not being active the previous frame
* The `JumpReleaseFrames` counter is non-zero and within the configured Jump Release Frames setting
* Any part of the jump release had an effect on movement (`JumpReleaseMatters` is set)

The Jump Release Before Dash check runs at the start of any dash or fastbubble and activates in the following conditions:
* The `JumpReleaseFrames` counter is non-zero and within the configured Jump Release Frames setting
* Any part of the jump release had an effect on movement (`JumpReleaseMatters` is set)

The Jump Release Before Room Exit check runs at the start of any screen transition and activates in the following conditions:
* The direction of the screen transition is included in the configured Transition Direction setting
* The `JumpReleaseFrames` counter is non-zero and within the configured Jump Release Frames setting
* Any part of the jump release had an effect on movement (`JumpReleaseMatters` is set)

### Unbuffered Move After Landing
The `FramesAfterLand` counter is used to measure the time between landing on the ground and doing a dash or jump. The counter is:
* Incremented at the end of each frame
* Cleared (reset to a value greater than the maximum configurable length) on `Level.LoadLevel()`
* Reset to 0 or cleared before the `Player` state machine runs (after `onGround` has been set for this frame) if Madeline is on the ground this frame and wasn't the previous frame. The counter is cleared if this is the first frame after a `Level.LoadLevel()` or if the player is not in control of Madeline (e.g., Madeline lands on the ground during an intro cutscene). Otherwise, it is reset to 0.

The `UltradSinceLanding` flag is used to track whether Madeline has performed an ultra since landing on the ground, for the purposes of ignoring unbuffered jumps that come after an ultra. Note that when I say "performs an ultra", I refer specifically to the act of receiving the 1.2x speed multiplier when colliding with the ground. The flag is:
* Set whenever Madeline performs an ultra
* Whenever Madeline lands on the ground (detected as described above for `FramesAfterLand`), set if Madeline performed an ultra at the end of the previous frame, cleared otherwise

The `CanDashThisFrame` and `CouldDashLastFrame` flags together track whether Madeline could dash the previous frame if dash had been pressed, for the purposes of ignoring dashes that happen some time after landing on the ground, but on the first frame a dash was possible. These flags are set as follows:
* Before the `Player` state machine runs (after the beginning of `Player.Update()` has set the relevant values for this frame), `CanDashThisFrame` is set if Madeline has a dash remaining and is not in dash cooldown
* At the end of the frame, the value of `CanDashThisFrame` is copied to `CouldDashLastFrame`

The Unbuffered Move After Landing check runs in two places: for dashes, it runs at the start of any dash or fastbubble and activates in the following conditions:
* The configured mode includes triggering on dashes
* It was possible to dash on the previous frame (`CouldDashLastFrame` is set)
* The `FramesAfterLand` counter is non-zero and within the configured Frames Late setting

For jumps, the check runs on `Player.Jump()`. Note that this excludes any walljumps (which are not relevant to jumping after landing on the ground), and also supers / demohypers / wavedashes, which often have extension or corner correction concerns that make the earliest possible jump not actually desirable. Also note that the jump check in general is not recommended for most cases due to half gravity concerns. In any case, the check activates in the following conditions:
* The configured mode includes triggering on jumps
* The `FramesAfterLand` counter is non-zero and within the configured Frames Late setting
* If the Ignore Ultras setting is enabled, Madeline has not performed an ultra since landing on the ground (`UltradSinceLanding` is not set)

### Unbuffered Move After Gaining Control
The `InControlFrames` counter is used to measure the time between gaining control of Madeline and doing a dash or jump. The counter is:
* Incremented at the end of each frame
* Reset to 0 from `Level.Update()` whenever the game was doing a cutscene skip the previous frame and no longer is this frame
* Reset to 0 from `Player.Update()` whenever the player did not have control of Madeline the previous frame and now does this frame. Specifically, the player is considered *not* in control during `StDummy` (the usual cutscene state), any of the intro cutscene states, `StBirdDashTutorial` (the prologue dash tutorial cutscene), and `StTempleFall` (the 5A post-mirror falling cutscene).
* Cleared (reset to a value greater than the maximum configurable length) whenever any kind of jump or dash is performed, to prevent triggering multiple times after gaining control once

The `CouldDashLastFrame` flag, described in the Unbuffered Move After Land section, is used for this rule. This rule also uses the `CanJumpThisFrame` and `CouldJumpLastFrame` flags to apply the same reasoning to jumps. These flags are set as follows:
* Before the `Player` state machine runs (after the beginning of `Player.Update()` has set the relevant values for this frame), the value of `CanJumpThisFrame` is set. The value is set to true in any of the following conditions, and false otherwise (note that this only includes situations where the Unbuffered Move After Gaining Control rule could be triggered by a jump, not all scenarios where any kind of jump could happen):
    * Madeline is in `StNormal` and the coyote timer is not expired (this includes still being on the ground)
    * Madeline is in `StNormal` and can jump off the surface of water beneath her
    * Madeline is in `StSwim` and can jump out of the water
    * Madeline is in `StNormal` and can do any kind of walljump
    * Madeline is in `StClimb` and can climb jump
* At the end of the frame, the value of `CanJumpThisFrame` is copied to `CouldJumpLastFrame`

The Unbuffered Move After Gaining Control check runs at the start of any dash or fastbubble, and upon any kind of jump. It activates in the following conditions:
* Madeline dashed and she could have the previous frame (`CouldDashLastFrame` is set), or she jumped and could have the previous frame (`CouldJumpLastFrame` is set)
* The `InControlFrames` counter is non-zero and within the configured Frames Late setting

### Unbuffered Dash After Up Transition
The `FramesSinceUpTransition` counter is used to measure the time between entering the room via an upward screen transition and dashing. The counter is:
* Incremented at the end of each frame
* On `Level.LoadLevel()`, reset to 0 if the `LoadLevel()` happened the same frame as an upward screen transition, cleared otherwise (reset to a value greater than the maximum configurable length)

The Unbuffered Dash After Up Transition check runs at the start of any dash or fastbubble and activates in the following conditions:
* The `FramesSinceUpTransition` counter is greater than 11 (the number of frames of dash cooldown given by upward screen transitions) and within 11 + the configured Frames Late setting

### Unbuffered Fastbubble
The `FramesBeforeFastBubble` counter is used to measure the time between entering a bubble and performing a fastbubble. The counter is:
* Reset to 0 on entering a bubble (from `Player.Boost()` and `Player.RedBoost()`)
* Incremented every frame while in the bubble startup state (from `Player.BoostUpdate()`)

The `CouldDashBeforeBubble` flag is used to track whether Madeline could have dashed the frame before entering the bubble, had dash been pressed. The flag is:
* Assigned the current value of `CanDashThisFrame` when Madeline enters a bubble

The Unbuffered Fastbubble check runs in `Player.BoostUpdate()`, before the `FramesBeforeFastBubble` counter is incremented. It activates in the following conditions:
* The dash or demodash button is `Pressed` this frame (Madeline will perform a fastbubble later this frame)
* Madeline could not have dashed before entering the bubble (`CouldDashBeforeBubble` is not set)
* The `FramesBeforeFastBubble` counter is non-zero and within the configured Frames Late setting

### `moveX` Rules
This section covers the Release Forward Before Room Exit, Release Forward Before Dash, and Release Forward Before Wallkick rules, as they share common logic for detecting when forward has been released. These rules are by far the most complicated in the mod, so allow me to introduce some governing concepts before diving into implementation details:
* If the player presses backward and Madeline's speed reverses before dashing or wallkicking such that the player is holding forward by the time the action is performed, that will not activate these rules, regardless of the length of the `moveX` input. Notably, this includes cases where the player presses backward as Madeline approaches a wall, then does an unbuffered wallkick, such that Madeline hits the wall, then her speed reverses for a few frames, then the wallkick comes out.
* Forward releases matter if and only if part of the release happens in `StNormal`, while swimming, or in a feather. Of course, technically swimming and feathers use analog input but I just check `moveX` everywhere because it should be a decent approximation and I don't want to bother learning how analog works.
* The game treats holding neutral identically to holding backward. Therefore, for the purposes of determining the length of `moveX` inputs, switching between holding backward and neutral is *not* considered a new input.
* If a given `moveX` input is ever a "forward" input (the input matches the direction of Madeline's speed), it continues to be considered a forward input until a new `moveX` input is performed, even if Madeline's speed changes such that it is no longer a forward input.
* When Madeline hits a wall, the direction toward the wall remains considered the "forward direction" until she is no longer next to the wall or starts moving away from it (despite her speed getting set to 0 when she hits the wall, which would otherwise make the "forward" direction ambiguous).
* If the player releases forward after hitting a wall, that release is considered to not matter until Madeline is no longer next to the wall. If the player releases forward before hitting a wall, that release does matter.
* `forceMoveX` (obtained from wallkicks, horizontal springs, climbhops, and flingbirds) is treated as a normal `moveX` input for most of the detection logic, but explicitly excluded from the final rule activation conditions. This means that if `forceMoveX` *ending* results in no longer holding forward, that will be treated as a forward release starting at the end of `forceMoveX`, but no rule will trigger while `forceMoveX` is still active.

The `ReleaseWFrames` counter is used to measure the time since releasing forward (or pressing backward). The counter is manipulated as follows:
* At the end of the frame, if the `moveX` input is unambiguously forward, the counter is cleared (reset to a value greater than the maximum configurable forward release length). Here, "unambiguously forward" means that Madeline's speed at the start of the frame was non-zero and the direction of the `moveX` input matches the direction of that speed, *or* the player is holding toward a wall that Madeline just hit and is still next to.
* At the end of the frame, if the `moveX` input is *not* unambiguously forward, it is reset to 1 if all of the following conditions are met, and incremented otherwise:
    * This is a new `moveX` value compared to the previous frame
    * The previous frame's `moveX` was unambiguously forward
    * This is not the first `moveX` value after entering a room (including via screen transition, teleport, respawn, etc)

The `MoveXUsedThisFrame` and `ReleaseWMatters` flags are used to track which forward releases had an effect on the movement outcome. These flags are set as follows:
* `MoveXUsedThisFrame` is cleared at the start of the frame. It is set throughout the frame in the following places:
    * In `Player.NormalUpdate()`, at the particular point of the function where the game checks the `moveX` value to affect speed
    * In `Player.SwimUpdate()`, at the particular point of the function where the game checks the analog input to affect speed
    * On `Player.StarFlyUpdate()`, if the initial feather startup is complete so the game will check the analog input to affect speed
* `MoveXUsedThisFrame` is also cleared throughout the frame, since other actions may overwrite the speed updates done with the `moveX` / analog input earlier in the frame:
    * On `Player.ClimbBegin()` (grabbing a wall sets X speed to 0)
    * At the start of a dash
    * On `Player.WallJump()` (wallkicking / neutralling resets X speed)
    * On `Player.SuperJump()` (supering / hypering resets X speed)
    * On `Player.SuperWallJump()` (wallbouncing resets X speed)
* At the end of the frame, `ReleaseWMatters` is cleared when the `ReleaseWFrames` counter is cleared (on an unambiguous forward input) or reset (on a new non-forward input).
* At the end of the frame, `ReleaseWMatters` is set if the `moveX` input is not unambiguously forward, `MoveXUsedThisFrame` is set, and Madeline is not against a wall she just hit.

The system for detecting when Madeline is "against a wall she just hit" works as follows:
* When Madeline hits a wall, her X position and the direction of the wall (left or right) are recorded. A flag is set indicating that she just hit a wall and is still against that wall.
* At the start of the frame, if Madeline just hit a wall and is still against that wall, the detection system checks if she has left it. If her X position has changed from her position when she hit the wall, or if there is no longer a wall 1 pixel in the direction of the wall she just hit, she has left the wall and will no longer be considered "against a wall she just hit" until she hits a wall again.
* If Madeline is still against a wall she just hit after the above check, and the `moveX` input is in the direction of that wall, the player is considered to be holding forward for the rest of the frame.

The Release Forward Before Room Exit check runs at the start of any screen transition and activates in the following conditions:
* The direction of the transition is horizontal, not vertical
* The `ReleaseWFrames` counter is non-zero and within the configured Release / Turn Frames setting
* The forward-release had an effect on the movement outcome (`ReleaseWMatters` is set)
* `forceMoveX` is not active

The Release Forward Before Dash rule runs at the start of any dash or fastbubble and activates in the following conditions:
* The `ReleaseWFrames` counter is non-zero and within the configured Release / Turn Frames setting
* The forward-release had an effect on the movement outcome (`ReleaseWMatters` is set)
* `forceMoveX` is not active

The Release Forward Before Wallkick rule runs at the start of a wallkick / neutral (on `Player.WallJump()`) and activates in the following conditions:
* The `ReleaseWFrames` counter is non-zero and within the configured Release / Turn Frames setting
* The forward-release had an effect on the movement outcome (`ReleaseWMatters` is set)
* `forceMoveX` is not active
* `moveX` is non-zero (that is, the walljump is not a neutral jump)
* Madeline is not moving away from the wall

### Fastfall Change Before Dash
The `FastfallMoveYFrames` counter is used to measure the time between pressing or releasing fastfall and dashing. The counter is:
* Reset to 0 at the end of the frame if the "fastfallness" (pressing down vs pressing up or neutral) of the `moveY` input differs from the previous frame, and this is *not* the first `moveY` input after entering a room (including via screen transition, teleport, respawn, etc)
* Incremented at the end of the frame (after possibly being reset)
* Cleared (reset to a value greater than the maximum configurable length) on `Level.LoadLevel()`

The `FastfallCheckedThisFrame` and `FastfallCheckedLastFrame` flags are used to determine whether the fastfall input actually mattered the frame before dashing. These flags are set as follows:
* `FastfallCheckedThisFrame` is cleared at the start of the frame
* From `Player.NormalUpdate()`, in the particular part of the function that handles fastfalling, `FastfallCheckedThisFrame` is set if the conditions for possibly fastfalling are met (Madeline is in the air and moving down fast enough to start fastfalling)
* `FastfallCheckedThisFrame` is cleared on any type of jump, as jumping can overwrite the speed changes done by fastfalling later in the frame
* At the end of the frame, the value of `FastfallCheckedThisFrame` is copied to `FastfallCheckedLastFrame`

The Fastfall Change Before Dash check runs at the start of any dash or fastbubble and activates in the following conditions:
* The `FastfallMoveYFrames` counter is non-zero and within the configured Fastfall Change Frames setting
* The fastfall input had an effect on the previous frame's movement outcome (`FastfallCheckedLastFrame` is set)
* Madeline is not on the ground

### Short Wallboost
The Short Wallboost check runs from the same part of the code that performs a wallboost, and operates by checking the value of `Player.wallBoostTimer`. The length of the wallboost is considered to be the length of the neutral input before pressing away from the wall (not the length of time between climb jumping and actually getting the wallboost, since wallboosts are one frame delayed from when the input is performed). The check activates if this length is within the configured Wallboost Frames setting.

### Buffered Ultra
The `UltradLastFrame` flag is used to track when Madeline performed an ultra (received the 1.2x speed multiplier while colliding with the ground) the previous frame. The flag is:
* Cleared before the part of `Player.NormalUpdate()` that actually moves Madeline based on her speed (note this is also before *other* entities move Madeline at the *beginning* of the next frame)
* Set from the particular part of the code that applies the 1.2x speed multiplier when Madeline collides with the ground

The BufferedUltra check runs from the part of `Player.NormalUpdate()` that causes Madeline to do a normal ground jump. It activates in the following conditions:
* Madeline was not on the ground the previous frame
* Either of these conditions:
    * Madeline is moving down and her previous dash direction was a down-diag (which also indicates she has not ultra'd from that dash yet)
    * The rule is configured to activate even on successful ultras and Madeline performed an ultra since the last frame (`UltradLastFrame` is set), indicating she just landed while her previous dash was a down-diag

## Contributing
Contributions are welcome, and feel to reach out if you have a feature request or believe you've discovered a faulty edge case. You can do the github things or just message me (if you're reading this, you probably know where to find me).
