[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](/LICENSE.md)
[![GitHub issues](https://img.shields.io/github/issues/KristofferStrube/ActivityPubBotDotNet)](https://github.com/KristofferStrube/ActivityPubBotDotNet/issues)
[![GitHub forks](https://img.shields.io/github/forks/KristofferStrube/ActivityPubBotDotNet)](https://github.com/KristofferStrube/ActivityPubBotDotNet/network/members)
[![GitHub stars](https://img.shields.io/github/stars/KristofferStrube/ActivityPubBotDotNet)](https://github.com/KristofferStrube/ActivityPubBotDotNet/stargazers)

# Introduction
An implementation of a [ActivityPub](https://www.w3.org/TR/activitypub/) bot that can communicate with Mastodon servers.

Try to say *"AcitivityPub Bot Dot Net"* fast three times.

It is build using Minimal API and EF Core with SQLite. It uses the [KristofferStrube.ActivityStreams](https://www.nuget.org/packages/KristofferStrube.ActivityStreams) NuGet package for strongly typed classes for parsing the payload of endpoints and to send messages to other servers.

It is build with inspiration from David Fowler's project [TodoApi](https://github.com/davidfowl/TodoApi).

# Local Development
## Setup Database
Setup the dotnet tool *dotnet-ef*.
```bash
dotnet tool install dotnet-ef -g
```
Create the database by running the following script from the `src/KristofferStrube.ActivityPubBotDotNet.Server/` folder.
```bash
dotnet ef database update
```
## Adding SSH Keys
For the server project you need to add two configuration keys that will contain a RSA key value pair.
These can either be supplied through [User Secrets](https://blog.elmah.io/asp-net-core-not-that-secret-user-secrets-explained/) or by setting the appropriate values in `appsettings.Development.json`. The Keys are:
- `PEM:Public`
- `PEM:Private`
## Running the Server
Go to the `src/KristofferStrube.ActivityPubBotDotNet.Server/` folder and run.
```bash
dotnet run
```

# Goals
## Server Interaction Goals
- [x] [Accept](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-accept) [Follow](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-follow) activities sent to [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person).
- [ ] Remove subscription when someone [Undo](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-undo) a [Follow](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-follow) activity.
- [x] Persist [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person) information in EF Core.
- [ ] Validate message [signing](https://blog.joinmastodon.org/2018/07/how-to-make-friends-and-verify-requests/).
- [x] Dynamic information of [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person).
- [ ] Dynamic webfinger endpoint.
- [ ] Serve [Articles](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-article) of [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person).
- [ ] Send [Create](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-create) Activity message when a new Article is created.
- [ ] Send [Delete](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-delete) Activity message when [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person) information changes.
## Administration Goals
- [ ] Be able to authenticate the admin somehow.
- [ ] Be able to update a [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person) information.
- [ ] Be able to create new [Articles](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-article)
- [ ] Be able to delete a [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person).
## Client Goals
- [ ] Show [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person) information if someone hits the API and doesn't request valid `content-type`.
- [ ] Show [Person](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-person)'s [Articles](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-article) below Profile information.
