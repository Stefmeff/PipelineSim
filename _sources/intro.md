# Welcome to PipSim's documentation!

This jupyter book is the result of my bachelors thesis `"Design Principles and Interactive Visualization of Pipeline Architectures"` in computer engineering at TU Wien. The theoretical part acts as an introduction to both `synchronous`and `asynchronous` pipeline architectures. Alongside this theoretical part a simulation tool was developed with the aim of simulating/visualizing all the presented circuits and architectures. The tool is meant to be used alongside the text to provide a more intuitive understanding of the topics at hand. For more details about the functionalities and usage of the simulation tool, read the description below. To dive into the theory, feel free to continue with reading the Section `Pipeline Architectures`. Note that for every section introducing a key circuit design, there will be an interactive window included, where you can immediatly test it's functionality.

## Tool Description

This simulation tool aims at visualizing the operation principles of both synchronous and asynchronous pipeline architectures, providing a more intuitive understanding of the topic. Similar to other available logic simulators, the tool allows to build digital circuits from scratch by placing and connecting different components. One of the advantages of this tool is, that it can also simulate the timing behaviour of the circuits, which is a key aspect when it comes to pipelining.


<div style="width:100%; overflow:hidden;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8);">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px"
      loading="lazy">
    </iframe>
  </div>
</div>      

The user can adjust relevant timing parameters of individual components, such as propagation delays, setup and hold time, clock period etc. and see how these parameters affect the circuits overall behaviour and which timing constraints need to be adhered, for the design to work as intended. By coloring signals belonging to different
tokens, the user can easily distinguish and track the propagation of the data that travels through a pipeline.

The window above shows the main interface of the application. The application allows the user to drag and drop different components from the library on the left and connect them via wires, to realize fully functional circuits. By right-clicking on a component, users can access an editor that allows them to modify component-specific timing parameters. All timing parameters are specified as positive integer values, with the unit of time corresponding to the simulation time displayed in the top menu. The editors also allow the user to
check a box next to the propagation delay setting. This enables the visualization of a components delay via a progress bar displaying the signal waves that travel through a circuit.

To start the simulation, the user has to press the start/pause button in the top menu. Now the user can interact and observe the functionality of the circuit. To adjust the speed of the visualization the user can change the simulation time in the top menu. Additionally there is a single-step mode, that runs the simulation for a specified amount of time units and then pauses it again. If there is a setup or hold time violation in a
sequencing element, the simulation stops and the affected component is marked. Once the design of a circuit is complete, the tool allows the project to be saved and reloaded for later use. For further usage details there is also a help option and tool tip within the application.

