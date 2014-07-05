parallel-sdk
===

An implementation of the Open Modeling Interface SDK that supports parallelism.

### overview

As the availability of computing infrastructure continues to increase, so too does the need for accessible means for utilizing those resources. An effective approach is to enable desktop-oriented scientific software tools and frameworks to support execution on high performance cyberinfrastructure in a way that is transparent to the user. The Open Modeling Interface (OpenMI) provides a composition framework for the sequential execution of model components. This project provides a modified version of the SDK in which components are executed in parallel. Components that are implemented using the standard OpenMI SDK do not require any changes or recompiling to utilize the parallel SDK; the standard dll can simply be replaced with the parallel version of the dll. Parallel execution of model components can result in significant reductions in simulation runtimes on both multi-core desktop machines as well as in high performance computing environments. 

### references

Bulatewicz, T. and D. Andresen. 2014. Accessible parallelization for the Open Modeling Interface. In Proceedings of the Conference on Extreme Science and Engineering Discovery Environment. Atlanta, GA. July 13-18.

### getting started

For each version of the OpenMI (1.4 and 2.0) there is a `Sample` folder with a sample composition that includes 5 instances of a simple component that demonstrates how to use the parallel SDK. The instances are linked together in a tree formation where one instance accepts input from the other 4 instances. The composition executes 100 time steps and the component sleeps for 5 seconds on each step. By using the parallel SDK, the root instance requests input from all 4 components in parallel. As a result, instead of a total runtime of 2500 seconds (100x5x5) the runtime is 1000 seconds (100x5x2).

To run the sample for 1.4, simply download the project and execute the `run.bat` file in the `Sample/Runtime` folder. For the 2.0 sample, use Pipstrille to load and execute the composition located in the `Sample/Runtime` folder.
