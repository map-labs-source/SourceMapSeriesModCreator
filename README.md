# "Source Map Series Mod Creator"

This is a tool which can create collections for individual maps in the form of Source mods, designed for use in mapping competitions. This was created for use in Map Labs to assemble competition builds.

First, the tool takes a "tokenized" template build which acts as a base for competition builds. Several files in this template build are named with and/or contain variables that get replaced when assembling a mod. Then, the tool inserts content from a list of specified entries, and defines the entries accordingly in relevant `.bns` and localization files.

### Requirements for running

This application requires the Microsoft Windows Desktop Runtime 7.0 or newer in order to run. It may prompt you to install it if you do not already have it installed.

### Requirements for building

This application's only dependency is .NET 7.0.

---

# Tutorial

Before using this tool, you will need to have the following:

1. A "tokenized" template build (included in each release)
2. A list of existing files to help identify conflicting content (one for HL2 + episodes is included in each release)

These can be placed in any directory.

Now you should be able to launch the tool. After doing so, you should see **Template Directory** and **Source Content List(s)** on the far left of the window. Change "Template Directory" to the path to the tokenized template build and change "Source Content List(s)" to the path to the file list.

You should also see a **Bin Directory** text box in the upper right corner. Make sure this matches the engine `bin` directory of Source SDK 2013 (i.e. wherever you would launch Hammer from). This is needed for packaging entries into VPKs.

---

## Event parameters

You should see several other text boxes in the upper part of the window. Most of these are used to describe a single event/competition and would change between each one.

Here's what each box does and how you should fill them out:

- **Output Directory** is the directory the build should be placed in. Under normal circumstances, this should be a subfolder in your Steam installation's `sourcemods` folder. (e.g. `sourcemods/eyecandy2`)

- **Event Title** and **Event Comment** describe the name and description of the event. (e.g. "Eye Candy 2" and "For this competition, we asked entrants to focus on unique and beautiful visuals.")
- **Event Long Placement** describes the "long" version of the competition's anthology placement. (e.g. "Map Labs #19")
- **Event Short Placement** describes the "short" version of the competition's anthology placement. (e.g. "ml19")

- **Background Map** is the name of the competition's background map. (e.g. "background_ml19")
	- "Background Map 2" is for background maps which have a separate version used when exiting a map, with the original being used on startup. This field was used in Half-Life: Eternal for the background map's intro speech. You should leave this blank if your background map doesn't do anything like this.

### Content directories

Around the bottom of the window, you should see two text boxes saying **Misc Directory** and **Background Map Directory**. The former is used for packaging content related to the competition itself (usually just map thumbnails and unique icons) while the latter is used for packaging content related to the background map. Both of these text boxes should be paths to folders which directly contain the relevant content. (e.g. Misc Directory becomes `E:\Map Labs\Mod Template\ml01_misc`, with `ml01_misc` containing folders like `maps`, `resource`, etc.)

Note that filling out these text boxes is not explicitly required for the tool to create a build.

---

## Entries

You should see a table in the bottom part of the window containing data for each of the competition's entries. This is used to describe each entry, gather their content, and integrate them into the sourcemod. You can add an entry to this table by clicking the **Add Entry** button directly to the right.

Each entry should have information corresponding to the following columns:

* **Entry Title** refers to the name of the entry. (e.g. "Day of Red Letter")
* **Entrant(s)** refers to the names of each entrant as they. This should match how they wished to be credited. (e.g. "Salamancer")
* **Placement** refers to the competition's placement. In Map Labs competitions, this is mainly just used to mark bonus entries.
* **Comment** allows for an optional comment in the map's description in the Bonus Maps dialog. Use this if there's anything players should know before playing this particular entry.
* **Content Directory** is a path to the entry's content. (e.g. `E:\Map Labs\Entries\ml05\dayofredletter`, with `dayofredletter` containing folders like `maps`, `materials`, etc.)
* **Starting Map** refers to the name of the entry map. (e.g. `ml05_dayofredletter`)

You can delete an entry by selecting it and pressing your keyboard's "Delete" key. You can also adjust the size of each column by clicking on the space between each one.

#### OPTIONAL: Clarification on the "Placement" field

The "Placement" field has a few other options:

* "First", "Second", and "Third" are intended to match scoring results. However, since builds are usually published *before* scores in Map Labs competitions, this isn't really used in practice.
* "NonEntry" just prevents an entry from appearing in the entry list. This is mainly obsolete and was used to get background map/competition-specific content before unique boxes were added. It may be removed in the future.

---

## Saving/Loading

You can save all of the data from the text boxes and entry list by clicking the **Save** or **Save As** buttons on the right side of the window. This will open a Windows Explorer prompt to save it to a new `.xml` file. You can name this file anything and save it anywhere you'd like.

If you open the tool later and/or need to load a different competition's data, click on the **Load** button and select a saved `.xml` file.

The tool will automatically ask you to save changes before exiting the program or creating a mod, but you should still try to save as often as you can.

---

## Checking for conflicting content

Once you have your entries specified, you can check for conflicting content between them by clicking on **Check for conflicting content** in the bottom left corner. This will make sure no entries overwrite each other's files, or overwrite any files from the original Half-Life 2 and episodes (as specified in "Source Content List(s)" from earlier).

After you press the button, a console prompt will appear. If there are any conflicting files between two entries (or between an entry and source content), the checking process will stop and the console will show which files conflict with each other.

If the process completes with no problems, the console will eventually show "No conflicts detected!" and allow itself to be closed by pressing any key.

### Manifest warnings

The tool is designed to ignore script manifests (e.g. `game_sounds_manifest.txt`) in entries and instead print warnings about which entries contain them. These manifests should be removed and any new soundscripts, soundscapes, etc. should be turned into map-specific files.

---

## Creating the sourcemod

Once no conflicts remain, you should be able to assemble the sourcemod by clicking on the green **Create Mod** button in the bottom right corner. This will open another console prompt. If the process completes with no problems, the console will eventually show "Done packaging mod!" and allow itself to be closed by pressing any key.

You should now be able to find the packaged mod in the output directory specified earlier. If it's in the `sourcemods` folder, you should see the mod in your Steam library after restarting Steam. It should be named after whatever was specified in Event Title.

Now, just launch the mod and make sure it works properly. If it does, then congratulations! You have successfully packaged a mapping competition sourcemod.

This is all you'll need in order to put together a basic dev build. However, for Map Labs competitions, there's still a couple other small steps involved for putting together the "release" build:

#### Integrating entry thumbnails

By default, the mod creator will automatically generate placeholder thumbnails for each entry. To find them, go to the packaged mod's `maps/<event short placement>`, with `<event short placement>` being what was specified in "Event Short Placement" (e.g. `maps/ml05`).

You can start integrating real thumbnails by copying these to the directory specified in the "Misc Directory" box. If you didn't specify a Misc Directory, create one somewhere and specify it in the tool. Then, just add a `maps` folder and copy the short placement directory from before (e.g. `ml05`) to this folder.

From that point forward, you can integrate any custom thumbnails by editing each entry's `.tga` file. The tool will *not* overwrite them.

#### Shuffling entries

Release builds should not reflect submission order, and should instead be random. You can randomize this by clicking "Shuffle Entries" before shipping a build.

#### Removing the "Dev Build" tag

Before you ship a packaged build, go into its `gameinfo.txt` and remove the "Dev Build" tag.

---

If you need any clarification later on what the different buttons and fields in the tool do, most of them will provide a tooltip when you hover over them with your mouse.
