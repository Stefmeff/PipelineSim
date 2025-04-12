(use markdown preview with ctrl+shift+V)

# Timing Simulation

The timing simulation is handled by the class "TimeTick". The TimeTick class is responsible for keeping track of a global simulation time and sending out timing events to listening elements in the simulation. The target simulation time is decided by the user via an input field but is adapted if the performance indicated by the delta-time isnt sufficient.

# Components

Circuit components (e.g. Flip-Flop, AND-Gate, etc.) are described by the classes inside the folder "ComponentClasses". All the components subscribe to the timing events produced by the TimeTick class and keep track of the simulation time. Every tick they update their state according to the inputs and simulation time. For each component there is a Unity Prefab that stores the respective gameObject and is loaded upon instantiation.

# Pins and Wires

Each components has output and/or input pins (see Folder "ComponentClasses\Wires&Pins"). Theses pins handle the wire connections between components and propagate signals through the circuit. 

# Tokens/Signal Represenation

Signals in the circuit are represented as "Token" objects (see folder "Tokens"). (Note: the token representation might be overworked in the future)

# Serialization

For saving and loading projects, the components classes are serialized as .json files. The class "ProjectManager" implements the serialization and deserialization of files and keeps track of all the components in the simulation. Note that Unity uses gameObjects that implement their behaviour via "MonoBehaviour" classes. These Monobehaviour classes can for example handle user inputs but cannot be serialized/deserialized. When the component classes are deserialized they first have to be loaded and coupled with the respective gameObject and its MonoBehaviour (see Folder ComponentMonobehaviour and ILoadable Interface).

# File browser for Loading and Saving

To browse for files to either load or save data, we use the "StandaloneFileBrowser" for Unity (https://github.com/gkngkc/UnityStandaloneFileBrowser). See the scripts "SaveButton" and "LoadButton" for the usage.