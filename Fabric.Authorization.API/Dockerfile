FROM microsoft/aspnetcore:1.1.2
ARG source
WORKDIR /app
EXPOSE 5004

RUN echo "deb [check-valid-until=no] http://cdn-fastly.deb.debian.org/debian jessie main" > /etc/apt/sources.list.d/jessie.list
RUN echo "deb [check-valid-until=no] http://archive.debian.org/debian jessie-backports main" > /etc/apt/sources.list.d/jessie-backports.list
RUN sed -i '/deb http:\/\/deb.debian.org\/debian jessie-updates main/d' /etc/apt/sources.list

RUN apt-get -o Acquire::Check-Valid-Until=false update -y && \
	apt-get install -y --no-install-recommends\
	cron \
	ldap-utils \
	&& rm -rf /var/lib/apt/lists/*

COPY ${source:-obj/Docker/publish} .

RUN mkdir -p /usr/local/bin \
	&& mv scripts/update-groups.sh /usr/local/bin/update-groups.sh \
	&& chmod a+x scripts/setup-crontab.sh \
	&& chmod a+x /usr/local/bin/update-groups.sh

#RUN	./scripts/setup-crontab.sh

ENTRYPOINT ["dotnet", "Fabric.Authorization.API.dll"]
