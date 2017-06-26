FROM microsoft/aspnetcore:1.1.2
ARG source
WORKDIR /app
EXPOSE 5004
COPY ${source:-obj/Docker/publish} .

RUN mkdir -p /usr/local/bin
 
COPY ${source:-scripts/setup-crontab.sh} .
COPY ${source:-scripts/update-groups.sh} /usr/local/bin

RUN chmod a+x ./setup-crontab.sh
RUN chmod a+x /usr/local/bin/update-groups.sh

RUN  ["/bin/bash", "-c", "apt-get update"]
RUN  ["/bin/bash", "-c", "apt-get -y install cron"]
RUN  ["/bin/bash", "-c", "apt-get -y install ldap-utils"]
RUN  ["/bin/bash", "-c", "cat setup-crontab.sh | bash"]

ENTRYPOINT ["dotnet", "Fabric.Authorization.API.dll"]
