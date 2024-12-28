# Text Prototype
### How to test
- In Bash Terminal #1
    1. `git clone https://github.com/chris-carrington/text-prototype.git`
    1. `dotnet add package DotNetEnv`
    1. `dotnet add package System.Text.Json`
    1. Add a `.env` file adjacent to `Program.cs`
        - In the file add: `SimpleTextAPIToken=APIKey`
        - In the file add: `SimpleTextPhoneNumber=PhoneNumberToSendTo`
    1. `dotnet run`
- In Bash Terminal #2
    1. `curl http://localhost:3000/send`
