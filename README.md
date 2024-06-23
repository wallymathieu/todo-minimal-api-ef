# Todo API

Code based on [todo-csharp-sql](https://github.com/Azure-Samples/todo-csharp-sql).

The code contains only the API part and with added tests. Stripped down for readability and focus.

## Prerequisites

- .NET Core 8
- Visual Studio or Visual Studio Code
- Docker

## Dev container

The repository contains a `devcontainer.json` file for Visual Studio Code. This means that it should be possible to run the test container tests in a dev container in Visual Studio Code. This is a bit of a more advanced pattern and can be safely ignored. 

## Getting started

1. Clone the repository
2. Open the solution in Visual Studio or Visual Studio Code
3. Run the tests

## Difficulties

1. The code uses an odd pattern of configuration key pointing to the connection string. This is not a common pattern and can be confusing.
