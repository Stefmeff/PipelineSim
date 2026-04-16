# Welcome to PipSim's documentation!

This jupyter book is the result of my bachelors thesis `"Design Principles and Interactive Visualization of Pipeline Architectures"` in computer engineering at TU Wien. The theoretical part acts as an introduction to both `synchronous`and `asynchronous` pipeline architectures, while also diving into more advanced optimizations like `time borrowing`,`wave pipelining`, etc. Alongside this theoretical part a simulation tool was developed with the aim of simulating/visualizing all the presented circuits and architectures. The tool is meant to be used alongside the text to provide a more intuitive understanding of the topics at hand. For more details about the functionalities and usage of the simulation tool, read the description below. To dive into the theory, feel free to continue with reading the Section `Pipeline Architectures`. Note that for every section introducing a key circuit design, there will be an interactive window included, where you can immediatly test it's functionality. You can download versions for Windows and Linux here, or use the Web-Application:

<div style="display: flex; gap: 16px; flex-wrap: wrap; justify-content: center; margin: 24px 0;">
  <a href="https://github.com/Stefmeff/PipelineSim/releases/download/v1.0.0/PipSim-Windows.zip"
     style="display: inline-flex; align-items: center; gap: 10px; padding: 12px 24px;
            background-color: #222; color: #fff; border: 1px solid #444; border-radius: 8px;
            text-decoration: none; font-weight: 600; font-size: 15px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.15);">
    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
    Windows Download
  </a>
  <a href="https://github.com/Stefmeff/PipelineSim/releases/download/v1.0.0/PipSim-Linux.zip"
     style="display: inline-flex; align-items: center; gap: 10px; padding: 12px 24px;
            background-color: #222; color: #fff; border: 1px solid #444; border-radius: 8px;
            text-decoration: none; font-weight: 600; font-size: 15px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.15);">
    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
    Linux Download
  </a>
</div>

## Tool Description

This simulation tool aims at visualizing the operation principles of both synchronous and asynchronous pipeline architectures, providing a more intuitive understanding of the topic. Similar to other available logic simulators, the tool allows to build digital circuits from scratch by placing and connecting different components. One of the advantages of this tool is, that it can also simulate the timing behaviour of the circuits, which is a key aspect when it comes to pipelining.   

The user can adjust relevant timing parameters of individual components, such as propagation delays, setup and hold time, clock period etc. and see how these parameters affect the circuits overall behaviour and which timing constraints need to be adhered, for the design to work as intended. By coloring signals belonging to different data tokens, the user can easily distinguish and track the propagation of the data that travels through a pipeline. The impelemented delay visualization for logic gates is not only useful for conventional pipelines but even allows the visualization of wave-pipelined circuits. Additionally the tool allows bus-notation (inidividual wires can carry multiple bits) toghether with custom component creation to enable the visualization of more complicated circuits.

<div style="width:100%; overflow:hidden;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8);">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px"
      loading="lazy">
    </iframe>
  </div>
</div>   

The window above shows the main interface of the application. The application allows the user to drag and drop different components from the library on the left and connect them via wires, to realize fully functional circuits. By right-clicking on a component, users can access an editor that allows them to modify component-specific timing parameters. All timing parameters are specified as positive integer values, with the unit of time corresponding to the simulation time displayed in the top menu. The editors also allow the user to
check a box next to the propagation delay setting. This enables the visualization of a components delay via a progress bar displaying the signal waves that travel through a circuit.

To start the simulation, the user has to press the start/pause button in the top menu. Now the user can interact and observe the functionality of the circuit. To adjust the speed of the visualization the user can change the simulation time in the top menu. Additionally there is a single-step mode, that runs the simulation for a specified amount of time units and then pauses it again. If there is a setup or hold time violation in a
sequencing element, the simulation stops and the affected component is marked. Once the design of a circuit is complete, the tool allows the project to be saved and reloaded for later use. For further usage details there is also a help option and tool tip within the application.
