# Change Impact Analyzer
A set of tools to facilitate the execution of a **Change Impact Analysis** in a complex, multi-layered application


# What is "Change Impact Analysis" ?
**Change Impact Analysis** tries to determine which parts of a system are impacted by a given code changes.

cit.: Wikipedia: *'Change impact analysis' or 'impact analysis' is the analysis of changes within a deployed product or application and their potential consequences.*

See also https://en.wikipedia.org/wiki/Change_impact_analysis

# Premise and project status
This project is still in a very early development stage.
Many functionalities are missing, and will be added in the near future


# What does this project contain ?
This project contains an extendable, pluggable WPF application usable to:
1. Load a user-defined **"architecture descriptor"**, a JSON file that describes how the target application is structured in terms of:
   - Layers and components
   - Git-based local repositories of code
2. Automatically discover and generate a diagram that represents the architecture of the application, including the relationships between its modules/components.
3. Automatically analize a selected set of Git commits, and determine how such commits impact on the overall architectural diagram


# How does the Change Impact Analyzer work ?
1. The user create a **"architecture descriptor"** that describe how the target application is structured
2. Using the *"architecture descriptor"*, the *Change Impact Analyzer* loads all the declared *.sln* files
2. For every referenced *.csproj*, the application recursively discover its
   - *.csproj* dependencies
   - *NuGet* dependencies
3. The step (2) is repeated for every dependency, until a full **dependencies graph** is generated
4. The user is asked to select (or declare into the *"architecture descriptor"*) a set of Git-commits
5. The **Change Impact Analyzer** scan all the commits (using the *git log* command) and determine which files has been modified/created
6. For every modified/created file, the corresponding *.csproj* file is marked as "changed"
7. The detected changes are propagated into the dependencies graph, so that the user can clearly see which parts of the system has been impacted

--- 
The dependencies graph is shown just after the analysis is completed, and the detected impacts are highlighted in red

![alt text](https://github.com/DogmaSolutions/ChangeImpactAnalysis/blob/main/Docs/Sample_01.png?raw=true)

The detected impacts are also listed into an appropriate report tab

![alt text](https://github.com/DogmaSolutions/ChangeImpactAnalysis/blob/main/Docs/Sample_02.png?raw=true)

# Supported technologies
At the current state of development, the "Change Impact Analysis" application is able to explore and discover the dependencies of
- .NET SDK-style projects (.csproj) and Visual Studio solutions (.sln)
- NuGet packages
