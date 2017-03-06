# League Controller Engine #

A prototype for visualizing how League of Legends could be played with a controller

## How to Run League Controller Engine ##

1. **Download** [Visual Studio Community 2015](https://www.visualstudio.com/vs/community/)
2. **Pull** League Controller Engine repo

```bash
git clone git@github.com:qubytes/leaguecontrollerengine.git
```

3. **Open** LeagueControllerEngine.sln with Visual Studio Community 2015
4. **Plug in** Xbox 360 controller

   > NOTE: I have only tested with wired and wireless Xbox 360 controllers

5. **Click** Play

   Located at top-middle as green arrow

## Control Scheme ##

### Analog Sticks ###

* **Left Stick**

  **Action**: *Relative champion movement from the center of the screen (i.e. 'right click'). Limited to set radius*  
  **Release**: *Reset champion movement to center of screen and (i.e. 'stop command')*
  > NOTE: Champion movement is sent at a slower speed to reduce freqency of movement commands. 
  >       Although rapid changes will immediately be sent.
  
* **Right Stick**

  **Action**: *Cursor movement for quick point of interest locations (i.e. move the mouse)*  
  **Relase**: *Cursor resets to center of screen*
  
### Buttons ###

* **Triggers**

  **Left Trigger**: *Left mouse click*  
  **Right Trigger**: *Right mouse click*
  
* **Colored Buttons**

  **A**: *Q ability*  
  **B**: *W ability*  
  **X**: *E ability*  
  **Y**: *R ability*  
  > NOTE: Normal cast is *highly* recommended
  
* **Directional Buttons**

  **Down**: *'On My Way' ping*  
  **Up**: *'Asking for Assistance' ping*  
  **Left**: *'Retreat' ping*  
  **Right**: *'Alert' ping*  
  
* **Bumbers** *(not implemented)*

  **Right**: *Cycle selected item slot clockwise*  
  **Left**: *Cycle selected item slot counter-clockwise*
  
* **Menu Buttons**

  **Start**: *Main Menu (i.e. Escape)*  
  **Select**: *Scoreboard (i.e. Tab)*
