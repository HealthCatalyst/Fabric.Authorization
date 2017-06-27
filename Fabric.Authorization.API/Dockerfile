FROM microsoft/aspnetcore:1.1.2
ARG source
WORKDIR /app
EXPOSE 5004

RUN apt-get update -y && \
	apt-get install -y \
	cron \
	ldap-utils

COPY ${source:-obj/Docker/publish} .

RUN mkdir -p /usr/local/bin \
	&& mv scripts/update-groups.sh /usr/local/bin/update-groups.sh \
	&& chmod a+x scripts/setup-crontab.sh \
	&& chmod a+x /usr/local/bin/update-groups.sh \
	&& cat scripts/setup-crontab.sh | bash

ENTRYPOINT ["dotnet", "Fabric.Authorization.API.dll"]
