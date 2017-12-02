FROM microsoft/dotnet:2.0.3-runtime
MAINTAINER Bobby Kotzev

ENV TZ=America/Los_Angeles
ENV appDir /srv/formatik/payment

RUN mkdir -p ${appDir}
WORKDIR ${appDir}

COPY . ${appDir}

#set asp to listen on port 8000, any IP request
ENV ASPNETCORE_URLS=http://*:8000

ENTRYPOINT ["dotnet", "Octagon.Formatik.Payment.dll"]