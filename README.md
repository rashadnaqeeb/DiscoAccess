# DiscoAccess beta testing notes

DiscoAccess is a mod that makes Disco Elysium: The Final Cut playable by blind users. 
## Install

You need Disco Elysium: The Final Cut on Steam, and the .NET SDK, version 9.0.200 or later, from https://dotnet.microsoft.com/download.

1. Run `setup-bepinex.ps1`. It finds your Steam install on its own and puts the mod loader into the game folder. If it can't find the game, set the `DISCO_ELYSIUM_DIR` environment variable to the game folder and re-run.
2. Launch the game once and wait until you reach the main menu. This first launch generates files the build needs and can take a few minutes, so be patient if it seems frozen. Then quit.
3. Run `build.ps1` the same way. It builds the mod and drops it into the game folder.
4. Play.

To update: pull the new version, close the game, and re-run `build.ps1`. If a game update from Steam breaks things, re-run `setup-bepinex.ps1` first, then `build.ps1`.

## Bugs

I expect a lot of bugs, especially around the scanner and what it can and cannot see. Please feel free to bombard me with examples. Saves help a lot, especially if it's somewhere obscure.

## How everything works

WASD controls a cursor, which can move around your visible screen, blocked by geometry. When it hits the edge of your visible range, you'll hear a boop, which means it's time to click (Enter) so your character moves. The camera is slaved to your character by the game.

The cursor does not stop at all geometry. To avoid getting stuck in tight corners because of small debris and furniture, it's able to hop over meter-wide gaps.

As you move, you will hear wind tones to indicate where geometry is blocking you. You will hear sonar sounds as well, which sweep as you move every 0.4 seconds. I don't super expect that you'll find things with the sonar constantly; it's more a "there's something here" warning.

## Sounds

I haven't made a learn-game-sounds menu yet, but you can find the sounds under `assets/audio`. To summarize: pop is an orb, clink is an interactable, rattle is a lootable container, ding is an NPC, and doors sound like a doorknob being turned. The scanner plays the correct sound over something as it scrolls, which should help.

## The scanner

The scanner lets you press Page Up and Page Down to cycle through things. Ctrl+Page Up and Ctrl+Page Down filter it, though it starts in everything mode. There are also keyboard buttons that do basically the same thing: comma does NPCs and interactables, period handles containers and orbs (things you only ever click once), and slash handles exits. Hold Shift to cycle backwards.

The scanner is its own point of attention, separate from the cursor: cycling it tells you what's there and how far without moving the cursor or your character. To act on the thing it just read, press I and your character walks over and interacts. The scanner is anchored to the player character and only picks up what the player sees, so for more stuff, move the player by clicking somewhere.

I will eventually add bookmarks to the big city map.

## Keys

Cursor keys: WASD moves the cursor, Enter interacts with whatever it's on, Backspace walks to the cursor without interacting, C recenters the cursor on your character, Space stops walking. I interacts with the thing the scanner last read.

Status keys: M for money, H for health, T for time, R for the map you're on (also announced automatically when it changes), X for experience and skill points.

Game keys: Ctrl+C character sheet, Ctrl+I inventory, Ctrl+T thoughts, Ctrl+J journal, F1 game help. Left arrow heals health, right arrow heals morale. 1 and 2 use the items in your left and right hands. F5 quicksaves, F8 quickloads. Escape opens the pause menu. Ctrl+L cycles the game language.

In inv: backspace interacts with the focused item instead of equipping.

Mod settings: F12.

## Controller

A gamepad works alongside the keyboard; every pad control mirrors a keyboard key. Button names below are PlayStation first, Xbox in parentheses.

In the world: the left stick moves the cursor. Cross (A) interacts like Enter, triangle (Y) walks to the cursor without interacting like Backspace, circle (B) recenters the cursor on your character like C, and square (X) opens the character sheet. The bumpers cycle the scanner, left going backwards. The left trigger moves the cursor to the thing the scanner last read, like J, and the right trigger walks over and interacts with it, like I. Clicking the left stick uses your left-hand item like 1, clicking the right stick uses your right-hand item like 2. On the dpad, left and right heal health and morale like the arrow keys, and down stops walking like Space. Flicking the right stick speaks a status readout: up for health, right for money, down for time, left for experience. Start opens the pause menu.

In menus and dialogue: the left stick or dpad navigates, cross (A) activates like Enter, circle (B) backs out like Escape, and triangle (Y) is the secondary action like Backspace. The bumpers step between controls like Shift+Tab and Tab. On the character sheet, inventory, journal, and thoughts screens, the triggers switch between those screens, matching the game's own controller layout. In dialogue, dpad left and right still heal, and the right stick still reads status.
