FROM ubuntu

RUN apt-get clean && \
	apt-get update && \
	apt-get -y install --no-install-recommends \
	wget \
	default-jre-headless \
	telnet \
	iputils-ping \
	unzip

RUN mkdir /jmeter \
	&& cd /jmeter \
	&& wget https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-3.2.tgz \
	&& tar -xzf apache-jmeter-3.2.tgz \
	&& rm apache-jmeter-3.2.tgz

ENV JMETER_HOME /jmeter/apache-jmeter-3.2/
ENV PATH $JMETER_HOME/bin:$PATH

COPY Fabric.Authorization.Perf.jmx .
COPY create-userproperties.sh .
COPY entrypoint.sh .

ENTRYPOINT ./entrypoint.sh
