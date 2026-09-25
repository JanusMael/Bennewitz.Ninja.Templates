# The site as a container image: framework-dependent on the ASP.NET Core runtime image, running as
# the image's non-root `app` user, on port 8080. CI builds and runs it on every push; nothing pushes
# it anywhere.
#
#   docker build --build-arg COMMIT_SHA=$(git rev-parse HEAD) -t sitestem .
#   docker run -p 8080:8080 sitestem
#
# TLS belongs to whatever is in front of it. Behind a proxy that is not on loopback, add its network
# to ForwardedHeadersOptions in Program.cs, or every request looks like plain HTTP from the proxy.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# ⚠ The repository's build files first, then the project, then restore: Directory.Build.props
# carries the AutoVersioning reference and Directory.Packages.props every version, so a restore
# without them fails (NU1015) or resolves something else. Copied before the source so a source-only
# change reuses the restored layer.
COPY ["Directory.Build.props", "Directory.Packages.props", "NuGet.config", "./"]
COPY ["src/SiteStem/SiteStem.csproj", "src/SiteStem/"]
RUN dotnet restore "src/SiteStem/SiteStem.csproj"

COPY . .
# .git is not in the build context, so the commit the version records comes in as an argument.
ARG COMMIT_SHA=""
RUN dotnet publish "src/SiteStem/SiteStem.csproj" -c Release --no-restore -o /app/publish \
    -p:UseAppHost=false -p:CommitSha=${COMMIT_SHA}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SiteStem.dll"]
