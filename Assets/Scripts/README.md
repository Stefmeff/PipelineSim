# Timing Simulation

The timing simulation is handled by the class "TimeTick" (see folder "GeneralPurpose"). The TimeTick class is responsible for keeping track of a global simulation time and sending out timing events to the components in the simulation. The target simulation time is decided by the user via an input field but is adapted if the performance indicated by the delta-time isnt sufficient.

# Tokens/Signal Represenation

Signals in the circuit are represented as "BitToken" objects (see Folder "Tokens"). These tokens have a boolean value representing the logic level (high or low) and a color that represent the token within the simulation. Additionally they have a precise time-stamp, that marks the tokens time of arrival at the current circuit component. This time-stamp is initialized once a token is issued by a source component, and increased when a token travels through a component with a delay. 

# Components

Circuit components (e.g. Flip-Flop, AND-Gate, etc.) are described by the classes inside the folder "ComponentClasses". All the components subscribe to the timing events produced by the TimeTick class and keep track of the simulation time. Every time tick they update their state according to the input signals values and time of arrival. For each component there is a Unity Prefab that stores the respective gameObject and is loaded upon instantiation.

# Pins and Wires

Each component has output and/or input pins (see Folder "ComponentClasses\Wires&Pins"). Theses pins handle the wire connections between components and propagate signals through the circuit. 

# Serialization

For saving and loading projects, the components classes are serialized as .json files. The class "ProjectManager" implements the serialization and deserialization of files and keeps track of all the components in the simulation. Note that Unity uses gameObjects that implement their behaviour via "MonoBehaviour" classes. These Monobehaviour classes can for example handle user inputs but cannot be serialized/deserialized. When the component classes are deserialized they first have to be loaded and coupled with the respective gameObject and its MonoBehaviour (see Folder ComponentMonobehaviour and ILoadable Interface).

# UI+Game Controls

The scripts describing UI components such as buttons, text input fields and other general game controls are all contained within the folder "GeneralPurpose". 

# File browser for Loading and Saving

To browse for files to either load or save data, we use the "StandaloneFileBrowser" for Unity (https://github.com/gkngkc/UnityStandaloneFileBrowser). See the scripts "SaveButton" and "LoadButton" for the usage.