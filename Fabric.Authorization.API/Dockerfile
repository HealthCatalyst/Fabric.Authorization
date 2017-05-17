FROM microsoft/aspnetcore:1.1.2
ARG source
WORKDIR /app
EXPOSE 5004
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "Fabric.Authorization.API.dll"]
