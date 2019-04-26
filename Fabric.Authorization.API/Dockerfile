FROM mcr.microsoft.com/dotnet/core/aspnet:2.1
ARG source
WORKDIR /app
EXPOSE 5004

COPY ${source:-obj/Docker/publish} .

ENTRYPOINT ["dotnet", "Fabric.Authorization.API.dll"]
