# SpaceyToast Note Application
The easiest way to manage your notes. Each notes are linked to an entry in the calendar, so you can look back to what you wrote each day.

![image](https://user-images.githubusercontent.com/46863870/209702132-337765dd-e390-4c06-8593-423af56105cc.png)

### Requirements
#### Packages (in case something doesn't work)
 - Microsoft.NETCore.UniversalWindowsPlatform 6.2.14
 - Microsoft.Toolkit.Uwp.UI.Controls 7.1.3
 - WinUI 2.8.1
 - Newtonsoft.Json 13.0.2
 #### Developer environment
 - The latest version of Visual Studio 2022
 - Microsoft Windows 10 build 2004
 - .NET components including UWP
 - The latest version of the Windows 10 SDK

### Installation
#### Source code
It's very easy to get started if you have a basis knowledge of Visual Studio :)
 1. Download or clone the whole repository
 2. Launch the ``.sln`` file with VS2022
 3. If it ask to upgrade the Windows SDK, accept and wait for it to finish.
 4. Build the solution (Build => Build solution)
 5. Press "Launch" at the top of the toolbar
#### Binary
There's also the possibility to use the binary that is provided in the release page.
 1. Download the latest version of SpaceyToast_binary.zip in the release page
 2. Extract the downloaded file with 7-zip or WinRar
 3. Right click on "install.ps1" and select "Execute with Powershell"
 4. It should ask to install a certificate, accept and wait.
 5. Once it's finished, press WIN or click on the Start Menu button and execute the newly installed application

### User data
User data are stored in: C:\\Users\\[USERNAME]\\AppData\\Local\\Packages\\app_name\\LocalState
All data are saved in JSON files and are parsed when launching the application.

manifest.json: Contains tags and data related to each drawings

tags.json: Contains all available tags

user.json: User data such as theme, last calendar and "Atelier"

[XXXXXXX-XXXXX-XXXXXXXX].json: Each drawings are represented by a file with a "Guid", it contains the position of all elements on the canvas

### Usage
Click on an entry to start a drawing session. You can draw and import some pictures to the canvas to make it more enjoyable =)

Press the little star button to assign a tag to your drawing so you'll never lost it. You can also add a new tag by typing the name on the textbox. Press ENTER to confirm and it will add the new tag.

When using some tools, some buttons are available or not. For example, the opacity of the pencil tool can be adjusted while the brush one cannot.

You also have access to an infinite amount of colors! tap or click on the current selected color and a popup will appear to let you select between a million of colors.

**tools**

-Pencil tool: Draw strokes to the canvas like a real pencil!

-Brush tool: Draw strokes as plain color on the canvas

-Eraser tool: Remove strokes

-Ink selection tool: Select a lot of strokes to operate with (copy, cut, paste, delete)

-Cursor tool: Disable input that comes from the drawing area, allows to select pictures, move and zoom the canvas

#### Shortcuts
CTRL+Z: Undo

CTRL+MouseWheel: Zoom

Left click (using the cursor tool): Move the current view

DEL: Delete the selected objects

### Additonnal informations

#### Warnings:

You must return to the calendar page in order to save your work!

Removed tags are deleted for all drawings just before closing the application for performance purposes. You will not have access to deleted tags during the current application's execution, though.
