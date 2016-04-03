# GitMind

A visual Windows Git client, which intends to make it easier to use Git when using a branching model similar to e.g. [GitFlow](http://nvie.com/posts/a-successful-git-branching-model/). The goal is to visualize the branch structure on screen in the same way as the you imagines the structure in your mind.


## Background
Most current Git clients tends to visualize an overwhelming number of branches, which makes the commits history difficult to understand As a workaround, many developer simplify history by rebasing or squashing. 

Some clients like e.g. Visual Studio tries to reduce the complexity. But I think that the [Bazaar QLog](http://doc.bazaar.canonical.com/explorer/en/guide/qbzr/qlog.html) [interface](http://stackoverflow.com/questions/5099152/git-history-visualizer-gui-that-can-hide-branches) might provide a better inspiration. 

The GitMind client aims to provide an user experience, where the visualization of branches and commits history is understandable and usable without the need for rebasing or squashing. 


## Status
The current code status is a prototype which tries to give a preview of the base concepts. The focus has been to produce a working viewing client which handles a "happy path". I.e. there is limited error handling and very limited handling of different environments. The code is currently being refactored to be more robust and better adhere to standard patterns like e.g. MVVM, as well as being more readable and understandable. 

Once the viewing functionality like history, status, diff, ... has been stabilized, action commands like commit, push, pull branch, merge, ... will be implemented as well.  


