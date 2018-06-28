# Title

Created Authorization Client for .Net Standard 1.6

## Context

We needed to create an Authorization Client SDK for the Authorization Service.  We wanted the service to be up to date with .net core 2.0 and .net standard 1.6 at the time of it's creation.  We also wanted to make sure the service could take in an HttpClient so that the application can control when to dispose of the object when needed.  Lastly, we should create integration tests based off the Fabric.Authorization.IntegrationTests.

## Decision
- Create SDK with C# as opposed to powershell
- We have created an SDK with .net core 1.1 and .net standard 1.6.
- The HttpClient can be passed into the constructor of the Authorization Client.
- Instead of an IntegrationTest project, we created a FunctionalTest project

## Consequences

Considering there is a SDK for Identity (Identity Server) but not for Authorization, we decided to create one, as well as settle on C# because our platform is built around it and chances are if clients are using Windows and our software, then they are using C#.

We chose .net core 1.1 because .net core 2.0 was not compatible with our pipeline.  At the time of creation, the CI pipeline did not have a target for .net core 2.0 to even make a separate step possible.  

HttpClient has issues in disposing of the object.  The OS/APIs in the library hangs onto ports and connections longer than you might think.  This can clog a system if you create and dispose of too many HttpClients at once.  It is then better to have the application manage this state.

The Fabric.Authorization.IntegrationTests are tightly coupled to Nancy's browser object.  Without the ability to tap into it properly, our client could not retrieve secrets from this.  Because of this we replicated Fabric.Authorization.FunctionalTests.  Once this is mirrored, we should further decide to remove the current Fabric.Authorization.FunctionalTests for Fabric.Authorization.Client.FunctionalTests because it keeps the tooling and framework consistant within the .net ecosystem.


----
* Status: accepted
* Date: 2018- 06 - 27
* Related Items: 143849 
	https://healthcatalyst.visualstudio.com/CAP/_workitems/edit/143849

Fabric Authorization Client HttpClient Functional Tests .net core 1.1 2.0 FunctionalTests Fabric.Authorization.Client
