FROM microsoft/aspnetcore:1.1
ARG source
WORKDIR /app
EXPOSE 5002
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "Fabric.Identity.MvcSample.dll"]
