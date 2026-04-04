# KBlazor.Showcase/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY KBlazor/KBlazor.csproj KBlazor/
COPY KBlazor.Showcase/KBlazor.Showcase.csproj KBlazor.Showcase/
RUN dotnet restore KBlazor.Showcase/KBlazor.Showcase.csproj

COPY KBlazor/ KBlazor/
COPY KBlazor.Showcase/ KBlazor.Showcase/
RUN dotnet publish KBlazor.Showcase/KBlazor.Showcase.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KBlazor.Showcase.dll"]
