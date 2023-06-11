Technical Details
=================

This document details the major parts of the headless and how they fit together.
It also discusses some of the challenges that cropped up when trying to get Neos
running on a .NET runtime it wasn't compiled for, and the solutions that makes
it - despite everything - work.

## General design
Overall, the server follows modern .NET design patterns and uses plenty of 
existing components from the ecosystem. First and foremost, the design was made 
with a hosted, UI-less, interaction-free environment in mind.

The application's foundation is the .NET Generic Host, which allows simple and
reusable asynchronous background services. The generic host also integrates well
with external configuration systems and service managers such as systemd.

Configuration also follows current technologies and is based on .NET's 
configuration extensions from Microsoft. This enables configuration of all 
components - future or present - in one location with one syntax and system.

The REST API is implemented via ASP.NET Core, meaning that the server hosts its
own web server in-process. This server (Kestrel) can be extended with countless
modules and middlewares and is highly performance-oriented.

Beyond the basics of pretty much every recently developed .NET console 
application out there, the server roughly follows the same architecture as the
stock headless client. The major differences visible at first glance are as 
follows:

 * The FrooxEngine systems are hosted in an asynchronous background service that 
   avoids manual thread management and frees up more workers for the thread 
   pool. 
 * This also extends to worlds, which run in tasks on the thread pool.

On top of this, the available configuration has also been extended to open up
more options for the end user that aren't available in the stock headless 
client.

As a result of using the .NET Generic Host, we also have native support for
systemd on Linux, enabling rich two-way communication between the program and
the service daemon. In practice, this means that shutdowns, restarts, and 
startups are far more reliable than if we were just running as a simple 
foreground program.  

## Challenges of a new runtime
Okay, so, let's address the elephant in the room - how the hell does this run on
.NET 7? NeosVR is compiled with Unity in mind, and Unity (at least the version 
used by Neos) runs on Mono, which means the assemblies are all targeting the
ancient and Windows-only .NET Framework. The runtimes are fundamentally 
different in implementation and architecture, and running one on the other 
sounds completely impossible at first glance.

Fortunately, Microsoft was quite dedicated to backward (and forward!) 
compatibility during the development of modern .NET, and implemented support for
a few core technologies that lets modern .NET applications use existing 
assemblies.

### .NET can reference .NET Framework
First and foremost, in .NET Core 2.0 Microsoft introduced the ability for .NET
Core 2.0 applications to reference and use assemblies compiled for .NET 
Framework. Previously, .NET Core had been very limited in its API surface, but 
the 2.0 version introduced so much of the existing API in .NET Framework that
suddenly it was not only possible but *feasible* to use already-existing 
binaries in new applications.

Immo Landwerth has a [pretty good video][1] on the topic, but this image from
his slides says a lot:

![Referencing libraries in .NET Standard][2]

In this case, the topic is .NET Standard (which is now sort of obsolete - it 
gets confusing), but it's applicable to .NET 5 and above too. In particular,
note that Microsoft introduced a compatibility shim that lets the runtime 
translate existing system types between the two runtimes and swap in the real 
implementation at runtime.

So, what does this mean for us? Well, it means that - theoretically - we 
*should* be able to create a .NET 7 executable and "just" reference the 
FrooxEngine assemblies shipped with Neos, string together some glue code, and
we'd be off to the races. Simple, right?

### Narrator: it was not simple
Confession: it was pretty simple. Referencing the assemblies did just work, and
within a couple of hours I had a prototype up and running that could connect to
and authenticate with Neos's server infrastructure. Assets were downloading, 
records were syncing, happy days.

However, a few issues did crop up that needed addressing before the client could
properly start. While Microsoft did allow for cross-runtime referencing, the API
surface provided by the modern runtime is not identical to the one provided by
.NET Framework. Primarily, many Windows-specific APIs have not made the 
transition (for good reason!), as have a number of legacy technologies and 
approaches to code isolation that never really worked all that well in the first
place.

But if we can reference these existing libraries, and they try to use these
unavailable APIs, what happens then? Well, in short... kaboom. Attempting to use
an unavailable API will, at best, raise a `PlatformNotSupportedException` (if 
the method exists but has no implementation), or a `MethodNotFoundException` (
if it's just not there at all).

Fortunately, Microsoft has provided a number of [resources][3] that help 
developers analyze and identify [compatibility issues][4] with their programs. 

In the case of NeosVR and FrooxEngine, Frooxius has fortunately not used many 
Windows-specific APIs, and in the cases where they are used, they're typically
guarded with a platform check beforehand. This is great, because even if the
method is missing from the runtime, it won't actually raise an error until you
try to use it.

### Patching at runtime
To work around the remaining problems, we reach for every .NET modder's 
favourite sledgehammer: [Harmony][5]. If you're not familiar with it, Harmony
lets you edit and modify the code of any .NET method at runtime. You can, with
a bit of know-how, quite literally rewrite anything to anything. For us, this 
means removing or replacing calls to unavailable API surface with equivalent 
alternatives from modern .NET.

### Curse you, encryption
Firstly, the `LibraryInitializer` sets up some options related to HTTP 
communication used by the entire application. In particular, it sets which 
security protocols are allowed by the TLS handler, restricting which encryption
methods and ciphers that can be negotiated between the server and the client.

The technologies behind TLS have evolved a lot over the last 20 or so years, 
leaving several previously standardized cipher sets and TLS protocols deprecated
due to irreparable security problems. One of these is `SSLv3`, which is 
requested as an allowed protocol by Neos.

This protocol (and the earlier ones) is so insecure that modern .NET simply 
refuses to use it even if requested, throwing an error instead. Fortunately, 
Neos's servers have kept up with the times and support more modern TLS 
protocols. The fix is simple - remove SSLv3 from the requested protocol set and
off we go.

#### Thread.Abort is bad
The next issue lies in the `WorkProcessor` class's thread handling. In the 
stock code, threads have no graceful termination condition, and are instead
simply killed outright on application shutdown. This is Very Bad™ - so much so,
in fact, that Microsoft removed the ability to abort threads in modern .NET.

Threads that are aborted have no chance to clean up their resources and can 
leave both memory and OS resources, such as file handles, dangling and occupied
until the entire application quits. Obviously, we want to avoid this whenever 
possible, and it is better to let threads know that they should stop so they can
gracefully terminate.

Fortunately, there's an alternative - `Thread.Interrupt`. This method queues an
interrupt on the thread that causes any operation that puts the thread into a
waiting state - that is, not doing anything and just waiting for work - to 
throw a `ThreadInterruptedException`. This exception can then bubble up or get 
handled properly, allowing leftover resources to be cleaned up as well. Great!

There is a gotcha to this - if the thread never goes into a wait state, the
exception will never get thrown, and the thread will never terminate. In the 
case of `WorkProcessor` and its threads, this is not an issue since the threads 
always go into a wait state between processing incoming workloads.

#### Native libraries and their names
In Mono, native libraries are loaded through a system referred to as DLL 
mapping. In effect, this takes the hardcoded DLL name in something like a 
`DllImport` attribute and remaps it based on your platform via an XML file
shipped with your program, matching it up to whatever's appropriate for your
device.

.NET does not have this feature. Early on, this was a pretty big point of 
contention and a large gap in .NET's cross-platform ambitions. .NET Core 3.0 
introduced the `NativeLibrary` class and a number of associated events that 
applications could hook into instead, enabling flexible overrides at runtime.

Neos uses a number of native libraries to do things like font rendering, image
loading, mesh transformations, etc. By default, it relies on both having the 
native libraries in the current working directory and the names exactly 
matching. On top of this, Assimp's C# bindings roll their own native library
loading outside of .NET's normal systems, instead opting to directly bind to the
dynamic library loading functions of the platform.

The fix is relatively simple - add library mapping logic that uses 
`NativeLibrary` (which is aware of platform-specific loading quirks) and checks
both system directories as well as Neos's own.

#### Stack traces from other threads
In .NET Framework, it was possible to suspend and gather a stack trace from any
thread in your own process. FrooxEngine uses this to provide debug information
about the main engine update routine and where it might have gotten stuck.

This is one of those APIs that never made it into modern .NET, and when the 
engine inevitably gets stuck for a while loading some large object or syncing
something for an extended period of time, the engine watchdog starts crashing
hard with a `MissingMethodException`. 

Again, a relatively simple fix, but this time there's no functional equivalent. 
We could, theoretically, pull in Microsoft's CLR debugging tools and spin up an 
entirely separate process to get us our stack trace, but that's a lot of work 
for something that's only useful to a developer. Instead, we take the easy way
out and just throw a normal but informative `NotSupportedException` that can be
handled gracefully.

#### Type serialization rears its ugly head
So far, the problems have been comparatively trivial to take care of - an 
unsupported API with an alternative there, a value to modify here, some 
reimplementation of platform logic etc. The next - and by far most complex - 
problem took a couple of days to debug. [art0007i][6]'s help was invaluable 
during this part of the testing.

In short, once the client was successfully booting up and connecting to Neos's
servers, a session could be established and published in Neos's world browser.
Any user that tried to join the world, however, was instantly booted out with
an obscure reference to a `NullReferenceException`. 

Digging into the issue, it turns out that Neos's `WorkerManager` relies on the
full type name as generated by the runtime when serializing and deserializing
records. Since the client and server are now running on different runtimes, many
system types are defined in different assemblies and as such their full type 
names differ. Once more, Immo Landwerth's video explains a lot, and their slide 
is an excellent reference.

![Type identity differences][7]

As can be seen above, even something as basic as the superclass for all types,
`object`, now has a critical difference between the two runtimes. .NET Framework
(and, by extension, the desktop client) knows about one `object`, and that's 
defined in `mscorlib`. This is where most system types lived back in the day.

In modern .NET, however, these types have been split up and distributed across
several new libraries with new identities (.NET Standard had one library, 
`netstandard`, but I digress). Thankfully, Microsoft put a system in place to
resolve these problems at runtime via something called "type forwarding". 
Essentially, a type identified by a fully-qualified type name (that is, a type 
name that says "this is my name, and this is the assembly I live in") can be 
annotated with an attribute that forwards the implementation to another type in
another assembly.

This is the compatibility shim that the previous slide showed in purple, and 
which the above slide shows as `NETSTANDARD.DLL`. Effectively, when an existing 
.NET Framework assembly referenced by a modern application asks for 
`mscorlib!Object`, there exists a shim `mscorlib` that *forwards* the request to
the real assembly (in the case of `object`, that happens to be in 
`System.Private.CoreLib` on modern runtimes).

However, since Neos simply uses the assembly-qualified name without any 
modification, the client (which does not have a shim for 
`System.Private.CoreLib`!) cannot find the requested type and chokes.

Fortunately, there is a fix. With Harmony, we can hook into and override the 
`GetTypename` method in `WorkerManager` and provide our own shimming logic. 
Since practically every type that has been moved into another assembly retains
an attribute (`TypeForwardedFromAttribute`) that contains the fully-qualified 
type name it was previously known as, we can - when the attribute exists - pull
the correct name from there instead and provide it to the serialization logic.

This has the nice side effect of being both backward- and forward-compatible, 
since modern runtimes already have the shims for the old names in place. Not 
every type has the attribute, though, requiring us to make a few assumptions 
about types that live in the new `mscorlib` equivalent. In testing, it's been
working well - time will tell if any exceptions to the rule need to be 
implemented.

For serialization (that is, saving object graphs), a similar patch must be
implemented. `FrooxEngine.Worker.WorkerTypeName` naively uses `FullName`, 
and can be redirected to use the forwarder-aware type name generator. The same
goes for `FrooxEngine.SaveControl.StoreTypeVersions`, which also accesses the 
full type name directly.

If you're interested in the specifics, I highly recommend perusing the patches
in the source code. There are some interesting nuances to how Neos uses the 
type name in its serialization logic that necessitated a little bit of 
finagling.

#### Bugs, improvements, and quality of life
At this point, incredibly, the client actually *works*. Multiple people can join
the session, play together, execute LogiX, chat with each other, and do some
pretty performance-heavy stuff without major problems.

Focus has shifted from just making things work to actually improving the NeosVR
hosting experience for server owners, and part of that is taking care of 
frustrating bugs or oddities in the code. In no particular order, that has so
far included

 * Removing noisy stack traces from logs
 * Fixing hardcoded Unity-related paths
 * Replacing the double-parsing of the process's command line arguments
 * Shifting LoadAssembly paths into the JSON configuration
 * REST API!

I'm pretty proud of that last point. Managing the stock headless client has 
always been a bit of a mess, especially since any sort of programmatic control
inevitably involved parsing logs and pushing keystrokes into a virtual terminal.
Now, server administrators and application developers can create well-integrated
tools and utilities that don't need to roll their own communication logic, and 
we can easily extend the available functionality with more advanced operations
and middleware. Kerberos-authenticated headless server control, anyone...? No?

## Closing remarks
I hope that was informative. I've had a blast working with this and exploring
the intricacies of both FrooxEngine and the .NET runtime, and I look forward to
seeing what people can do with it in the future.

If you have any questions or topics regarding the server that you'd like me to
elaborate on, I'd love to hear it. Drop me a line via Discord (@jgullberg) or by
opening an issue on the repo - don't be shy! Suggestions for improvements and
more features are, of course, always welcome.

[1]: https://www.youtube.com/watch?v=vg6nR7hS2lI
[2]: https://image.slidesharecdn.com/2016-11-161212190418/75/net-standard-under-the-hood-3-2048.jpg?cb=1668219832
[3]: https://learn.microsoft.com/en-us/dotnet/core/porting/
[4]: https://learn.microsoft.com/en-us/dotnet/core/porting/net-framework-tech-unavailable
[5]: https://github.com/pardeike/Harmony/
[6]: https://github.com/art0007i/
[7]: https://image.slidesharecdn.com/2016-11-161212190418/75/net-standard-under-the-hood-4-2048.jpg?cb=1668219832
