# Building

`undercut-f1` can be built in various way to support different deployment targets.

## Native AOT

We don't currently compile to NativeAot, due to single-file packaging constraints (our native libraries currently
need to be shipped alongside the executable rather than being embedded/statically linked).

There is support for NativeAot though, and we ensure continued support by running the AOT-specific roslyn analyzers.

## Build Commands

### Standalone Executable

```sh
# dotnet publish -r <runtime> -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:IncludeAllContentForSelfExtract=true -p:PublicRelease=true UndercutF1.Console/UndercutF1.Console.csproj -o undercutf1-<runtime>-output
dotnet publish -r linux-amd64 -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:IncludeAllContentForSelfExtract=true -p:PublicRelease=true UndercutF1.Console/UndercutF1.Console.csproj -o undercutf1-linux-amd64-output
```

`IncludeNativeLibrariesForSelfExtract` and `IncludeAllContentForSelfExtract` ensures our native library dependencies are
packages inside the executable directly, instead of as lib files next to the executable. This ensures we can publish
a single executable as an artifact.

`DebugType` ensures we publish no debugging symbols, for sake of package size.

`PublicRelease` tells `Nerdbank.GitVersioning` that this is a public build, so it shouldn't use prerelease version
suffixes.

### Docker

Build for a specific platform:

```sh
docker build . -t justaman62/undercutf1:latest --platform linux/amd64
```

Run:

```sh
docker run -it -e TERM_PROGRAM -v $HOME/undercut-f1/data:/data -v $HOME/undercut-f1/logs:/logs justaman62/undercutf1:latest
```

Passing through the `TERM_PROGRAM` environment variable helps `undercut-f1` detect which terminal graphics protocol to
use.

The data and logs directories are configured to be `/data` and `/logs` in the container, respectively.
`-v $HOME/undercut-f1/data:/data -v $HOME/undercut-f1/logs:/logs` mounts these volumes to your home dir, but any dir
can be used.
