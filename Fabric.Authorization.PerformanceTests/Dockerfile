FROM healthcatalyst/fabric.docker.jmeter

COPY Fabric.Authorization.Perf.jmx .
COPY create-userproperties.sh .
COPY appSettings.json /apdexcalc
COPY entrypoint.sh .

RUN chmod +x create-userproperties.sh \
	&& chmod +x entrypoint.sh

ENTRYPOINT ./entrypoint.sh
