# The API as a container image: one native executable on the chiseled runtime-deps image, which has
# no shell, no package manager and no .NET runtime, running as the image's non-root `app` user on
# port 8080. CI builds and runs it on every push; nothing pushes it anywhere.
#
#   docker build --build-arg COMMIT_SHA=$(git rev-parse HEAD) -t apistem .
#   docker run -p 8080:8080 apistem
#
# TLS belongs to whatever is in front of it. Behind a proxy that is not on loopback, add its network
# to ForwardedHeadersOptions in Program.cs, or every request looks like plain HTTP from the proxy.

# The SDK's AOT image carries the native toolchain, clang and the linker, that PublishAot needs.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot AS build
WORKDIR /src

# ⚠ The repository's build files first, then the project, then restore: Directory.Build.props
# carries the AutoVersioning reference and Directory.Packages.props every version, so a restore
# without them fails (NU1015) or resolves something else. Copied before the source so a source-only
# change reuses the restored layer. The runtime identifier is restored too, for the native packages.
COPY ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.config", "./"]
COPY ["src/ApiStem/ApiStem.csproj", "src/ApiStem/"]
RUN dotnet restore "src/ApiStem/ApiStem.csproj" -r linux-x64

COPY . .
# .git is not in the build context, so the commit the version records comes in as an argument.
ARG COMMIT_SHA=""
RUN dotnet publish "src/ApiStem/ApiStem.csproj" -c Release -r linux-x64 --no-restore -o /app/publish \
    -p:CommitSha=${COMMIT_SHA}

# No ICU in a chiseled image, which is why the project sets InvariantGlobalization.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
WORKDIR /app
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
# The executable and its settings; not the .dbg symbols, which only a debugger reads.
COPY --from=build ["/app/publish/ApiStem", "/app/publish/appsettings.json", "./"]
ENTRYPOINT ["./ApiStem"]
