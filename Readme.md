# CliFx

[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://tyrrrz.me/ukraine)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/CliFx/main.yml?branch=master)](https://github.com/Tyrrrz/CliFx/actions)
[![Coverage](https://img.shields.io/codecov/c/github/Tyrrrz/CliFx/master)](https://codecov.io/gh/Tyrrrz/CliFx)
[![Version](https://img.shields.io/nuget/v/CliFx.svg)](https://nuget.org/packages/CliFx)
[![Downloads](https://img.shields.io/nuget/dt/CliFx.svg)](https://nuget.org/packages/CliFx)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)
[![Donate](https://img.shields.io/badge/donate-$$$-8a2be2.svg)](https://tyrrrz.me/donate)
[![Fuck Russia](https://img.shields.io/badge/fuck-russia-e4181c.svg?labelColor=000000)](https://twitter.com/tyrrrz/status/1495972128977571848)

> 🟡 **Project status**: maintenance mode<sup>[[?]](https://github.com/Tyrrrz/.github/blob/master/docs/project-status.md)</sup>

**CliFx** is a simple to use, yet powerful framework for building command-line applications.
Its primary goal is to completely take over the user input layer, allowing you to forget about infrastructural concerns and instead focus on writing your application.

## Terms of use<sup>[[?]](https://github.com/Tyrrrz/.github/blob/master/docs/why-so-political.md)</sup>

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

To learn more about the war and how you can help, [click here](https://tyrrrz.me/ukraine). Glory to Ukraine! 🇺🇦

## Install

- 📦 [NuGet](https://nuget.org/packages/CliFx): `dotnet add package CliFx`

## Features

- Complete application framework, not just an argument parser
- Minimum boilerplate and easy to get started
- Class-first configuration via attributes
- Comprehensive auto-generated help text
- Support for deeply nested command hierarchies
- Graceful cancellation via interrupt signals
- Support for reading and writing binary data
- Testable console interaction layer
- Built-in analyzers to catch configuration issues
- Targets .NET Standard 2.0+
- No external dependencies

## Screenshots

![help screen](.assets/help-screen.png)

## Usage

### Application and commands

To turn your program into a command-line interface, modify your `Main` method so that it delegates execution to `CliApplication`.
You can use `CliApplicationBuilder` to fluently create and configure an instance of `CliApplication`:

```csharp
public static class Program
{
    public static async Task<int> Main() =>
        await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync();
}
```

> **Warning**:
> Ensure that your `Main()` method returns the integer exit code provided by `CliApplication.RunAsync()`, as shown in the above example.
> Exit code is used to communicate execution result to the parent process, so it's important that your program propagates it.

> **Note**:
> When calling `CliApplication.RunAsync()`, **CliFx** resolves command-line arguments and environment variables from `Environment.GetCommandLineArgs()` and `Environment.GetEnvironmentVariables()` respectively.

The code above uses `AddCommandsFromThisAssembly()` to detect command types defined within the current assembly.
Commands are entry points, through which the user can interact with your application.

To define a command, create a new class by implementing the `ICommand` interface and annotate it with the `[Command]` attribute:

```csharp
[Command]
public class HelloWorldCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        console.Output.WriteLine("Hello world!");

        // Return default task if the command is not asynchronous
        return default;
    }
}
```

In order to implement `ICommand`, the class needs to define an `ExecuteAsync(...)` method.
This is the method that gets called by the framework when the user decides to execute this command.

As the only parameter, this method takes an instance of `IConsole`, which is an abstraction around the system console.
Use this abstraction in place of `System.Console` whenever you need to write output, read input, or otherwise interact with the console.

With the basic setup above, the user can now run the application and get a greeting in return:

```powershell
> dotnet myapp.dll

Hello world!
```

Out of the box, the application also comes with built-in `--help` and `--version` options.
They can be used to show help text or application version respectively:

```powershell
> dotnet myapp.dll --help

MyApp v1.0

USAGE
  dotnet myapp.dll [options]

OPTIONS
  -h|--help         Shows help text.
  --version         Shows version information.
```

```powershell
> dotnet myapp.dll --version

v1.0
```

### Parameters and options

Commands can be configured to take input from command-line arguments.
To do that, you need to add properties to the command class and bind them using special attributes.

In **CliFx**, there are two types of argument bindings: **parameters** and **options**.
Parameters are bound from arguments based on the order they appear in, while options are bound by their name.

As an example, here's a command that calculates the logarithm of a value — it uses a parameter binding to specify the input value and an option binding for the logarithm base:

```csharp
[Command]
public class LogCommand : ICommand
{
    // Order: 0
    [CommandParameter(0, Description = "Value whose logarithm is to be found.")]
    public required double Value { get; init; }

    // Name: --base
    // Short name: -b
    [CommandOption("base", 'b', Description = "Logarithm base.")]
    public double Base { get; init; } = 10;

    public ValueTask ExecuteAsync(IConsole console)
    {
        var result = Math.Log(Value, Base);
        console.Output.WriteLine(result);

        return default;
    }
}
```

> **Note**:
> You can specify whether a parameter or an option is required by setting the `IsRequired` property on the attribute.
> Alternatively, you can also use the `required` keyword (introduced in C# 11) on the property to mark the corresponding argument binding as required.

> **Note**:
> **CliFx** has built-in analyzers that detect common errors in command definitions.
> Your code will not compile if the command contains duplicate options, overlapping parameters, or otherwise invalid configuration.

In order to execute this command, at a minimum, the user needs to provide the input value:

```powershell
> dotnet myapp.dll 10000

4
```

They can also pass the `base` option to override the default logarithm base of 10:

```powershell
> dotnet myapp.dll 729 -b 3

6
```

In case the user forgets to specify the `value` parameter, the application will exit with an error:

```powershell
> dotnet myapp.dll -b 10

Missing required parameter(s):
<value>
```

Available parameters and options are also listed in the command's help text, which can be accessed by passing the `--help` option:

```powershell
> dotnet myapp.dll --help

MyApp v1.0

USAGE
  dotnet myapp.dll <value> [options]

PARAMETERS
* value             Value whose logarithm is to be found.

OPTIONS
  -b|--base         Logarithm base. Default: "10".
  -h|--help         Shows help text.
  --version         Shows version information.
```

Overall, parameters and options are both used to consume input from the command-line, but they differ in a few important ways:

|                    | Parameter                                                                      | Option                                                                                               |
| ------------------ | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------- |
| **Identification** | Positional (by relative order).                                                | Nominal (by name or short name).                                                                     |
| **Requiredness**   | Required by default. Only the last parameter can be configured to be optional. | Optional by default. Any option can be configured to be required without limitations.                |
| **Arity**          | Only the last parameter can be bound to a non-scalar property (i.e. an array). | Any option can be bound to a non-scalar property without limitations.                                |
| **Fallback**       | —                                                                              | Can be configured to use an environment variable as fallback if the value isn't explicitly provided. |

As a general guideline, use parameters for required inputs that the command can't function without.
Use options for all other non-required inputs, or when specifying the name explicitly makes the usage clearer.

### Argument syntax

This library employs the POSIX argument syntax, which is used in most modern command-line tools.
Here are some examples of how it works:

- `myapp --foo bar` sets option `"foo"` to value `"bar"`
- `myapp -f bar` sets option `'f'` to value `"bar"`
- `myapp --switch` sets option `"switch"` without value
- `myapp -s` sets option `'s'` without value
- `myapp -abc` sets options `'a'`, `'b'` and `'c'` without value
- `myapp -xqf bar` sets options `'x'` and `'q'` without value, and option `'f'` to value `"bar"`
- `myapp -i file1.txt file2.txt` sets option `'i'` to a sequence of values `"file1.txt"` and `"file2.txt"`
- `myapp -i file1.txt -i file2.txt` sets option `'i'` to a sequence of values `"file1.txt"` and `"file2.txt"`
- `myapp cmd abc -o` routes to command `cmd` (assuming it's a command) with parameter `abc` and sets option `'o'` without value

Additionally, argument parsing in **CliFx** aims to be as deterministic as possible, ideally yielding the same result regardless of the application configuration.
In fact, the only context-sensitive part in the parser is the command name resolution, which needs to know the list of available commands in order to discern them from parameters.

The parser's context-free nature has several implications on how it consumes arguments.
For example, `myapp -i file1.txt file2.txt` will always be parsed as an option with multiple values, regardless of the arity of the underlying property it's bound to.
Similarly, unseparated arguments in the form of `myapp -ofile` will be treated as five distinct options `'o'`, `'f'`, `'i'`, `'l'`, `'e'`, instead of `'o'` being set to value `"file"`.

These rules also make the order of arguments important — command-line string is expected to follow this pattern:

```powershell
> myapp [...directives] [command] [...parameters] [...options]
```

### Value conversion

Parameters and options can be bound to properties with the following underlying types:

- Basic types
  - Primitive types (`int`, `bool`, `double`, `ulong`, `char`, etc.)
  - Date and time types (`DateTime`, `DateTimeOffset`, `TimeSpan`)
  - Enum types (converted from either name or numeric value)
- String-initializable types
  - Types with a constructor accepting `string` (`FileInfo`, `DirectoryInfo`, etc.)
  - Types with a static `Parse(...)` method accepting `string` and optionally `IFormatProvider` (`Guid`, `Uri`, etc.)
- Nullable versions of all above types (`decimal?`, `TimeSpan?`, etc.)
- Any other type if a custom converter is specified
- Collections of all above types
  - Array types (`T[]`)
  - Types that are assignable from arrays (`IReadOnlyList<T>`, `ICollection<T>`, etc.)
  - Types with a constructor accepting an array (`List<T>`, `HashSet<T>`, etc.)

#### Non-scalar parameters and options

Here's an example of a command with an array-backed parameter:

```csharp
[Command]
public class FileSizeCalculatorCommand : ICommand
{
    // FileInfo is string-initializable and IReadOnlyList<T> can be assigned from an array,
    // so the value of this property can be mapped from a sequence of arguments.
    [CommandParameter(0)]
    public required IReadOnlyList<FileInfo> Files { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var totalSize = Files.Sum(f => f.Length);

        console.Output.WriteLine($"Total file size: {totalSize} bytes");

        return default;
    }
}
```

```powershell
> dotnet myapp.dll file1.bin file2.exe

Total file size: 186368 bytes
```

Same command, but using an option for the list of files instead:

```csharp
[Command]
public class FileSizeCalculatorCommand : ICommand
{
    [CommandOption("files")]
    public required IReadOnlyList<FileInfo> Files { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var totalSize = Files.Sum(f => f.Length);

        console.Output.WriteLine($"Total file size: {totalSize} bytes");

        return default;
    }
}
```

```powershell
> dotnet myapp.dll --files file1.bin file2.exe

Total file size: 186368 bytes
```

#### Custom conversion

To create a custom converter for a parameter or an option, define a class that inherits from `BindingConverter<T>` and specify it in the attribute:

```csharp
// Maps 2D vectors from AxB notation
public class VectorConverter : BindingConverter<Vector2>
{
    public override Vector2 Convert(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return default;

        var components = rawValue.Split('x', 'X', ';');
        var x = int.Parse(components[0], CultureInfo.InvariantCulture);
        var y = int.Parse(components[1], CultureInfo.InvariantCulture);

        return new Vector2(x, y);
    }
}

[Command]
public class SurfaceCalculatorCommand : ICommand
{
    // Custom converter is used to map raw argument values
    [CommandParameter(0, Converter = typeof(VectorConverter))]
    public required Vector2 PointA { get; init; }

    [CommandParameter(1, Converter = typeof(VectorConverter))]
    public required Vector2 PointB { get; init; }

    [CommandParameter(2, Converter = typeof(VectorConverter))]
    public required Vector2 PointC { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var a = (PointB - PointA).Length();
        var b = (PointC - PointB).Length();
        var c = (PointA - PointC).Length();

        var p = (a + b + c) / 2;
        var surface = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

        console.Output.WriteLine($"Triangle surface area: {surface}");

        return default;
    }
}
```

### Environment variables

An option can be configured to use a specific environment variable as fallback.
If the user does not provide value for such option through command-line arguments, the current value of the environment variable will be used instead.

```csharp
[Command]
public class AuthCommand : ICommand
{
    [CommandOption("token", EnvironmentVariable = "AUTH_TOKEN")]
    public required string AuthToken { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        console.Output.WriteLine(AuthToken);

        return default;
    }
}
```

```powershell
> $env:AUTH_TOKEN="test"

> dotnet myapp.dll

test
```

Environment variables can be configured for options of non-scalar types (arrays, lists, etc.) as well.
In such case, the value of the environment variable will be split by `Path.PathSeparator` (`;` on Windows, `:` on Unix systems).

### Multiple commands

In order to facilitate a variety of different workflows, command-line applications may provide the user with more than just a single command.
Complex applications may also nest commands underneath each other, employing a multi-level hierarchical structure.

With **CliFx**, this is achieved by simply giving each command a unique name through the `[Command]` attribute.
Commands that have common name segments are considered to be hierarchically related, which affects how they're listed in the help text.

```csharp
// Default command, i.e. command without a name
[Command]
public class DefaultCommand : ICommand
{
    // ...
}

// Child of default command
[Command("cmd1")]
public class FirstCommand : ICommand
{
    // ...
}

// Child of default command
[Command("cmd2")]
public class SecondCommand : ICommand
{
    // ...
}

// Child of FirstCommand
[Command("cmd1 sub")]
public class SubCommand : ICommand
{
    // ...
}
```

Once configured, the user can execute a specific command by pre-pending its name to the passed arguments.
For example, running `dotnet myapp.dll cmd1 arg1 -p 42` will execute `FirstCommand` in the above example.

Requesting help will show direct subcommands of the current command:

```powershell
> dotnet myapp.dll --help

MyApp v1.0

USAGE
  dotnet myapp.dll [options]
  dotnet myapp.dll [command] [...]

OPTIONS
  -h|--help         Shows help text.
  --version         Shows version information.

COMMANDS
  cmd1              Subcommands: cmd1 sub.
  cmd2

You can run `dotnet myapp.dll [command] --help` to show help on a specific command.
```

The user can also refine their help request by querying it on a specific command:

```powershell
> dotnet myapp.dll cmd1 --help

USAGE
  dotnet myapp.dll cmd1 [options]
  dotnet myapp.dll cmd1 [command] [...]

OPTIONS
  -h|--help         Shows help text.

COMMANDS
  sub

You can run `dotnet myapp.dll cmd1 [command] --help` to show help on a specific command.
```

> **Note**:
> Defining a default (unnamed) command is not required.
> If it's absent, running the application without specifying a command will just show the root-level help text.

### Reporting errors

Commands in **CliFx** do not directly return exit codes, but can instead communicate execution errors by throwing `CommandException`.
This special exception can be used to print an error message to the console, return a specific exit code, and also optionally show help text for the current command:

```csharp
[Command]
public class DivideCommand : ICommand
{
    [CommandOption("dividend")]
    public required double Dividend { get; init; }

    [CommandOption("divisor")]
    public required double Divisor { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (Math.Abs(Divisor) < double.Epsilon)
        {
            // This will print the error and set exit code to 133
            throw new CommandException("Division by zero is not supported.", 133);
        }

        var result = Dividend / Divisor;
        console.Output.WriteLine(result);

        return default;
    }
}
```

```powershell
> dotnet myapp.dll --dividend 10 --divisor 0

Division by zero is not supported.


> $LastExitCode

133
```

> **Warning**:
> Even though exit codes are represented by 32-bit integers in .NET, using values outside 8-bit unsigned range will cause an overflow on Unix systems.
> To avoid unexpected results, use numbers between 1 and 255 for exit codes that indicate failure.

### Graceful cancellation

Console applications support the concept of interrupt signals, which can be issued by the user to abort the currently ongoing operation.
If your command performs critical work, you can intercept these signals to handle cancellation requests in a graceful way.

In order to make the command cancellation-aware, call `console.RegisterCancellationHandler()` to register the signal handler and obtain the corresponding `CancellationToken`.
Once this method is called, the program will no longer terminate on an interrupt signal but will instead trigger the token, so it's important to be able to process it correctly.

```csharp
[Command]
public class CancellableCommand : ICommand
{
    private async ValueTask DoSomethingAsync(CancellationToken cancellation)
    {
        await Task.Delay(TimeSpan.FromMinutes(10), cancellation);
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // Make the command cancellation-aware
        var cancellation = console.RegisterCancellationHandler();

        // Execute some long-running cancellable operation
        await DoSomethingAsync(cancellation);

        console.Output.WriteLine("Done.");
    }
}
```

> **Warning**:
> Cancellation handler is only respected when the user sends the interrupt signal for the first time.
> If the user decides to issue the signal again, the application will terminate immediately regardless of whether the command is cancellation-aware.

### Type activation

Because **CliFx** takes responsibility for the application's entire lifecycle, it needs to be capable of instantiating various user-defined types at run-time.
To facilitate that, it uses an interface called `ITypeActivator` that determines how to create a new instance of a given type.

The default implementation of `ITypeActivator` only supports types that have public parameterless constructors, which is sufficient for the majority of scenarios.
However, in some cases you may also want to define a custom initializer, for example when integrating with an external dependency container.

The following example shows how to configure your application to use [`Microsoft.Extensions.DependencyInjection`](https://nuget.org/packages/Microsoft.Extensions.DependencyInjection) as the type activator in **CliFx**:

```csharp
public static class Program
{
    public static async Task<int> Main()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<MyService>();

        // Register commands
        services.AddTransient<MyCommand>();

        var serviceProvider = services.BuildServiceProvider();

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeActivator(serviceProvider)
            .Build()
            .RunAsync();
    }
}
```

### Testing

Thanks to the `IConsole` abstraction, **CliFx** commands can be easily tested in isolation.
While an application running in production would rely on `SystemConsole` to interact with the real console, you can use `FakeConsole` and `FakeInMemoryConsole` in your tests to execute your commands in a simulated environment.

For example, imagine you have the following command:

```csharp
[Command]
public class ConcatCommand : ICommand
{
    [CommandOption("left")]
    public string Left { get; init; } = "Hello";

    [CommandOption("right")]
    public string Right { get; init; } = "world";

    public ValueTask ExecuteAsync(IConsole console)
    {
        console.Output.Write(Left);
        console.Output.Write(' ');
        console.Output.Write(Right);

        return default;
    }
}
```

To test it, you can instantiate the command in code with the required values, and then pass an instance of `FakeInMemoryConsole` as parameter to `ExecuteAsync(...)`:

```csharp
// Integration test at the command level
[Test]
public async Task ConcatCommand_executes_successfully()
{
    // Arrange
    using var console = new FakeInMemoryConsole();

    var command = new ConcatCommand
    {
        Left = "foo",
        Right = "bar"
    };

    // Act
    await command.ExecuteAsync(console);

    var stdOut = console.ReadOutputString();

    // Assert
    Assert.That(stdOut, Is.EqualTo("foo bar"));
}
```

Similarly, you can also test your command at a higher level like so:

```csharp
// End-to-end test at the application level
[Test]
public async Task ConcatCommand_executes_successfully()
{
    // Arrange
    using var console = new FakeInMemoryConsole();

    var app = new CliApplicationBuilder()
        .AddCommand<ConcatCommand>()
        .UseConsole(console)
        .Build();

    var args = new[] {"--left", "foo", "--right", "bar"};
    var envVars = new Dictionary<string, string>();

    // Act
    await app.RunAsync(args, envVars);

    var stdOut = console.ReadOutputString();

    // Assert
    Assert.That(stdOut, Is.EqualTo("foo bar"));
}
```

### Debug and preview mode

When troubleshooting issues, you may find it useful to run your app in debug or preview mode.
To do that, you need to pass the corresponding directive before any other arguments.

In order to run the application in debug mode, use the `[debug]` directive.
This will cause the program to launch in a suspended state, waiting for debugger to be attached to the process:

```powershell
> dotnet myapp.dll [debug] cmd -o

Attach debugger to PID 3148 to continue.
```

To run the application in preview mode, use the `[preview]` directive.
This will short-circuit the execution and instead print the consumed command-line arguments as they were parsed, along with resolved environment variables:

```powershell
> dotnet myapp.dll [preview] cmd arg1 arg2 -o foo --option bar1 bar2

Command-line:
  cmd <arg1> <arg2> [-o foo] [--option bar1 bar2]

Environment:
  FOO="123"
  BAR="xyz"
```

You can also disallow these directives, e.g. when running in production, by calling `AllowDebugMode(...)` and `AllowPreviewMode(...)` methods on `CliApplicationBuilder`:

```csharp
var app = new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .AllowDebugMode(true) // allow debug mode
    .AllowPreviewMode(false) // disallow preview mode
    .Build();
```

## Etymology

**CliFx** is made out of "Cli" for "Command-line Interface" and "Fx" for "Framework".
It's pronounced as "cliff ex".
