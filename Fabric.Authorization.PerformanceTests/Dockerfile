FROM ubuntu:16.04

RUN apt-get update && \
	apt-get -y install \
	apt-transport-https 

RUN echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893

RUN apt-get clean && \
	apt-get update && \
	apt-get -y install --no-install-recommends \
	wget \
	default-jre-headless \
	telnet \
	iputils-ping \
	unzip \ 
	dotnet-dev-1.0.4


RUN mkdir /jmeter \
	&& cd /jmeter \
	&& wget https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-3.2.tgz \
	&& tar -xzf apache-jmeter-3.2.tgz \
	&& rm apache-jmeter-3.2.tgz

ENV JMETER_HOME /jmeter/apache-jmeter-3.2/
ENV PATH $JMETER_HOME/bin:$PATH

RUN mkdir /apdexcalc
COPY Fabric.Authorization.Perf.jmx .
COPY create-userproperties.sh .
COPY entrypoint.sh .
COPY publish /apdexcalc

RUN chmod +x create-userproperties.sh \
	&& chmod +x entrypoint.sh

ENTRYPOINT ./entrypoint.sh
